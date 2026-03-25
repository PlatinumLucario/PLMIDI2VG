using Kermalis.MIDI;
using System.Diagnostics;

namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KMIDI
{
    private MIDIHeaderChunk? _midiHeader;
    private MIDIFormat _midiFormat;
    private ushort _midiNumTracks;
    private TimeDivisionValue _midiTimeDivision;
    private MIDITrackChunk[]? _midiTracks;

    private int _absoluteTime;
    private int _runningStatus;
    private readonly List<MP2KEvent> _trackEvents = [];
    private readonly List<MP2KEvent> _seqEvents = [];
    private int _minNote;
    private int _maxNote;
    private int _blockCount = 0;

    internal int MIDIChannel;
    internal int InitialWait;

    internal void ReadMIDIFileHeader()
    {
        _midiHeader = MP2KConverter.Instance!.MIDIFile.HeaderChunk;
        _midiFormat = _midiHeader.Format;
        _midiNumTracks = _midiHeader.NumTracks;
        _midiTimeDivision = _midiHeader.TimeDivision;
    }

    internal void StartTrack()
    {
        _absoluteTime = 0;
        _runningStatus = 0;
    }

    private void ConvertTimes(List<MP2KEvent> midiEvents)
    {
        for (int i = 0; i < midiEvents.Count; i++)
        {
            MP2KEvent ev = midiEvents[i];
            ev.Time = 24 * MP2KConverter.Instance!.ClocksPerBeat * ev.Time / _midiTimeDivision.PPQN_TicksPerQuarterNote;

            if (ev.Type is EventType.Note)
            {
                ev.Param1 = (byte)MP2KUtils.NoteVelocityLUT[ev.Param1];

                int duration = 24 * MP2KConverter.Instance!.ClocksPerBeat * ev.Param2 / _midiTimeDivision.PPQN_TicksPerQuarterNote;

                if (duration == 0)
                {
                    duration = 1;
                }

                if (!MP2KConverter.Instance!.ExactGateTimeEnabled && duration < 96)
                {
                    duration = MP2KUtils.NoteDurationLUT[duration];
                }

                ev.Param2 = duration;
                midiEvents[i] = ev;
            }
        }
    }

    private static List<MP2KEvent> SplitTime(List<MP2KEvent> inEvents)
    {
        List<MP2KEvent> outEvents = [];

        int time = 0;

        foreach (MP2KEvent evt in inEvents)
        {
            int diff = evt.Time - time;

            if (diff > 96)
            {
                int wholeNoteCount = (diff - 1) / 96;
                diff -= 96 * wholeNoteCount;

                for (int i = 0; i < wholeNoteCount; i++)
                {
                    time += 96;
                    MP2KEvent timeSplitEvent = new()
                    {
                        Time = time,
                        Type = EventType.TimeSplit
                    };
                    outEvents.Add(timeSplitEvent);
                }
            }

            int lutValue = MP2KUtils.NoteDurationLUT[diff];

            if (lutValue != diff)
            {
                MP2KEvent timeSplitEvent = new()
                {
                    Time = time + lutValue,
                    Type = EventType.TimeSplit
                };
                outEvents.Add(timeSplitEvent);
            }

            time = evt.Time;

            outEvents.Add(evt);
        }

        return outEvents;
    }

    private static List<MP2KEvent> CreateTies(List<MP2KEvent> inEvents)
    {
        List<MP2KEvent> outEvents = [];

        foreach (MP2KEvent evt in inEvents)
        {
            if (evt.Type == EventType.Note && evt.Param2 > 96)
            {
                MP2KEvent tieEvent = evt;
                tieEvent.Param2 = -1;
                outEvents.Add(tieEvent);

                MP2KEvent eotEvent = new()
                {
                    Time = evt.Time + evt.Param2,
                    Type = EventType.EndOfTie,
                    Note = evt.Note
                };
                outEvents.Add(eotEvent);
            }
            else
            {
                outEvents.Add(evt);
            }
        }

        return outEvents;
    }

    private void CalculateWaits(List<MP2KEvent> events)
    {
        InitialWait = events[0].Time;
        int wholeNoteCount = 0;

        for (int i = 0; i < events.Count && events[i].Type != EventType.EndOfTrack; i++)
        {
            MP2KEvent evt = events[i];
            evt.Time = events[i + 1].Time - events[i].Time;

            if (events[i].Type == EventType.TimeSignature)
            {
                evt.Type = EventType.WholeNoteMark;
                evt.Param2 = wholeNoteCount++;
            }

            events[i] = evt;
        }
    }

    internal void ReadMIDITracks()
    {
        _midiTracks = new MIDITrackChunk[MP2KConverter.Instance!.MIDIFile.EnumerateTrackChunks().Count()];
        _midiTracks = [.. MP2KConverter.Instance!.MIDIFile.EnumerateTrackChunks()];
        ReadSeqEvents();

        MP2KConverter.Instance!.Engine.AGBTrack = 1;

        for (int midiTrack = 0; midiTrack < _midiTracks.Length - 1; midiTrack++, MIDIChannel++)
        {
            ReadTrackEvents();

            if (_minNote != 0xFF)
            {
#if DEBUG
                Debug.WriteLine($"Track{MP2KConverter.Instance!.Engine.AGBTrack} = Midi-Ch.{MIDIChannel + 1}");
#endif

                // Remove TEMPO from all tracks except track 1
                if (MP2KConverter.Instance!.Engine.AGBTrack == 2)
                {
                    _seqEvents.RemoveAll(EventMatch);
                    static bool EventMatch(MP2KEvent e) => e.Type == EventType.Tempo;
                }

                List<MP2KEvent> ev = MergeEvents();

                ConvertTimes(ev);
                ev = InsertTimingEvents(ev);
                ev = CreateTies(ev);
                MP2KUtils.SortEvents(ev);
                ev = SplitTime(ev);
                CalculateWaits(ev);

                if (MP2KConverter.Instance!.CompressionEnabled)
                {
                    Compress(ev);
                }

                MP2KConverter.Instance!.Engine.WriteAGBTrack(ev);

                MP2KConverter.Instance!.Engine.AGBTrack++;
            }
        }
    }

}
