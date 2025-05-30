using System.Drawing;
using Match_3.DataObjects;
using Match_3.Service;

namespace Match_3.Workflow;

public static class QuestBuilder 
{           
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
        scoped Span<uint> maxCountPerType = stackalloc uint[tileCount];
        //maxCountPerType.Fill(maxCountPerType);
        
        foreach (var colorType in subset)
        {
            int toEven = GetRandomInterval();
            SubEventData match = new((int)(maxCountPerType[idx] / Config.MaxTilesPerMatch), toEven);
            //these subQuests below are just placeholders until this class is done then I change them to
            //smth meaningful
            SubEventData swap = new(4, -1f);
            SubEventData replacement = new(5, -1f);
            SubEventData tolerance = new(6, -1f);
            quests[idx] = new Quest(Color.FromKnownColor(colorType), GameTime.CreateTimer(toEven) ,match, swap, replacement, tolerance);
            GameState.Instance.DefineStateType(idx, colorType);
            idx++;
        }
        
        QuestHolder holder = new(quests);
        return holder;
    }
        
    public static ReadOnlySpan<char> BuildQuestMessageFrom(ref readonly Quest quest, Logger logger)
    {
        logger.UpdateQuestLog(quest);
        
        scoped var questIterator = new FormatTextEnumerator(logger.CopiedLog, 5,true);
        
        // NOTE: we need to use while loop
        //       because foreach creates a hidden copy of my iterator and
        //       hence my original iterator is empty hence I need valid data!
        while (questIterator.MoveNext()) 
        {
            var segment = questIterator.Current;
            //e.g: Swap Quest.MemberName with <corresponding value>
            var memberName = segment.MemberName2Replace.Mutable();
            var value = quest.GetValueByMemberName(memberName).ToString();
            value.CopyTo(memberName);
            memberName[value.Length..].Clear();
        }
        return logger.CopiedLog;
    }    
}