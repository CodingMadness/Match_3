using System.Drawing;
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

    public static QuestHolder BuildQuests()
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

        var currLvl = GameState.Instance;
        const int tileCount = Config.TileColorCount;
        // const int questLogParts = 4;
        scoped Span<TileColorTypes> subset = stackalloc TileColorTypes[tileCount];
        FadeableColor.Fill(subset);
        subset.Shuffle();
        subset = subset.TakeRndItemsAtRndPos(currLvl.LevelId);
        int questCount = subset.Length;
        int idx = 0;
        var quests = new Quest[questCount];
        
        GameState.Instance.InitStates(questCount);
        
        foreach (var colorType in subset)
        {
            int toEven = GetRandomInterval();
            (int Count, float CountDown) match = new(BaseTypeUtility.Randomizer.Next(2,5), toEven);
            (int Count, float CountDown) swap = new(4, -1f);
            (int Count, float CountDown) replacement = new(5, -1f);
            (int Count, float CountDown) tolerance = new(6, -1f);
            quests[idx] = new Quest(Color.FromKnownColor(colorType), GameTime.CreateTimer(toEven) ,match, swap, replacement, tolerance);
            GameState.Instance.DefineStateType(idx, colorType);
            idx++;
        }
        
        QuestHolder holder = new(quests);
        return holder;
    }

    public static void BuildQuestText(Span<Quest> quests, QuestLogger logger)
    {
        foreach (ref readonly var quest in quests)
        {
            UpdateQuestLogger(in quest, logger);
        }
        logger.Reset();
    }
}