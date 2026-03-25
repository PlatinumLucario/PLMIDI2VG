using Kermalis.MIDI;

namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KMIDI
{
    private bool CheckNoteEnd(ref MP2KEvent trackEvent, IMIDIEvent midiEvent)
    {
        trackEvent.Param2 += midiEvent.DeltaTicks;

        DetermineEventCategory(midiEvent, out MIDIEventCategory category);

        switch (category)
        {
            case MIDIEventCategory.Control:
                {
                    int channel = ((IMIDIChannelMessage)midiEvent.Msg).Channel & 0xF;

                    if (channel != MIDIChannel)
                    {
                        return false;
                    }

                    switch (midiEvent.Msg)
                    {
                        case NoteOffMessage noteOff:
                            {
                                int note = (int)noteOff.Note;
                                if (note == trackEvent.Note)
                                {
                                    return true;
                                }
                                break;
                            }
                        case NoteOnMessage noteOn:
                            {
                                int note = (int)noteOn.Note;
                                int velocity = noteOn.Velocity;
                                if (velocity == 0 && note == trackEvent.Note)
                                {
                                    return true;
                                }
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }

                    return false;
                }
            case MIDIEventCategory.SysEx:
                {
                    return false;
                }
            case MIDIEventCategory.Meta:
                {
                    int metaEventType = (int)((MetaMessage)midiEvent.Msg).Type;

                    if (metaEventType == 0x2F)
                    {
                        throw new InvalidDataException("There's no NoteOff event after this NoteOn event. Every note must have a NoteOff event before an EndOfTrack Meta type.");
                    }

                    return false;
                }
            default:
                {
                    throw new InvalidDataException("This event is invalid and can't be used.");
                }
        }
    }

    internal void FindNoteEnd(ref MP2KEvent trackEvent, IMIDIEvent midiEvent)
    {
        int savedRunningStatus = _runningStatus;

        trackEvent.Param2 = 0;

        while (!CheckNoteEnd(ref trackEvent, midiEvent.Next!))
        {
            if (midiEvent.Next is not null)
            {
                midiEvent = midiEvent.Next;
            }
        }

        _runningStatus = savedRunningStatus;
    }
}
