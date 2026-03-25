using Kermalis.MIDI;

namespace PlatinumLucario.MIDI.GBA.MP2K;

public sealed class MP2KConverter
{
    // Main
    internal readonly MIDIFile MIDIFile;
    internal string ASMFileLabel;
    internal byte MasterVolume;
    internal string VoiceGroupLabel;
    internal int Priority;
    internal int Reverb;

    // Variables exclusive to official mid2agb
    internal readonly int ClocksPerBeat;
    internal readonly bool ExactGateTimeEnabled;
    internal readonly bool CompressionEnabled;

    // Variables exclusive to ipatix's midi2agb
    internal bool ApplyNaturalVolumeScale;
    internal byte ModType;
    internal bool ModTypeEnabled;
    internal byte ModSpeed;
    internal bool ModSpeedEnabled;
    internal byte ModDelay;
    internal bool ModDelayEnabled;
    internal float ModScale;

    internal readonly List<string> ASMFileOutput = [];
    private StreamWriter? _asmFile;

    internal readonly MP2KMIDI MIDI = new();
    internal readonly MP2KEngine Engine = new();

    internal static MP2KConverter? Instance;

    public MP2KConverter(
        MIDIFile midiFile, string asmFileLabel = "output_file",
        byte masterVolume = 127, string voiceGroupLabel = "_dummy",
        int priority = 0, int reverb = -1,
        int clocksPerBeat = 1, bool exactGateTimeEnabled = false,
        bool compressionEnabled = true
        )
    {
        MIDIFile = midiFile;
        ASMFileLabel = asmFileLabel;
        MasterVolume = masterVolume;
        VoiceGroupLabel = voiceGroupLabel;
        Priority = priority;
        Reverb = reverb;
        ClocksPerBeat = clocksPerBeat;
        ExactGateTimeEnabled = exactGateTimeEnabled;
        CompressionEnabled = compressionEnabled;

        Instance = this;

        AddMIDIInfo();
    }

    internal MP2KConverter(
        MIDIFile midiFile, string asmFileLabel = "output_file",
        byte masterVolume = 127, string voiceGroupLabel = "_dummy",
        int priority = 0, int reverb = -1,
        bool applyNaturalVolumeScale = false,
        bool modTypeEnabled = false, byte modType = 0,
        bool modSpeedEnabled = false, byte modSpeed = 0,
        bool modDelayEnabled = false, byte modDelay = 0,
        float modScale = 1.0f
    )
    {
        MIDIFile = midiFile;
        ASMFileLabel = asmFileLabel;
        MasterVolume = masterVolume;
        VoiceGroupLabel = voiceGroupLabel;
        Priority = priority;
        Reverb = reverb;
        ApplyNaturalVolumeScale = applyNaturalVolumeScale;
        ModTypeEnabled = modTypeEnabled;
        ModType = modType;
        ModSpeedEnabled = modSpeedEnabled;
        ModSpeed = modSpeed;
        ModDelayEnabled = modDelayEnabled;
        ModDelay = modDelay;
        ModScale = modScale;

        Instance = this;

        AddMIDIInfo();
    }

    private void AddMIDIInfo()
    {
        MIDI.ReadMIDIFileHeader();
        MP2KEngine.WriteAGBHeader();
        MIDI.ReadMIDITracks();
        MP2KEngine.WriteAGBFooter();
    }

    public void SaveAsASM(string filePath)
    {
        _asmFile = new StreamWriter(filePath);

        foreach (string line in ASMFileOutput)
        {
            _asmFile.WriteLine(line);
        }

        _asmFile.Flush();
        _asmFile.Close();
    }

}
