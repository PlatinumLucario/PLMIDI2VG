using Kermalis.MIDI;

namespace PlatinumLucario.MIDI.GBA.MP2K;

// Rewritten C# code based on ipatix's midi2agb midi_apply_filters C++ method
// which has been adapted to operate with MP2KEvent structs instead
internal partial class MP2KMIDI
{
    internal void ApplyFilters()
    {
        byte VolumeScale(byte vol, byte expr)
        {
            double x = vol * expr * MP2KConverter.Instance!.MasterVolume;
            if (MP2KConverter.Instance!.ApplyNaturalVolumeScale)
            {
                x /= 127.0 * 127.0 * 128.0;
                x = Math.Pow(x, 10.0 / 6.0);
                x *= 127.0;
                x = Math.Round(x);
            }
            else
            {
                x /= 127.0 * 128.0;
                x = Math.Round(x);
            }
            return (byte)Math.Clamp((int)x, 0, 127);
        }

        byte VelocityScale(byte vel)
        {
            double x = vel;
            if (MP2KConverter.Instance!.ApplyNaturalVolumeScale)
            {
                x /= 127.0;
                x = Math.Pow(x, 10.0 / 6.0);
                x *= 127.0;
                x = Math.Round(x);
            }
            return (byte)Math.Clamp((int)x, 1, 127);
        }

        byte volume = 100;
        byte expression = 127;

        for (int ievt = 0; ievt < _trackEvents.Count; ievt++)
        {
            MP2KEvent ev = _trackEvents[ievt];
            if (ev.Type == EventType.Controller)
            {
                if (ev.Param1 == (byte)ControllerType.ChannelVolume)
                {
                    volume = (byte)ev.Param2;
                    ev.Param2 = VolumeScale(volume, expression);
                }
                else if (ev.Param1 == (byte)ControllerType.ExpressionController)
                {
                    expression = (byte)ev.Param2;
                    ev.Param2 = VolumeScale(volume, expression);
                }
                else if (ev.Param1 == (byte)ControllerType.ModulationWheel)
                {
                    float scaledMod = (byte)ev.Param2 * MP2KConverter.Instance!.ModScale;
                    scaledMod = MathF.Round(scaledMod);
                    ev.Param2 = (byte)Math.Clamp(scaledMod, 0.0f, 127.0f);
                }
            }
            else if (ev.Type == EventType.Note)
            {
                ev.Param1 = VelocityScale(ev.Param1);
            }
        }
    }
}