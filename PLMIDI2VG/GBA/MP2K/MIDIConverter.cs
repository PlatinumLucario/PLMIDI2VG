using Kermalis.MIDI;
using System.Diagnostics;

namespace PlatinumLucario.MIDI.GBA.MP2K;

public sealed class MIDIConverter
{
    // Main
    private MIDIFile _midiFile;
    private string _asmFileLabel;
    private byte _masterVolume;
    private string _voiceGroupLabel;
    private int _priority;
    private int _reverb;
    private int _clocksPerBeat;
    private bool _exactGateTimeEnabled;
    private bool _compressionEnabled;

    // MIDI
    private MIDIHeaderChunk? _midiHeader;
    private MIDIFormat _midiFormat;
    private ushort _midiNumTracks;
    private int _midiChannel;
    private TimeDivisionValue _midiTimeDivision;
    private MIDITrackChunk[]? _midiTracks;
    private int _absoluteTime;
    private int _runningStatus;
    private List<Event> _trackEvents = [];
    private List<Event> _seqEvents = [];
    private int _minNote;
    private int _maxNote;
    private int _blockCount = 0;
    private int _initialWait;

    // AGB
    private int _agbTrack;
    private static string? _lastOpName;
    private static int _blockNum;
    private static bool _keepLastOpName;
    private static int _lastVelocity;
    private static int _lastNote;
    private static bool _velocityChanged;
    private static bool _noteChanged;
    private static bool _inPattern;
    private static int _extendedCommand;
    private static int _memaccOp;
    private static int _memaccParam1;
    private static int _memaccParam2;

