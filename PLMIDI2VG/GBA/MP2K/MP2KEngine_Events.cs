namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KEngine
{
    private void WriteControllerOp(MP2KEvent evt)
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
                    WriteOp(evt.Time, "VOL   ", $"{evt.Param2}*{MP2KConverter.Instance!.ASMFileLabel}_mvl/mxv");
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
                    MP2KConverter.Instance!.ASMFileOutput.Add($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_L{evt.Param2}:");
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

    private static void WriteExtendedOp(MP2KEvent evt)
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

    private static void WriteMemAcc(MP2KEvent evt)
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

    private static void WriteWord(string format)
    {
        MP2KConverter.Instance!.ASMFileOutput.Add($"\t .word\t" + format);
    }

    private void WriteSeqLoopLabel(MP2KEvent evt)
    {
        _blockNum = evt.Param1 + 1;
        MP2KConverter.Instance!.ASMFileOutput.Add($"{MP2KConverter.Instance!.ASMFileLabel}_{AGBTrack}_B{_blockNum}:");
        WriteWait(evt.Time);
        ResetTrackVars();
    }

    private static void WriteEndOfTieOp(MP2KEvent evt)
    {
        int note = evt.Note;
        bool noteChanged = (note != _lastNote);

        if (!noteChanged || !_noteChanged)
        {
            _lastOpName = "";
        }

        if (!noteChanged && MP2KConverter.Instance!.CompressionEnabled)
        {
            WriteOp(evt.Time, "EOT   ");
        }
        else
        {
            _lastNote = note;
            if (note >= 24)
            {
                WriteOp(evt.Time, "EOT   ", MP2KUtils.NoteTable[note % 12] + $"{note / 12 - 2:D1} ");
            }
            else
            {
                WriteOp(evt.Time, "EOT   ", MP2KUtils.MinusNoteTable[note % 12] + $"{note / -12 + 2:D1}");
            }
        }

        _noteChanged = noteChanged;
    }

    private static void WriteNote(MP2KEvent evt)
    {
        int note = evt.Note;
        int velocity = MP2KUtils.NoteVelocityLUT[evt.Param1];
        int duration = -1;

        if (evt.Param2 != -1)
        {
            duration = MP2KUtils.NoteDurationLUT[evt.Param2];
        }

        int gateTimeParam = 0;

        if (MP2KConverter.Instance!.ExactGateTimeEnabled && duration != -1)
        {
            gateTimeParam = evt.Param2 - duration;
        }

        string gtpBuf = gateTimeParam > 0 ? $", gtp{gateTimeParam}" : "";

        string opName = duration == -1 ? "TIE   " : $"N{duration:D2}   ";

        bool noteChanged = true;
        bool velocityChanged = true;

        if (MP2KConverter.Instance!.CompressionEnabled)
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

            string noteBuf = note >= 24 ? MP2KUtils.NoteTable[note % 12] + $"{(note / 12) - 2:D1} " : MP2KUtils.MinusNoteTable[note % 12] + $"{(note / -12) + 2:D1}";

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

    private static void WriteOp(int wait, string name, string format = null!)
    {
        string line = "\t.byte\t\t";

        if (format != null)
        {
            if (!MP2KConverter.Instance!.CompressionEnabled || _lastOpName != name)
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

        MP2KConverter.Instance!.ASMFileOutput.Add(line);

        WriteWait(wait);
    }

    private static void WriteWait(int wait)
    {
        if (wait > 0)
        {
            MP2KConverter.Instance!.ASMFileOutput.Add($"\t.byte\tW{wait:D2}");
            _velocityChanged = true;
            _noteChanged = true;
            _keepLastOpName = true;
        }
    }

    private static void WriteValue(string format)
    {
        MP2KConverter.Instance!.ASMFileOutput.Add("\t.byte\t" + format);
        _velocityChanged = true;
        _noteChanged = true;
        _keepLastOpName = true;
    }
}
