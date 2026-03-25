using Kermalis.MIDI;

namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KMIDI
{
    private byte GetChannelNum()
    {
        int channel = -1;
        for (IMIDIEvent ev = _midiTracks![0].First!; ev is not null; ev = ev.Next!)
        {
            if (ev.Msg is MetaMessage mev)
            {
                mev.ReadMIDIChannelPrefixMessage(out byte ch);
                channel = ch;
            }
        }
        return (byte)channel;
    }

    internal void DetermineEventCategory(IMIDIEvent midiEvent, out MIDIEventCategory category)
    {
        switch (midiEvent.Msg)
        {
            case NoteOffMessage noteOff: // >= 0x80 & < 0x90
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0x80 + noteOff.Channel;
                    break;
                }
            case NoteOnMessage noteOn: // >= 0x90 & < 0xA0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0x90 + noteOn.Channel;
                    break;
                }
            case PolyphonicPressureMessage polyphonicPressure: // >= 0xA0 & < 0xB0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0xA0 + polyphonicPressure.Channel;
                    break;
                }
            case ControllerMessage controller: // >= 0xB0 & < 0xC0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0xB0 + controller.Channel;
                    break;
                }
            case ProgramChangeMessage programChange: // >= 0xC0 & < 0xD0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0xC0 + programChange.Channel;
                    break;
                }
            case ChannelPressureMessage channelPressure: // >= 0xD0 & 0xE0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0xD0 + channelPressure.Channel;
                    break;
                }
            case PitchBendMessage pitchBend: // >= 0xE0 & < 0xF0
                {
                    category = MIDIEventCategory.Control;
                    _runningStatus = 0xE0 + pitchBend.Channel;
                    break;
                }
            case SysExMessage sysEx: // >= 0xF0
                {
                    category = MIDIEventCategory.SysEx;
                    _runningStatus = 0;
                    break;
                }
            case MetaMessage meta: // == 0xFF
                {
                    category = MIDIEventCategory.Meta;
                    _runningStatus = 0;
                    break;
                }
            default:
                {
                    category = MIDIEventCategory.Invalid;
                    break;
                }
        }
    }

    private void MakeBlockEvent(ref MP2KEvent midiEvent, EventType type)
    {
        midiEvent.Type = type;
        midiEvent.Param1 = (byte)_blockCount++;
        midiEvent.Param2 = 0;
    }

    private bool ReadSeqEvent(ref MP2KEvent seqEvent, IMIDIEvent midiEvent)
    {
        _absoluteTime += midiEvent.DeltaTicks;
        seqEvent.Time = _absoluteTime;

        DetermineEventCategory(midiEvent, out MIDIEventCategory eventCategory);

        switch (eventCategory)
        {
            case MIDIEventCategory.Control:
            case MIDIEventCategory.SysEx:
                {
                    return false;
                }
            default:
                {
                    throw new InvalidDataException("This MIDI event is invalid and cannot be used.");
                }
            case MIDIEventCategory.Meta:
                {
                    switch (((MetaMessage)midiEvent.Msg).Type)
                    {
                        case MetaMessageType.Text:
                        case MetaMessageType.Copyright:
                        case MetaMessageType.TrackName:
                        case MetaMessageType.InstrumentName:
                        case MetaMessageType.Lyric:
                        case MetaMessageType.Marker:
                        case MetaMessageType.CuePoint:
                            {
                                ((MetaMessage)midiEvent.Msg).ReadTextMessage(out string text);
                                switch (text)
                                {
                                    case "[" or "loopStart":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.LoopBegin);
                                            break;
                                        }
                                    case "][" or "loopEndStart":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.LoopEndBegin);
                                            break;
                                        }
                                    case "]" or "loopEnd":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.LoopEnd);
                                            break;
                                        }
                                    case ":":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.Label);
                                            break;
                                        }
                                    default:
                                        {
                                            switch (text[..(text.IndexOf('=') + 1)])
                                            {
                                                case "modt=":
                                                    {
                                                        byte modt = Convert.ToByte(text[5..]);
                                                        modt = Math.Clamp(modt, (byte)0, (byte)2);
                                                        byte channel = GetChannelNum();
                                                        if (channel >= 0)
                                                        {
                                                            seqEvent.Type = EventType.Controller;
                                                            seqEvent.CCExType = CCExType.ModT;
                                                            seqEvent.Channel = channel;
                                                            seqEvent.Time = midiEvent.Ticks;
                                                            seqEvent.Param1 = modt;
                                                        }
                                                        break;
                                                    }
                                                case "modt_global=":
                                                    {
                                                        byte modt = Convert.ToByte(text[12..]);
                                                        modt = Math.Clamp(modt, (byte)0, (byte)2);
                                                        MP2KConverter.Instance!.ModTypeEnabled = true;
                                                        MP2KConverter.Instance!.ModType = modt;
                                                        break;
                                                    }
                                                case "tune=":
                                                    {
                                                        sbyte tune = Convert.ToSByte(text[5..]);
                                                        tune = Math.Clamp(tune, (sbyte)-64, (sbyte)63);
                                                        byte channel = GetChannelNum();
                                                        if (channel >= 0)
                                                        {
                                                            seqEvent.Type = EventType.Controller;
                                                            seqEvent.CCExType = CCExType.Tune;
                                                            seqEvent.Channel = channel;
                                                            seqEvent.Time = midiEvent.Ticks;
                                                            seqEvent.Param1 = (byte)tune;
                                                        }
                                                        break;
                                                    }
                                                case "lfos=":
                                                    {
                                                        byte lfos = Convert.ToByte(text[5..]);
                                                        lfos = Math.Clamp(lfos, (byte)0, (byte)127);
                                                        byte channel = GetChannelNum();
                                                        if (channel >= 0)
                                                        {
                                                            seqEvent.Type = EventType.Controller;
                                                            seqEvent.CCExType = CCExType.LFOS;
                                                            seqEvent.Channel = channel;
                                                            seqEvent.Time = midiEvent.Ticks;
                                                            seqEvent.Param1 = lfos;
                                                        }
                                                        break;
                                                    }
                                                case "lfos_global=":
                                                    {
                                                        MP2KConverter.Instance!.ModSpeedEnabled = true;
                                                        byte lfos = Convert.ToByte(text[..12]);
                                                        lfos = Math.Clamp(lfos, (byte)0, (byte)127);
                                                        MP2KConverter.Instance!.ModSpeed = lfos;
                                                        break;
                                                    }
                                                case "lfodl=":
                                                    {
                                                        byte lfodl = Convert.ToByte(text[6..]);
                                                        lfodl = Math.Clamp(lfodl, (byte)0, (byte)127);
                                                        byte channel = GetChannelNum();
                                                        if (channel >= 0)
                                                        {
                                                            seqEvent.Type = EventType.Controller;
                                                            seqEvent.CCExType = CCExType.LFODL;
                                                            seqEvent.Channel = channel;
                                                            seqEvent.Time = midiEvent.Ticks;
                                                            seqEvent.Param1 = lfodl;
                                                        }
                                                        break;
                                                    }
                                                case "lfodl_global=":
                                                    {
                                                        MP2KConverter.Instance!.ModDelayEnabled = true;
                                                        byte lfodl = Convert.ToByte(text[..13]);
                                                        lfodl = Math.Clamp(lfodl, (byte)0, (byte)127);
                                                        MP2KConverter.Instance!.ModDelay = lfodl;
                                                        break;
                                                    }
                                                case "prio=":
                                                    {
                                                        byte prio = Convert.ToByte(text[5..]);
                                                        prio = Math.Clamp(prio, (byte)0, (byte)127);
                                                        byte channel = GetChannelNum();
                                                        if (channel >= 0)
                                                        {
                                                            seqEvent.Type = EventType.Controller;
                                                            seqEvent.CCExType = CCExType.Prio;
                                                            seqEvent.Channel = channel;
                                                            seqEvent.Time = midiEvent.Ticks;
                                                            seqEvent.Param1 = prio;
                                                        }
                                                        break;
                                                    }
                                                case "modscale_global=":
                                                    {
                                                        MP2KConverter.Instance!.ModScale = Math.Clamp(Convert.ToByte(text[16..]), 0.0f, 16.0f);
                                                        break;
                                                    }
                                                case "sym=":
                                                    {
                                                        MP2KConverter.Instance!.ASMFileLabel = MP2KUtils.ToAlphaNumerical(text[..4].ToCharArray());
                                                        break;
                                                    }
                                                case "mvl=":
                                                    {
                                                        byte mvl = Convert.ToByte(text[..4]);
                                                        if (mvl < 0 || mvl > 128)
                                                        {
                                                            throw new ArgumentOutOfRangeException($"The Master Volume value specified is {mvl} and is out of range, it must be between 0-128");
                                                        }
                                                        MP2KConverter.Instance!.MasterVolume = mvl;
                                                        break;
                                                    }
                                                case "vgr=":
                                                    {
                                                        byte prio = Convert.ToByte(text[..4]);
                                                        if (prio < 0 || prio > 128)
                                                        {
                                                            throw new ArgumentOutOfRangeException($"The Priority value specified is {prio} and is out of range, it must be between 0-127");
                                                        }
                                                        MP2KConverter.Instance!.Priority = prio;
                                                        break;
                                                    }
                                                case "rev=":
                                                    {
                                                        byte rev = Convert.ToByte(text[..4]);
                                                        if (rev < 0 || rev > 127)
                                                        {
                                                            throw new ArgumentOutOfRangeException($"The Reverb value specified is {rev} and is out of range, it must be between 0-127");
                                                        }
                                                        MP2KConverter.Instance!.Reverb = rev;
                                                        break;
                                                    }
                                                case "nat=":
                                                    {
                                                        byte nat = Convert.ToByte(text[..4]);
                                                        if (nat < 0 || nat > 1)
                                                        {
                                                            throw new ArgumentOutOfRangeException($"The Natural Scale value specified is {nat} and is out of range, it must be between 0-1");
                                                        }
                                                        MP2KConverter.Instance!.ApplyNaturalVolumeScale = Convert.ToBoolean(nat);
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        return false;
                                                    }
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        case MetaMessageType.EndOfTrack:
                            {
                                seqEvent.Type = EventType.EndOfTrack;
                                seqEvent.Param1 = 0;
                                seqEvent.Param2 = 0;
                                break;
                            }
                        case MetaMessageType.Tempo:
                            {
                                ((MetaMessage)midiEvent.Msg).ReadTempoMessage(out uint msPQN, out decimal bPM);
                                seqEvent.Type = EventType.Tempo;
                                seqEvent.Param1 = 0;
                                seqEvent.Param2 = (int)msPQN;
                                break;
                            }
                        case MetaMessageType.TimeSignature:
                            {
                                ((MetaMessage)midiEvent.Msg).ReadTimeSignatureMessage(out byte numerator, out byte denominator, out _, out _);

                                int clockTicks = 96 * numerator * MP2KConverter.Instance!.ClocksPerBeat;
                                int timeSignatureValue = clockTicks / denominator;

                                if (timeSignatureValue <= 0 || timeSignatureValue >= 0x10000)
                                {
                                    throw new InvalidDataException("Invalid time signature value.");
                                }

                                seqEvent.Type = EventType.TimeSignature;
                                seqEvent.Param1 = 0;
                                seqEvent.Param2 = timeSignatureValue;
                                break;
                            }
                        default:
                            {
                                return false;
                            }
                    }

                    return true;
                }
        }
    }

    internal void ReadSeqEvents()
    {
        StartTrack();

        for (IMIDIEvent ev = _midiTracks![MP2KConverter.Instance!.Engine.AGBTrack].First!; ev != null; ev = ev.Next!)
        {
            MP2KEvent evt = new();

            if (ReadSeqEvent(ref evt, ev))
            {
                _seqEvents.Add(evt);

                if (ev.Msg is MetaMessage mev && mev.Type == MetaMessageType.EndOfTrack)
                {
                    return;
                }
            }
        }
    }

    private bool ReadTrackEvent(ref MP2KEvent trackEvent, IMIDIEvent midiEvent)
    {
        _absoluteTime += midiEvent.DeltaTicks;
        trackEvent.Time = _absoluteTime;

        DetermineEventCategory(midiEvent, out MIDIEventCategory category);

        switch (category)
        {
            case MIDIEventCategory.Control:
                {
                    switch (midiEvent.Msg)
                    {
                        case NoteOnMessage noteOn: // 0x9_
                            {
                                if (noteOn.Velocity != 0)
                                {
                                    trackEvent.Type = EventType.Note;
                                    trackEvent.Note = (byte)noteOn.Note;
                                    trackEvent.Param1 = noteOn.Velocity;
                                    FindNoteEnd(ref trackEvent, midiEvent);
                                    if (trackEvent.Param2 > 0)
                                    {
                                        if ((int)noteOn.Note < _minNote)
                                        {
                                            _minNote = (int)noteOn.Note;
                                        }
                                        if ((int)noteOn.Note > _maxNote)
                                        {
                                            _maxNote = (int)noteOn.Note;
                                        }
                                    }
                                }
                                break;
                            }
                        case ControllerMessage controller: // 0xB_
                            {
                                trackEvent.Type = EventType.Controller;
                                trackEvent.Param1 = (byte)controller.Controller;
                                trackEvent.Param2 = controller.Value;
                                break;
                            }
                        case ProgramChangeMessage programChange: // 0xC_
                            {
                                trackEvent.Type = EventType.InstrumentChange;
                                trackEvent.Param1 = (byte)programChange.Program;
                                trackEvent.Param2 = 0;
                                break;
                            }
                        case PitchBendMessage pitchBend: // 0xE_
                            {
                                trackEvent.Type = EventType.PitchBend;
                                trackEvent.Param1 = pitchBend.LSB;
                                trackEvent.Param2 = pitchBend.MSB;
                                break;
                            }
                        default:
                            {
                                return false;
                            }
                    }

                    return true;
                }
            case MIDIEventCategory.SysEx: // 0xF0-0xFE
                {
                    return false;
                }
            case MIDIEventCategory.Meta: // 0xFF
                {
                    var metaEventType = ((MetaMessage)midiEvent.Msg).Type;

                    if (metaEventType == MetaMessageType.EndOfTrack)
                    {
                        trackEvent.Type = EventType.EndOfTrack;
                        trackEvent.Param1 = 0;
                        trackEvent.Param2 = 0;
                        return true;
                    }

                    return false;
                }
            default:
                {
                    throw new InvalidDataException("This track event is invalid and can't be used.");
                }
        }
    }

    internal void ReadTrackEvents()
    {
        StartTrack();

        _trackEvents.Clear();

        _minNote = 0xFF;
        _maxNote = 0;

        for (IMIDIEvent ev = _midiTracks![MP2KConverter.Instance!.Engine.AGBTrack].First!; ev! != null; ev = ev.Next!)
        {
            MP2KEvent evt = new();

            if (ReadTrackEvent(ref evt, ev))
            {
                _trackEvents.Add(evt);

                if (evt.Type == EventType.EndOfTrack)
                {
                    return;
                }
            }
        }
    }

    internal List<MP2KEvent> MergeEvents()
    {
        List<MP2KEvent> events = [];

        int trackEventPos = 0;
        int seqEventPos = 0;

        while (_trackEvents[trackEventPos].Type != EventType.EndOfTrack
        && _seqEvents[seqEventPos].Type != EventType.EndOfTrack)
        {
            if (MP2KEventComparer.EventCompare(_trackEvents[trackEventPos], _seqEvents[seqEventPos]))
            {
                events.Add(_trackEvents[trackEventPos++]);
            }
            else
            {
                events.Add(_seqEvents[seqEventPos++]);
            }
        }

        while (_trackEvents[trackEventPos].Type != EventType.EndOfTrack)
        {
            events.Add(_trackEvents[trackEventPos++]);
        }

        while (_seqEvents[seqEventPos].Type != EventType.EndOfTrack)
        {
            events.Add(_seqEvents[seqEventPos++]);
        }

        // Push the EndOfTrack event with the larger time.
        if (MP2KEventComparer.EventCompare(_trackEvents[trackEventPos], _seqEvents[seqEventPos]))
        {
            events.Add(_seqEvents[seqEventPos]);
        }
        else
        {
            events.Add(_trackEvents[trackEventPos]);
        }

        return events;
    }

    internal static List<MP2KEvent> InsertTimingEvents(List<MP2KEvent> inEvents)
    {
        List<MP2KEvent> outEvents = [];

        MP2KEvent timingEvent = new()
        {
            Time = 0,
            Type = EventType.TimeSignature,
            Param2 = 96 * MP2KConverter.Instance!.ClocksPerBeat
        };

        foreach (MP2KEvent evt in inEvents)
        {
            while (MP2KEventComparer.EventCompare(timingEvent, evt))
            {
                outEvents.Add(timingEvent);
                timingEvent.Time += timingEvent.Param2;
            }

            if (evt.Type == EventType.TimeSignature)
            {
                if (MP2KConverter.Instance!.Engine.AGBTrack == 1 && evt.Param2 != timingEvent.Param2)
                {
                    MP2KEvent originalTimingEvent = evt;
                    originalTimingEvent.Type = EventType.OriginalTimeSignature;
                    outEvents.Add(originalTimingEvent);
                }
                timingEvent.Param2 = evt.Param2;
                timingEvent.Time = evt.Time + timingEvent.Param2;
            }

            outEvents.Add(evt);
        }

        return outEvents;
    }
}