    private List<string> _asmFileOutput = [];
    private StreamWriter? _asmFile;
    private readonly int[] _noteDurationLUT =
    [
        0, // 0
        1, // 1
        2, // 2
        3, // 3
        4, // 4
        5, // 5
        6, // 6
        7, // 7
        8, // 8
        9, // 9
        10, // 10
        11, // 11
        12, // 12
        13, // 13
        14, // 14
        15, // 15
        16, // 16
        17, // 17
        18, // 18
        19, // 19
        20, // 20
        21, // 21
        22, // 22
        23, // 23
        24, // 24
        24, // 25
        24, // 26
        24, // 27
        28, // 28
        28, // 29
        30, // 30
        30, // 31
        32, // 32
        32, // 33
        32, // 34
        32, // 35
        36, // 36
        36, // 37
        36, // 38
        36, // 39
        40, // 40
        40, // 41
        42, // 42
        42, // 43
        44, // 44
        44, // 45
        44, // 46
        44, // 47
        48, // 48
        48, // 49
        48, // 50
        48, // 51
        52, // 52
        52, // 53
        54, // 54
        54, // 55
        56, // 56
        56, // 57
        56, // 58
        56, // 59
        60, // 60
        60, // 61
        60, // 62
        60, // 63
        64, // 64
        64, // 65
        66, // 66
        66, // 67
        68, // 68
        68, // 69
        68, // 70
        68, // 71
        72, // 72
        72, // 73
        72, // 74
        72, // 75
        76, // 76
        76, // 77
        78, // 78
        78, // 79
        80, // 80
        80, // 81
        80, // 82
        80, // 83
        84, // 84
        84, // 85
        84, // 86
        84, // 87
        88, // 88
        88, // 89
        90, // 90
        90, // 91
        92, // 92
        92, // 93
        92, // 94
        92, // 95
        96, // 96
    ];
    private readonly int[] _noteVelocityLUT =
    [
        0, // 0
        4, // 1
        4, // 2
        4, // 3
        4, // 4
        8, // 5
        8, // 6
        8, // 7
        8, // 8
        12, // 9
        12, // 10
        12, // 11
        12, // 12
        16, // 13
        16, // 14
        16, // 15
        16, // 16
        20, // 17
        20, // 18
        20, // 19
        20, // 20
        24, // 21
        24, // 22
        24, // 23
        24, // 24
        28, // 25
        28, // 26
        28, // 27
        28, // 28
        32, // 29
        32, // 30
        32, // 31
        32, // 32
        36, // 33
        36, // 34
        36, // 35
        36, // 36
        40, // 37
        40, // 38
        40, // 39
        40, // 40
        44, // 41
        44, // 42
        44, // 43
        44, // 44
        48, // 45
        48, // 46
        48, // 47
        48, // 48
        52, // 49
        52, // 50
        52, // 51
        52, // 52
        56, // 53
        56, // 54
        56, // 55
        56, // 56
        60, // 57
        60, // 58
        60, // 59
        60, // 60
        64, // 61
        64, // 62
        64, // 63
        64, // 64
        68, // 65
        68, // 66
        68, // 67
        68, // 68
        72, // 69
        72, // 70
        72, // 71
        72, // 72
        76, // 73
        76, // 74
        76, // 75
        76, // 76
        80, // 77
        80, // 78
        80, // 79
        80, // 80
        84, // 81
        84, // 82
        84, // 83
        84, // 84
        88, // 85
        88, // 86
        88, // 87
        88, // 88
        92, // 89
        92, // 90
        92, // 91
        92, // 92
        96, // 93
        96, // 94
        96, // 95
        96, // 96
        100, // 97
        100, // 98
        100, // 99
        100, // 100
        104, // 101
        104, // 102
        104, // 103
        104, // 104
        108, // 105
        108, // 106
        108, // 107
        108, // 108
        112, // 109
        112, // 110
        112, // 111
        112, // 112
        116, // 113
        116, // 114
        116, // 115
        116, // 116
        120, // 117
        120, // 118
        120, // 119
        120, // 120
        124, // 121
        124, // 122
        124, // 123
        124, // 124
        127, // 125
        127, // 126
        127, // 127
    ];
    private readonly string[] _noteTable =
    [
        "Cn",
        "Cs",
        "Dn",
        "Ds",
        "En",
        "Fn",
        "Fs",
        "Gn",
        "Gs",
        "An",
        "As",
        "Bn",
    ];
    private readonly string[] _minusNoteTable =
    [
        "CnM",
        "CsM",
        "DnM",
        "DsM",
        "EnM",
        "FnM",
        "FsM",
        "GnM",
        "GsM",
        "AnM",
        "AsM",
        "BnM",
    ];
    private enum MIDIEventCategory
    {
        Control,
        SysEx,
        Meta,
        Invalid,
    }
    internal enum EventType
    {
        EndOfTie = 0x01,
        Label = 0x11,
        LoopEnd = 0x12,
        LoopEndBegin = 0x13,
        LoopBegin = 0x14,
        OriginalTimeSignature = 0x15,
        WholeNoteMark = 0x16,
        Pattern = 0x17,
        TimeSignature = 0x18,
        Tempo = 0x19,
        InstrumentChange = 0x21,
        Controller = 0x22,
        PitchBend = 0x23,
        KeyShift = 0x31,
        Note = 0x40,
        TimeSplit = 0xFE,
        EndOfTrack = 0xFF,
    }
    public MIDIConverter(
        MIDIFile midiFile, string asmFileLabel = "output_file",
        byte masterVolume = 127, string voiceGroupLabel = "_dummy",
        int priority = 0, int reverb = -1,
        int clocksPerBeat = 1, bool exactGateTimeEnabled = false,
        bool compressionEnabled = true
        )
    {
        _midiFile = midiFile;
        _asmFileLabel = asmFileLabel;
        _masterVolume = masterVolume;
        _voiceGroupLabel = voiceGroupLabel;
        _priority = priority;
        _reverb = reverb;
        _clocksPerBeat = clocksPerBeat;
        _exactGateTimeEnabled = exactGateTimeEnabled;
        _compressionEnabled = compressionEnabled;

        ReadMIDIFileHeader();
        WriteAGBHeader();
        ReadMIDITracks();
        WriteAGBFooter();
    }

    internal struct Event
    {
        internal int Time;
        internal EventType Type;
        internal byte Note;
        internal byte Param1;
        internal int Param2;
    };

    public void SaveAsASM(string filePath)
    {
        _asmFile = new StreamWriter(filePath);

        foreach (string line in _asmFileOutput)
        {
            _asmFile.WriteLine(line);
        }

        _asmFile.Flush();
        _asmFile.Close();
    }

    private void ReadMIDIFileHeader()
    {
        _midiHeader = _midiFile.HeaderChunk;
        _midiFormat = _midiHeader.Format;
        _midiNumTracks = _midiHeader.NumTracks;
        _midiTimeDivision = _midiHeader.TimeDivision;
    }

    private void ReadMIDITracks()
    {
        _midiTracks = new MIDITrackChunk[_midiFile.EnumerateTrackChunks().Count()];
        _midiTracks = [.. _midiFile.EnumerateTrackChunks()];
        ReadSeqEvents();

        _agbTrack = 1;

        for (int midiTrack = 0; midiTrack < _midiTracks.Length - 1; midiTrack++, _midiChannel++)
        {
            ReadTrackEvents();

            if (_minNote != 0xFF)
            {
#if DEBUG
                Debug.WriteLine($"Track{_agbTrack} = Midi-Ch.{_midiChannel + 1}");
#endif

                // Remove TEMPO from all tracks except track 1
                if (_agbTrack == 2)
                {
                    _seqEvents.RemoveAll(e => e.Type == EventType.Tempo);
                }

                List<Event> ev = MergeEvents();

                ConvertTimes(ev);
                ev = InsertTimingEvents(ev);
                ev = CreateTies(ev);
                ev.Sort(0, ev.Count, new EventCompare());
                ev = SplitTime(ev);
                CalculateWaits(ev);

                if (_compressionEnabled)
                {
                    Compress(ev);
                }

                WriteAGBTrack(ev);

                _agbTrack++;
            }
        }
    }

