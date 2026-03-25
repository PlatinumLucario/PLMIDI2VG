namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KEngine
{
    internal int AGBTrack;

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

    private static void ResetTrackVars()
    {
        _lastVelocity = -1;
        _lastNote = -1;
        _velocityChanged = false;
        _noteChanged = false;
        _keepLastOpName = false;
        _lastOpName = "";
        _inPattern = false;
    }

    internal static void WriteAGBHeader()
    {
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.include \"MPlayDef.s\"\n");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_grp, voicegroup{MP2KConverter.Instance!.VoiceGroupLabel}");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_pri, {MP2KConverter.Instance!.Priority}");
        if (MP2KConverter.Instance!.Reverb >= 0)
        {
            MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_rev, reverb_set{MP2KConverter.Instance!.Reverb:+#;-#;+0}");
        }
        else
        {
            MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_rev, 0");
        }
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_mvl, {MP2KConverter.Instance!.MasterVolume}");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_key, {0}");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_tbs, {MP2KConverter.Instance!.ClocksPerBeat}");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_exg, {Convert.ToByte(MP2KConverter.Instance!.ExactGateTimeEnabled)}");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.equ\t{MP2KConverter.Instance!.ASMFileLabel}_cmp, {Convert.ToByte(MP2KConverter.Instance!.CompressionEnabled)}");

        MP2KConverter.Instance!.ASMFileOutput.Add($"\n\t.section .rodata");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.global\t{MP2KConverter.Instance!.ASMFileLabel}");

        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.align\t2");
    }

    internal static void WriteAGBFooter()
    {
        int trackCount = MP2KConverter.Instance!.Engine.AGBTrack - 1;
        MP2KConverter.Instance!.ASMFileOutput.Add("\n@******************************************************@");
        MP2KConverter.Instance!.ASMFileOutput.Add("\t.align\t2");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\n{MP2KConverter.Instance!.ASMFileLabel}:");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.byte\t{trackCount}\t@ NumTrks");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.byte\t{0}\t@ NumBlks");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.byte\t{MP2KConverter.Instance!.ASMFileLabel}_pri\t@ Priority");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.byte\t{MP2KConverter.Instance!.ASMFileLabel}_rev\t@ Reverb.");
        MP2KConverter.Instance!.ASMFileOutput.Add("");
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t.word\t{MP2KConverter.Instance!.ASMFileLabel}_grp");
        MP2KConverter.Instance!.ASMFileOutput.Add("");

        // Track pointers
        for (int i = 1; i <= trackCount; i++)
        {
            MP2KConverter.Instance!.ASMFileOutput.Add($"\t.word\t{MP2KConverter.Instance!.ASMFileLabel}_{i}");
        }

        MP2KConverter.Instance!.ASMFileOutput.Add("\n\t.end");
    }

    internal void WriteAGBTrack(List<MP2KEvent> events)
    {
        MP2KConverter.Instance!.ASMFileOutput.Add($"\n@**************** Track {AGBTrack} (Midi-Chn.{MP2KConverter.Instance!.MIDI.MIDIChannel + 1}) ****************@\n");
        MP2KConverter.Instance!.ASMFileOutput.Add($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}:");

        int wholeNoteCount = 0;
        int loopEndBlockNum = 0;

        ResetTrackVars();

        bool foundVolBeforeNote = false;

        foreach (MP2KEvent evt in events)
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
            WriteValue($"\tVOL   , 127*{MP2KConverter.Instance!.ASMFileLabel}_mvl/mxv");
        }

        WriteWait(MP2KConverter.Instance!.MIDI.InitialWait);
        WriteValue($"KEYSH , {MP2KConverter.Instance!.ASMFileLabel}_key{0:+#;-#;+0}");

        for (int i = 0; events[i].Type != EventType.EndOfTrack; i++)
        {
            MP2KEvent evt = events[i];

            if (MP2KUtils.IsPatternBoundary(evt.Type))
            {
                if (_inPattern)
                {
                    WriteValue("PEND");
                }
                _inPattern = false;
            }

            if (evt.Type == EventType.WholeNoteMark || evt.Type == EventType.Pattern)
            {
                MP2KConverter.Instance!.ASMFileOutput.Add($"@ {wholeNoteCount++:D3}   ----------------------------------------");
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
                        WriteWord($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_B{loopEndBlockNum}");
                        WriteSeqLoopLabel(evt);
                        break;
                    }
                case EventType.LoopEndBegin:
                    {
                        WriteValue("GOTO");
                        WriteWord($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_B{loopEndBlockNum}");
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
                            MP2KConverter.Instance!.ASMFileOutput.Add($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_{evt.Param2 & 0x7FFFFFFF:D3}:");
                            ResetTrackVars();
                            _inPattern = true;
                        }
                        WriteWait(evt.Time);
                        break;
                    }
                case EventType.Pattern:
                    {
                        WriteValue("PATT");
                        WriteWord($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_{evt.Param2:D3}");

                        while (!MP2KUtils.IsPatternBoundary(events[i + 1].Type))
                        {
                            i++;
                        }

                        ResetTrackVars();
                        break;
                    }
                case EventType.Tempo:
                    {
                        WriteValue($"TEMPO , {(int)Math.Round(60000000.0f / evt.Param2)}*{MP2KConverter.Instance!.ASMFileLabel}_tbs/2");
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
}
