using System.Drawing;
using DotNext.Buffers;
using Match_3.DataObjects;
using Match_3.Service;

namespace Match_3.Workflow;

public static class QuestBuilder
{
    private static void UpdateQuestLogger(ref readonly Quest quest, QuestLogger questLogger)
    {
        static void SwapMemberNameWithValue(ref readonly Quest quest, ref readonly TextInfo segment)
        {
            //e.g: Swap Quest.MemberName with <corresponding value>
            var memberName = segment.MemberName2Replace.Mutable();
            var value = quest.GetValueByMemberName(memberName).ToString();
            value.CopyTo(memberName);
            memberName[value.Length..].Clear();
        }

        questLogger.UpdateNextQuestLog(quest);
        var nextLog = questLogger.CurrentLog;

        scoped var enumerator = new FormatTextEnumerator(nextLog, 5,true);

        // NOTE: we need to use while loop
        //       because foreach creates a hidden copy of my iterator and
        //       hence my original iterator is empty hence I need valid data!
        while (enumerator.MoveNext())
        {
            ref readonly var segment = ref enumerator.Current;
            SwapMemberNameWithValue(in quest, in segment);
        }
    }

    public static void DefineGameRules(out QuestState[] states, out Quest[] quests)
    {
        static int GetRandomInterval()
        {
            //we do netSingle() * 10f to have a real representative value for interval, like:
            // 0.4f * 10f => 2f will be the time we have left to make a match! and so on....
            float rndValue = SpanUtility.Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);
            return toEven;
        }

        static int ComputeQuestRelatedData(out Span<TileColorTypes> allColorTypes)
        {
            var currLvl = GameState.Instance;
            const int tileCount = Config.TileColorCount;
            var pool = new SpanOwner<TileColorTypes>(tileCount);
            FadeableColor.Fill(pool.Span);
            pool.Span.Shuffle();
            var subset = pool.Span.TakeRndItemsAtRndPos(currLvl.LevelId);
            int questCount = subset.Length;
            allColorTypes = pool.Span;
            return questCount;
        }

        var questCount = ComputeQuestRelatedData(out var allColorTypes);
        var tmpQuests = new Quest[questCount];
        var tmpStates = new QuestState[questCount];

        for (var index = 0; index < questCount; index++)
        {
            var colorType = allColorTypes[index];
            int toEven = GetRandomInterval();
            (int Count, float CountDown) match = new(BaseTypeUtility.Randomizer.Next(2, 5), toEven);
            (int Count, float CountDown) swap = new(4, -1f);
            (int Count, float CountDown) replacement = new(5, -1f);
            (int Count, float CountDown) tolerance = new(6, -1f);

            tmpQuests[index] = new Quest(Color.FromKnownColor(colorType),
                GameTime.CreateTimer(toEven),
                match,
                swap,
                replacement,
                tolerance);

            tmpStates[index] = new(colorType);
        }

        states = tmpStates;
        quests = tmpQuests;
    }

    public static void DefineQuestTextPerQuest(ReadOnlySpan<Quest> quests, QuestLogger logger)
    {
        foreach (ref readonly var quest in quests)
        {
            UpdateQuestLogger(in quest, logger);
            int x = logger._next;
        }
    }
}