    private void WriteAGBTrack(List<Event> events)
    {
        _asmFileOutput.Add($"\n@**************** Track {_agbTrack} (Midi-Chn.{_midiChannel + 1}) ****************@\n");
        _asmFileOutput.Add($"{_asmFileLabel}_{_agbTrack}:");

        int wholeNoteCount = 0;
        int loopEndBlockNum = 0;

        ResetTrackVars();

        bool foundVolBeforeNote = false;

        foreach (Event evt in events)
        {
            if (evt.Type == EventType.Note)
            {
                break;
            }

            if (evt.Type == EventType.Controller && evt.Param1 == 0x07)
            {
                foundVolBeforeNote = true;
            }
        }

        if (!foundVolBeforeNote)
        {
            WriteValue($"\tVOL   , 127*{_asmFileLabel}_mvl/mxv");
        }

        WriteWait(_initialWait);
        WriteValue($"KEYSH , {_asmFileLabel}_key{0:+#;-#;+0}");

        for (int i = 0; events[i].Type != EventType.EndOfTrack; i++)
        {
            Event evt = events[i];

            if (IsPatternBoundary(evt.Type))
            {
                if (_inPattern)
                {
                    WriteValue("PEND");
                }
                _inPattern = false;
            }

            if (evt.Type == EventType.WholeNoteMark || evt.Type == EventType.Pattern)
            {
                _asmFileOutput.Add($"@ {wholeNoteCount++:D3}   ----------------------------------------");
            }

            switch (evt.Type)
            {
                case EventType.Note:
                    {
                        WriteNote(evt);
                        break;
                    }
                case EventType.EndOfTie:
                    {
                        WriteEndOfTieOp(evt);
                        break;
                    }
                case EventType.Label:
                    {
                        WriteSeqLoopLabel(evt);
                        break;
                    }
                case EventType.LoopEnd:
                    {
                        WriteValue("GOTO");
                        WriteWord($"{_asmFileLabel}_{_agbTrack}_B{loopEndBlockNum}");
                        WriteSeqLoopLabel(evt);
                        break;
                    }
                case EventType.LoopEndBegin:
                    {
                        WriteValue("GOTO");
                        WriteWord($"{_asmFileLabel}_{_agbTrack}_B{loopEndBlockNum}");
                        WriteSeqLoopLabel(evt);
                        loopEndBlockNum = _blockNum;
                        break;
                    }
                case EventType.LoopBegin:
                    {
                        WriteSeqLoopLabel(evt);
                        loopEndBlockNum = _blockNum;
                        break;
                    }
                case EventType.WholeNoteMark:
                    {
                        if ((evt.Param2 & 0x80000000) != 0)
                        {
                            _asmFileOutput.Add($"{_asmFileLabel}_{_agbTrack}_{evt.Param2 & 0x7FFFFFFF:D3}:");
                            ResetTrackVars();
                            _inPattern = true;
                        }
                        WriteWait(evt.Time);
                        break;
                    }
                case EventType.Pattern:
                    {
                        WriteValue("PATT");
                        WriteWord($"{_asmFileLabel}_{_agbTrack}_{evt.Param2:D3}");

                        while (!IsPatternBoundary(events[i + 1].Type))
                        {
                            i++;
                        }

                        ResetTrackVars();
                        break;
                    }
                case EventType.Tempo:
                    {
                        WriteValue($"TEMPO , {(int)Math.Round(60000000.0f / evt.Param2)}*{_asmFileLabel}_tbs/2");
                        WriteWait(evt.Time);
                        break;
                    }
                case EventType.InstrumentChange:
                    {
                        WriteOp(evt.Time, "VOICE ", $"{evt.Param1}");
                        break;
                    }
                case EventType.PitchBend:
                    {
                        WriteOp(evt.Time, "BEND  ", $"c_v{evt.Param2 - 64:+#;-#;+0}");
                        break;
                    }
                case EventType.Controller:
                    {
                        WriteControllerOp(evt);
                        break;
                    }
                default:
                    {
                        WriteWait(evt.Time);
                        break;
                    }
            }
        }

        WriteValue("FINE");
    }

