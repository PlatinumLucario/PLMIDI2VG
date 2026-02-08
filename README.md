# 📖 Platinum Lucario's MIDI to Video Game Converter Library (PLMIDI2VG)

This handy library can be used to convert MIDI to other video game sequence source code formats.

It utilises Kermalis's [KMIDI](https://github.com/Kermalis/KMIDI/) to handle MIDI files.

This library can handle the following sequence formats:

## Game Boy Advance formats

### Music Player 2000 (MP2K)

A common sequencing format used in many Game Boy Advance games. PLMIDI2VG is equiped with a MIDI to MP2K converter, a rewrite into C# based on the [midi2agb](https://github.com/pret/pokeemerald/tree/master/tools/mid2agb) decomp code.

All that's needed is to specify the following in your project code:
```cs
MIDIConverter converter = new MIDIConverter(inputName);
converter.SaveAsASM(outputPath)
```
That's it! That's all that it needs. However, if there's some specific arguments that need to be specified (eg. compression), then it'll need to be specified in there too, for example:
```cs
internal class SaveASM
{
    internal SaveASM(MIDIFile inputName, string outputPath)
    {
        MIDIConverter converter = new MIDIConverter(
            inputName, "output_file", 127, "_dummy",
            0, -1, 1, false, true);
        
        converter.SaveAsASM(outputPath)
    }
}
```

### Special Thanks:
* [Kermalis](https://github.com/Kermalis/) - For KMIDI and helping me understand the inner workings of the engines
* YamaArashi - For writing the decompiled code of mid2agb
* [ipatix](https://github.com/ipatix/) - For [midi2agb](https://github.com/ipatix/midi2agb/) and its improvements over the classic mid2agb
