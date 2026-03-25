namespace PlatinumLucario.MIDI.GBA.MP2K;

internal class MP2KEventComparer : IComparer<ValueTuple<MP2KEvent, int>>
{
    public int Compare(ValueTuple<MP2KEvent, int> lvt, ValueTuple<MP2KEvent, int> rvt)
    {
        (MP2KEvent event1, int index1) = lvt;
        (MP2KEvent event2, int index2) = rvt;
        int result = EventIntCompare(event1, event2);
        if (result == 0)
        {
            return index1 - index2;
        }
        else
        {
            return result;
        }
    }
    internal static int EventIntCompare(MP2KEvent event1, MP2KEvent event2)
    {
        if (event1.Time < event2.Time)
        {
            return event1.Time - event2.Time;
        }

        if (event1.Time > event2.Time)
        {
            return event1.Time - event2.Time;
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
            return event1Type - event2Type;
        }

        if (event1Type > event2Type)
        {
            return event1Type - event2Type;
        }

        if (event1.Type == EventType.EndOfTie)
        {
            if (event1.Note < event2.Note)
            {
                return event1.Note - event2.Note;
            }

            if (event1.Note > event2.Note)
            {
                return event1.Note - event2.Note;
            }
        }

        return 0;
    }
    internal static bool EventCompare(MP2KEvent event1, MP2KEvent event2) => EventIntCompare(event1, event2) < 0;
}