    private void WriteControllerOp(Event evt)
    {
        switch (evt.Param1)
        {
            case 0x01:
                {
                    WriteOp(evt.Time, "MOD   ", $"{evt.Param2}");
                    break;
                }
            case 0x07:
                {
                    WriteOp(evt.Time, "VOL   ", $"{evt.Param2}*{_asmFileLabel}_mvl/mxv");
                    break;
                }
            case 0x0A:
                {
                    WriteOp(evt.Time, "PAN   ", $"c_v{evt.Param2 - 64:+#;-#;+0}");
                    break;
                }
            case 0x0C:
            case 0x10:
                {
                    WriteMemAcc(evt);
                    break;
                }
            case 0x0D:
                {
                    _memaccOp = evt.Param2;
                    WriteWait(evt.Time);
                    break;
                }
            case 0x0E:
                {
                    _memaccParam1 = evt.Param2;
                    WriteWait(evt.Time);
                    break;
                }
            case 0x0F:
                {
                    _memaccParam2 = evt.Param2;
                    WriteWait(evt.Time);
                    break;
                }
            case 0x11:
                {
                    _asmFileOutput.Add($"{_asmFileLabel}_{_agbTrack}_L{evt.Param2}:");
                    WriteWait(evt.Time);
                    ResetTrackVars();
                    break;
                }
            case 0x14:
                {
                    WriteOp(evt.Time, "BENDR ", $"{evt.Param2}");
                    break;
                }
            case 0x15:
                {
                    WriteOp(evt.Time, "LFOS  ", $"{evt.Param2}");
                    break;
                }
            case 0x16:
                {
                    WriteOp(evt.Time, "MODT  ", $"{evt.Param2}");
                    break;
                }
            case 0x18:
                {
                    WriteOp(evt.Time, "TUNE  ", $"c_v{evt.Param2 - 64:+#;-#;+0}");
                    break;
                }
            case 0x1A:
                {
                    WriteOp(evt.Time, "LFODL ", $"{evt.Param2}");
                    break;
                }
            case 0x1D:
            case 0x1F:
                {
                    WriteExtendedOp(evt);
                    break;
                }
            case 0x1E:
                {
                    _extendedCommand = evt.Param2;
                    // TODO: Loop the operator (and is also a todo in the mid2agb decomp)
                    break;
                }
            case 0x21:
            case 0x27:
                {
                    WriteValue($"PRIO  , {evt.Param2}");
                    WriteWait(evt.Time);
                    break;
                }
            default:
                {
                    WriteWait(evt.Time);
                    break;
                }
        }
    }

    private void WriteExtendedOp(Event evt)
    {
        // TODO (from mid2agb decomp): support for other extended commands

        switch (_extendedCommand)
        {
            case 0x08:
                {
                    WriteOp(evt.Time, "XCMD  ", $"xIECV , {evt.Param2}");
                    break;
                }
            case 0x09:
                {
                    WriteOp(evt.Time, "XCMD  ", $"xIECL , {evt.Param2}");
                    break;
                }
            default:
                {
                    WriteWait(evt.Time);
                    break;
                }
        }
    }

    private void WriteMemAcc(Event evt)
    {
        switch (_memaccOp)
        {
            case 0x00:
                {
                    WriteValue($"MEMACC, mem_set, 0x{_memaccParam1:X2}, {evt.Param2}");
                    break;
                }
            case 0x01:
                {
                    WriteValue($"MEMACC, mem_add, 0x{_memaccParam1:X2}, {evt.Param2}");
                    break;
                }
            case 0x02:
                {
                    WriteValue($"MEMACC, mem_sub, 0x{_memaccParam1:X2}, {evt.Param2}");
                    break;
                }
            case 0x03:
                {
                    WriteValue($"MEMACC, mem_mem_set, 0x{_memaccParam1:X2}, 0x{evt.Param2:X2}");
                    break;
                }
            case 0x04:
                {
                    WriteValue($"MEMACC, mem_mem_add, 0x{_memaccParam1:X2}, 0x{evt.Param2:X2}");
                    break;
                }
            case 0x05:
                {
                    WriteValue($"MEMACC, mem_mem_sub, 0x{_memaccParam1:X2}, 0x{evt.Param2:X2}");
                    break;
                }
            // TODO (mid2agb decomp): everything else
            case 0x06:
            case 0x07:
            case 0x08:
            case 0x09:
            case 0x0A:
            case 0x0B:
            case 0x0C:
            case 0x0D:
            case 0x0E:
            case 0x0F:
            case 0x10:
            case 0x11:
            case 0x46:
            case 0x47:
            case 0x48:
            case 0x49:
            case 0x4A:
            case 0x4B:
            case 0x4C:
            case 0x4D:
            case 0x4E:
            case 0x4F:
            case 0x50:
            case 0x51:
            default:
                break;
        }
    }

