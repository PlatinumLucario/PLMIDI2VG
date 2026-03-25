using System.Diagnostics;

namespace PlatinumLucario.MIDI.GBA.MP2K;

// Based on ipatix's midi2agb midi_remove_empty_tracks and
// find_next_event_at_tick_index methods which are rewritten
// into C# to operate with MP2KEvent structs
internal partial class MP2KMIDI
{
    internal static bool FindNextSameEvent(List<MP2KEvent> events, EventType type, int startIndex, int ctrl = -1)
    {
        int nextIndex = startIndex + 1;
        while (true)
        {
            if (nextIndex >= events.Count)
            {
                return false;
            }
            if (events[nextIndex].Time > events[startIndex].Time)
            {
                return false;
            }
            if (events[nextIndex].Type == type)
            {
                if (ctrl != -1)
                {
                    if (events[nextIndex].Param1 == ctrl)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            nextIndex++;
        }
    }
    internal void Optimize()
    {
        List<MP2KEvent> tempoEvents = [];
        List<MP2KEvent> timeSignatureEvents = [];

        foreach (MP2KEvent evt in _trackEvents)
        {
            if (evt.Type is EventType.Tempo)
            {
                tempoEvents.Add(evt);
                _trackEvents.Remove(evt);
            }
            else if (evt.Type is EventType.TimeSignature)
            {
                timeSignatureEvents.Add(evt);
                _trackEvents.Remove(evt);
            }
        }

        MP2KUtils.SortEvents(tempoEvents);
        MP2KUtils.SortEvents(timeSignatureEvents);

        bool del = true;
        foreach (MP2KEvent evt in _trackEvents)
        {
            if (evt.Type is EventType.Note)
            {
                del = false;
                break;
            }
        }
        if (del)
        {
            Debug.WriteLine($"No Note Event found in Track {MP2KConverter.Instance!.Engine.AGBTrack}, removing track...");
            _trackEvents.Clear();
            Debug.WriteLine($"Removed Track {MP2KConverter.Instance!.Engine.AGBTrack}");
        }

        if (_trackEvents.Count is 0)
        {
            return;
        }

        for (int ievt = 0; ievt < timeSignatureEvents.Count; ievt++)
        {
            if (FindNextSameEvent(timeSignatureEvents, EventType.TimeSignature, ievt))
            {
                timeSignatureEvents.RemoveAt(ievt--);
            }
        }

        int insertBegin = 0;
        foreach (MP2KEvent tev in tempoEvents)
        {
            var position = tempoEvents.ToArray().GetLowerBound(insertBegin);
            insertBegin = position + 1;
            _trackEvents.Insert(position, tev);
        }

        insertBegin = 0;
        foreach (MP2KEvent tev in timeSignatureEvents)
        {
            var position = timeSignatureEvents.ToArray().GetLowerBound(insertBegin);
            insertBegin = position + 1;
            _trackEvents.Insert(position, tev);
        }
    }
}
