namespace PlatinumLucario.MIDI.GBA.MP2K;

internal static partial class MP2KUtils
{
    internal static bool IsPatternBoundary(EventType type)
    {
        return type == EventType.EndOfTrack || (int)type <= 0x17;
    }

    internal static string ToAlphaNumerical(Span<char> str)
    {
        // replaces all characters that are not alphanumerical
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] >= 'a' && str[i] <= 'z')
                continue;
            if (str[i] >= 'A' && str[i] <= 'Z')
                continue;
            if (str[i] >= '0' && str[i] <= '9' && i > 0)
                continue;
            str[i] = '_';
        }
        return str.ToString();
    }

    internal static MP2KEvent EventSortedSelect((MP2KEvent e, int i) pair) => pair.e;
    internal static (MP2KEvent, int) EventOrderSelect((MP2KEvent e, int i) tuple) => tuple;
    internal static (MP2KEvent e, int i) EventIndexSelect(MP2KEvent evt, int index) => (evt, index);
    internal static void SortEvents(List<MP2KEvent> events)
    {
        var selection = events.Select(EventIndexSelect);
        var ordered = selection.OrderBy(EventOrderSelect, new MP2KEventComparer());
        events = [.. ordered.Select(EventSortedSelect)];
    }
}