    private void WriteWord(string format)
    {
        _asmFileOutput.Add($"\t .word\t" + format);
    }

    private void WriteSeqLoopLabel(Event evt)
    {
        _blockNum = evt.Param1 + 1;
        _asmFileOutput.Add($"{_asmFileLabel}_{_agbTrack}_B{_blockNum}:");
        WriteWait(evt.Time);
        ResetTrackVars();
    }

    private void WriteEndOfTieOp(Event evt)
    {
        int note = evt.Note;
        bool noteChanged = (note != _lastNote);

        if (!noteChanged || !_noteChanged)
        {
            _lastOpName = "";
        }

        if (!noteChanged && _compressionEnabled)
        {
            WriteOp(evt.Time, "EOT   ");
        }
        else
        {
            _lastNote = note;
            if (note >= 24)
            {
                WriteOp(evt.Time, "EOT   ", _noteTable[note % 12] + $"{note / 12 - 2:D1}");
            }
            else
            {
                WriteOp(evt.Time, "EOT   ", _minusNoteTable[note % 12] + $"{note / -12 + 2:D1}");
            }
        }

        _noteChanged = noteChanged;
    }

    private void WriteNote(Event evt)
    {
        int note = evt.Note;
        int velocity = _noteVelocityLUT[evt.Param1];
        int duration = -1;

        if (evt.Param2 != -1)
        {
            duration = _noteDurationLUT[evt.Param2];
        }

        int gateTimeParam = 0;

        if (_exactGateTimeEnabled && duration != -1)
        {
            gateTimeParam = evt.Param2 - duration;
        }

        string gtpBuf = gateTimeParam > 0 ? $", gtp{gateTimeParam}" : "";

        string opName = duration == -1 ? "TIE   " : $"N{duration:D2}   ";

        bool noteChanged = true;
        bool velocityChanged = true;

        if (_compressionEnabled)
        {
            noteChanged = (note != _lastNote);
            velocityChanged = (velocity != _lastVelocity);
        }

        if (_keepLastOpName)
        {
            _keepLastOpName = false;
        }
        else
        {
            _lastOpName = "";
        }

        if (noteChanged || velocityChanged || (gateTimeParam > 0))
        {
            _lastNote = note;

            string noteBuf = note >= 24 ? _noteTable[note % 12] + $"{(note / 12) - 2:D1} " : _minusNoteTable[note % 12] + $"{(note / -12) + 2:D1}";

            string velocityBuf;

            if (velocityChanged || (gateTimeParam > 0))
            {
                _lastVelocity = velocity;
                velocityBuf = $", v{velocity:D3}";
            }
            else
            {
                velocityBuf = "";
            }

            WriteOp(evt.Time, opName, $"{noteBuf}{velocityBuf}{gtpBuf}");
        }
        else
        {
            WriteOp(evt.Time, opName);
        }

        _noteChanged = noteChanged;
        _velocityChanged = velocityChanged;
    }

    private void WriteOp(int wait, string name, string format = null!)
    {
        string line = "\t.byte\t\t";

        if (format != null)
        {
            if (!_compressionEnabled || _lastOpName != name)
            {
                line += $"{name}, ";
                _lastOpName = name;
            }
            else
            {
                line += "        ";
            }
            line += format;
        }
        else
        {
            line += name;
            _lastOpName = name;
        }

        _asmFileOutput.Add(line);

        WriteWait(wait);
    }

    private void WriteWait(int wait)
    {
        if (wait > 0)
        {
            _asmFileOutput.Add($"\t.byte\tW{wait:D2}");
            _velocityChanged = true;
            _noteChanged = true;
            _keepLastOpName = true;
        }
    }

    private void WriteValue(string format)
    {
        _asmFileOutput.Add("\t.byte\t" + format);
        _velocityChanged = true;
        _noteChanged = true;
        _keepLastOpName = true;
    }

    private void ResetTrackVars()
    {
        _lastVelocity = -1;
        _lastNote = -1;
        _velocityChanged = false;
        _noteChanged = false;
        _keepLastOpName = false;
        _lastOpName = "";
        _inPattern = false;
    }

    private void CalculateWaits(List<Event> events)
    {
        _initialWait = events[0].Time;
        int wholeNoteCount = 0;

        for (int i = 0; i < events.Count && events[i].Type != EventType.EndOfTrack; i++)
        {
            Event evt = events[i];
            evt.Time = events[i + 1].Time - events[i].Time;

            if (events[i].Type == EventType.TimeSignature)
            {
                evt.Type = EventType.WholeNoteMark;
                evt.Param2 = wholeNoteCount++;
            }

            events[i] = evt;
        }
    }

