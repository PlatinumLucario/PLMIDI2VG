namespace PlatinumLucario.MIDI.GBA.MP2K;

internal struct MP2KEvent
{
    internal int Time;
    internal EventType Type;
    internal CCExType CCExType;
    internal byte Channel;
    internal byte Note;
    internal byte Param1;
    internal int Param2;
};
