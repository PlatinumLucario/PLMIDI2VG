namespace PlatinumLucario.MIDI.GBA.MP2K;

internal partial class MP2KMIDI
{
    private static int CalculateCompressionScore(List<MP2KEvent> events, int index)
    {
        int score = 0;
        uint lastParam1 = events[index].Param1;
        uint lastVelocity = 0x80u;
        EventType lastType = events[index].Type;
        int lastDuration = -2147483648;
        uint lastNote = 0x40u;

        if (events[index].Time > 0)
        {
            score++;
        }

        for (int i = index + 1; !MP2KUtils.IsPatternBoundary(events[i].Type); i++)
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
                    if (MP2KUtils.NoteDurationLUT[duration] != lastDuration)
                    {
                        val++;
                        lastDuration = MP2KUtils.NoteDurationLUT[duration];
                    }
                }
                else
                {
                    val++;
                    lastDuration = duration;
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
                lastDuration = -2147483648;

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

    private static bool IsCompressionMatch(List<MP2KEvent> events, int index1, int index2)
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
            if (events[index1].Type != events[index2].Type ||
                events[index1].Note != events[index2].Note ||
                events[index1].Param1 != events[index2].Param1 ||
                events[index1].Param2 != events[index2].Param2 ||
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
        } while (!MP2KUtils.IsPatternBoundary(events[index1].Type));

        return MP2KUtils.IsPatternBoundary(events[index2].Type);
    }

    private static void CompressWholeNote(List<MP2KEvent> events, int index)
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
                MP2KEvent evt1 = events[j];
                MP2KEvent evt2 = events[index];
                evt1.Type = EventType.Pattern;
                evt1.Param2 = events[index].Param2 & 0x7FFFFFFF;
                evt2.Param2 = (int)(events[index].Param2 | 0x80000000);
                events[j] = evt1;
                events[index] = evt2;
            }
        }
    }

    internal static void Compress(List<MP2KEvent> events)
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

            int ccs = CalculateCompressionScore(events, i);

            if (ccs >= 6)
            {
                CompressWholeNote(events, i);
            }
        }
    }
}
