using Kermalis.MIDI;
using System.Diagnostics;

namespace PlatinumLucario.MIDI.GBA.MP2K;

internal enum MIDIEventCategory
{
    Control,
    SysEx,
    Meta,
    Invalid,
}

/// <summary>
/// MIDI Control Command Exclusive Types
/// </summary>
internal enum CCExType
{
    /// <summary>
    /// Pitch Bend Range
    /// </summary>
    BendR = 20,
    /// <summary>
    /// LFO Speed
    /// </summary>
    LFOS = 21,
    /// <summary>
    /// Modulation Type
    /// </summary>
    ModT = 22,
    /// <summary>
    /// Tune
    /// </summary>
    Tune = 24,
    /// <summary>
    /// LFO Delay
    /// </summary>
    LFODL = 26,
    /// <summary>
    /// Loop Enabled
    /// </summary>
    Loop = 30,
    /// <summary>
    /// Priority
    /// </summary>
    Prio = 33,
    /// <summary>
    /// Loop Start
    /// </summary>
    LoopStart = 100,
    /// <summary>
    /// Loop End
    /// </summary>
    LoopEnd = 101,
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