    private List<Event> SplitTime(List<Event> inEvents)
    {
        List<Event> outEvents = [];

        int time = 0;

        foreach (Event evt in inEvents)
        {
            int diff = evt.Time - time;

            if (diff > 96)
            {
                int wholeNoteCount = (diff - 1) / 96;
                diff -= 96 * wholeNoteCount;

                for (int i = 0; i < wholeNoteCount; i++)
                {
                    time += 96;
                    Event timeSplitEvent = new()
                    {
                        Time = time,
                        Type = EventType.TimeSplit
                    };
                    outEvents.Add(timeSplitEvent);
                }
            }

            int lutValue = _noteDurationLUT[diff];

            if (lutValue != diff)
            {
                Event timeSplitEvent = new()
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

    private List<Event> CreateTies(List<Event> inEvents)
    {
        List<Event> outEvents = [];

        foreach (Event evt in inEvents)
        {
            if (evt.Type == EventType.Note && evt.Param2 > 96)
            {
                Event tieEvent = evt;
                tieEvent.Param2 = -1;
                outEvents.Add(tieEvent);

                Event eotEvent = new()
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

    private List<Event> InsertTimingEvents(List<Event> inEvents)
    {
        List<Event> outEvents = [];

        Event timingEvent = new()
        {
            Time = 0,
            Type = EventType.TimeSignature,
            Param2 = 96 * _clocksPerBeat
        };

        foreach (Event evt in inEvents)
        {
            while (new EventCompare().Compare(timingEvent, evt) == -1)
            {
                outEvents.Add(timingEvent);
                timingEvent.Time += timingEvent.Param2;
            }

            if (evt.Type == EventType.TimeSignature)
            {
                if (_agbTrack == 1 && evt.Param2 != timingEvent.Param2)
                {
                    Event originalTimingEvent = evt;
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

    private void ReadTrackEvents()
    {
        StartTrack();

        _trackEvents.Clear();

        _minNote = 0xFF;
        _maxNote = 0;

        for (IMIDIEvent ev = _midiTracks![_agbTrack].First!; ev! != null; ev = ev.Next!)
        {
            Event evt = new();

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

    private bool ReadTrackEvent(ref Event trackEvent, IMIDIEvent midiEvent)
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
                                trackEvent.Type = EventType.Controller;
                                trackEvent.Param1 = (byte)programChange.Program;
                                trackEvent.Param2 = 0;
                                break;
                            }
                        case PitchBendMessage pitchBend: // 0xE_
                            {
                                trackEvent.Type = EventType.Controller;
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

    private void FindNoteEnd(ref Event trackEvent, IMIDIEvent midiEvent)
    {
        int savedRunningStatus = _runningStatus;

        trackEvent.Param2 = 0;

        while (!CheckNoteEnd(ref trackEvent, midiEvent))
        {
            if (midiEvent.Next is not null)
            {
                midiEvent = midiEvent.Next;
            }
        }

        _runningStatus = savedRunningStatus;
    }

    private bool CheckNoteEnd(ref Event trackEvent, IMIDIEvent midiEvent)
    {
        trackEvent.Param2 += midiEvent.DeltaTicks;

        DetermineEventCategory(midiEvent, out MIDIEventCategory category);

        switch (category)
        {
            case MIDIEventCategory.Control:
                {
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

    private List<Event> MergeEvents()
    {
        List<Event> events = [];

        int trackEventPos = 0;
        int seqEventPos = 0;

        while (_trackEvents[trackEventPos].Type != EventType.EndOfTrack
        && _seqEvents[seqEventPos].Type != EventType.EndOfTrack)
        {
            if (new EventCompare().Compare(_trackEvents[trackEventPos], _seqEvents[seqEventPos]) == -1)
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
        if (new EventCompare().Compare(_trackEvents[trackEventPos], _seqEvents[seqEventPos]) == 1)
        {
            events.Add(_seqEvents[seqEventPos]);
        }
        else
        {
            events.Add(_trackEvents[trackEventPos]);
        }

        return events;
    }

    internal class EventCompare : IComparer<Event>
    {
        public int Compare(Event event1, Event event2)
        {
            if (event1.Time < event2.Time)
            {
                return -1;
            }

            if (event1.Time > event2.Time)
            {
                return 1;
            }

            int event1Type = (int)event1.Type;
            int event2Type = (int)event2.Type;

            if (event1.Type == EventType.Note)
            {
                event1Type += event1.Note;
            }

            if (event2.Type == EventType.Note)
            {
                event2Type += event2.Note;
            }

            if (event1Type < event2Type)
            {
                return -1;
            }

            if (event1Type > event2Type)
            {
                return 1;
            }

            if (event1.Type == EventType.EndOfTie)
            {
                if (event1.Note < event2.Note)
                {
                    return -1;
                }

                if (event1.Note > event2.Note)
                {
                    return 1;
                }
            }

            return 0;
        }
    }

    private void Compress(List<Event> events)
    {
        for (int i = 0; events[i].Type != EventType.EndOfTrack; i++)
        {
            while (events[i].Type != EventType.WholeNoteMark)
            {
                i++;

                if (events[i].Type == EventType.EndOfTrack)
                {
                    return;
                }
            }

            if (CalculateCompressionScore(events, i) >= 6)
            {
                CompressWholeNote(events, i);
            }
        }
    }

    private void CompressWholeNote(List<Event> events, int index)
    {
        for (int j = index + 1; events[j].Type != EventType.EndOfTrack; j++)
        {
            while (events[j].Type != EventType.WholeNoteMark)
            {
                j++;

                if (events[j].Type == EventType.EndOfTrack)
                {
                    return;
                }
            }

            if (IsCompressionMatch(events, index, j))
            {
                Event evt1 = events[j];
                Event evt2 = events[index];
                evt1.Type = EventType.Pattern;
                evt1.Param2 = events[index].Param2 & 0x7FFFFFFF;
                evt2.Param2 = (int)(events[index].Param2 | 0x80000000);
                events[j] = evt1;
                events[index] = evt2;
            }
        }
    }

    private bool IsCompressionMatch(List<Event> events, int index1, int index2)
    {
        if (events[index1].Type != events[index2].Type ||
            events[index1].Note != events[index2].Note ||
            events[index1].Param1 != events[index2].Param1 ||
            events[index1].Time != events[index2].Time)
        {
            return false;
        }

        index1++;
        index2++;

        do
        {
            if (events[index1].Type != events[index2].Type &&
                events[index1].Note != events[index2].Note &&
                events[index1].Param1 != events[index2].Param1 &&
                events[index1].Time != events[index2].Time)
            {
                return false;
            }

            index1++;
            index2++;
            if (index2 >= events.Count)
            {
                return false;
            }
        } while (!IsPatternBoundary(events[index1].Type));

        return IsPatternBoundary(events[index2].Type);
    }

    private int CalculateCompressionScore(List<Event> events, int index)
    {
        int score = 0;
        int lastParam1 = events[index].Param1;
        int lastVelocity = 0x80;
        EventType lastType = events[index].Type;
        uint lastDuration = 0x80000000;
        int lastNote = 0x40;

        if (events[index].Time > 0)
        {
            score++;
        }

        for (int i = index + 1; !IsPatternBoundary(events[i].Type); i++)
        {
            if (events[i].Type == EventType.Note)
            {
                int val = 0;

                if (events[i].Note != lastNote)
                {
                    val++;
                    lastNote = events[i].Note;
                }

                if (events[i].Param1 != lastVelocity)
                {
                    val++;
                    lastVelocity = events[i].Param1;
                }

                int duration = events[i].Param2;

                if (duration >= 0)
                {
                    if (_noteDurationLUT[duration] != lastDuration)
                    {
                        val++;
                        lastDuration = (uint)_noteDurationLUT[duration];
                    }
                }
                else
                {
                    val++;
                    lastDuration = (uint)duration;
                }

                if (duration != lastDuration)
                {
                    val++;
                }

                if (val == 0)
                {
                    val = 1;
                }

                score += val;
            }
            else
            {
                lastDuration = 0x80000000;

                if (events[i].Type == lastType)
                {
                    if ((lastType != EventType.Controller && (int)lastType != 0x25 && lastType != EventType.EndOfTie) || events[i].Param1 == lastParam1)
                    {
                        score++;
                    }
                    else
                    {
                        score += 2;
                    }
                }
                else
                {
                    score += 2;
                }
            }

            lastParam1 = events[i].Param1;
            lastType = events[i].Type;

            if (events[i].Time != 0)
            {
                score++;
            }
        }

        return score;
    }

    private bool IsPatternBoundary(EventType type)
    {
        return type == EventType.EndOfTrack || (int)type <= 0x17;
    }

    private void ConvertTimes(List<Event> midiEvents)
    {
        for (int i = 0; i < midiEvents.Count; i++)
        {
            Event ev = midiEvents[i];
            ev.Time = (24 * _clocksPerBeat * midiEvents[i].Time) / _midiTimeDivision.PPQN_TicksPerQuarterNote;

            if (midiEvents[i].Type is EventType.Note)
            {
                ev.Param1 = (byte)_noteVelocityLUT[midiEvents[i].Param1];

                int duration = (24 * _clocksPerBeat * midiEvents[i].Param2) / _midiTimeDivision.PPQN_TicksPerQuarterNote;

                if (duration == 0)
                {
                    duration = 1;
                }

                if (!_exactGateTimeEnabled && duration < 96)
                {
                    duration = _noteDurationLUT[duration];
                }

                ev.Param2 = duration;
                midiEvents[i] = ev;
            }
        }
    }

    private void ReadSeqEvents()
    {
        StartTrack();

        for (IMIDIEvent ev = _midiTracks![_agbTrack].First!; ev != null; ev = ev.Next!)
        {
            Event evt = new();

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

    private bool ReadSeqEvent(ref Event seqEvent, IMIDIEvent midiEvent)
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
                                    case "[":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.LoopBegin);
                                            break;
                                        }
                                    case "][":
                                        {
                                            MakeBlockEvent(ref seqEvent, EventType.LoopEndBegin);
                                            break;
                                        }
                                    case "]":
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
                                            return false;
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
                                if (((MetaMessage)midiEvent.Msg).Data.Length != 3)
                                {
                                    throw new InvalidDataException("Invalid tempo size.");
                                }

                                ((MetaMessage)midiEvent.Msg).ReadTempoMessage(out uint msPQN, out decimal bPM);
                                seqEvent.Type = EventType.Tempo;
                                seqEvent.Param1 = 0;
                                seqEvent.Param2 = (int)msPQN;
                                break;
                            }
                        case MetaMessageType.TimeSignature:
                            {
                                if (((MetaMessage)midiEvent.Msg).Data.Length != 4)
                                {
                                    throw new InvalidDataException("Invalid time signature size.");
                                }

                                ((MetaMessage)midiEvent.Msg).ReadTimeSignatureMessage(out byte numerator, out byte denominatorExponent, out byte clocksPerMetronomeClick, out byte num32ndNotesPerQuarterNote);

                                if (denominatorExponent >= 16)
                                {
                                    throw new InvalidDataException("Invalid time signature denominator.");
                                }

                                int clockTicks = 96 * numerator * _clocksPerBeat;
                                int denominator = 1 << denominatorExponent;
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

    private void MakeBlockEvent(ref Event midiEvent, EventType type)
    {
        midiEvent.Type = type;
        midiEvent.Param1 = (byte)_blockCount++;
        midiEvent.Param2 = 0;
    }

    private void DetermineEventCategory(IMIDIEvent midiEvent, out MIDIEventCategory category)
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

    private void StartTrack()
    {
        _absoluteTime = 0;
        _runningStatus = 0;
    }

    private void WriteAGBHeader()
    {
        _asmFileOutput.Add($"\t.include \"MPlayDef.s\"\n");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_grp, voicegroup{_voiceGroupLabel}");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_pri, {_priority}");
        if (_reverb >= 0)
        {
            _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_rev, reverb_set{_reverb:+#;-#;+0}");
        }
        else
        {
            _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_rev, 0");
        }
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_mvl, {_masterVolume}");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_key, {0}");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_tbs, {_clocksPerBeat}");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_exg, {Convert.ToByte(_exactGateTimeEnabled)}");
        _asmFileOutput.Add($"\t.equ\t{_asmFileLabel}_cmp, {Convert.ToByte(_compressionEnabled)}");

        _asmFileOutput.Add($"\n\t.section .rodata");
        _asmFileOutput.Add($"\t.global\t{_asmFileLabel}");

        _asmFileOutput.Add($"\t.align\t2");
    }

    private void WriteAGBFooter()
    {
        int trackCount = _agbTrack - 1;
        _asmFileOutput.Add("\n@******************************************************@");
        _asmFileOutput.Add("\t.align\t2");
        _asmFileOutput.Add($"\n{_asmFileLabel}:");
        _asmFileOutput.Add($"\t.byte\t{trackCount}\t@ NumTrks");
        _asmFileOutput.Add($"\t.byte\t{0}\t@ NumBlks");
        _asmFileOutput.Add($"\t.byte\t{_asmFileLabel}_pri\t@ Priority");
        _asmFileOutput.Add($"\t.byte\t{_asmFileLabel}_rev\t@ Reverb.");
        _asmFileOutput.Add("");
        _asmFileOutput.Add($"\t.word\t{_asmFileLabel}_grp");
        _asmFileOutput.Add("");

        // Track pointers
        for (int i = 1; i <= trackCount; i++)
        {
            _asmFileOutput.Add($"\t.word\t{_asmFileLabel}_{i}");
        }

        _asmFileOutput.Add("\n\t.end");
    }
}
