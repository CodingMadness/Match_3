using System.Runtime.CompilerServices;
using Match_3.DataObjects;
using Match_3.Service;

namespace Match_3.Workflow;

public static class QuestBuilder 
{           
    public static void DefineQuests()
    {
        static int GetRandomInterval()
        {
            //we do netSingle() * 10f to have a real representative value for interval, like:
            // 0.4f * 10f => 2f will be the time we have left to make a match! and so on....
            float rndValue = Utility.Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);
            return toEven;
        }

        var currLvl = GameState.Instance.Lvl;
        const int tileCount = Config.TileColorCount;
        // const int questLogParts = 4;
        scoped Span<TileColor> subset = stackalloc TileColor[tileCount];
        FadeableColor.Fill(subset);
        subset.Shuffle();
        subset = subset.TakeRndItemsAtRndPos(currLvl.Id);
        int questCount = subset.Length;
        int trueIdx = 0;
        currLvl.Quests = new Quest[questCount];
        GameState.Instance.CurrData.StatePerQuest = new State[questCount];
        scoped Span<uint> maxCountPerType = stackalloc uint[tileCount];
        maxCountPerType.Fill(currLvl.CountForAllColors);
        
        foreach (var color in subset)
        {
            int toEven = GetRandomInterval();
            SubEventData match = new((int)(maxCountPerType[trueIdx] / Config.MaxTilesPerMatch), toEven);
            //these subQuests below are just placeholders until this class is done then I change them to
            //smth meaningful
            SubEventData swap = new(4, -1f);
            SubEventData replacement = new(5, -1f);
            SubEventData tolerance = new(6, -1f);
            currLvl.Quests[trueIdx] = new Quest(color, GameTime.CreateTimer(toEven) ,match, swap, replacement, tolerance);
            GameState.Instance.CurrData.StatePerQuest[trueIdx] = new(color,false, default, new(0, 0f), new(0, 0f), new(0, 0f), new(0, 0f));
            trueIdx++;
        }

        //just for testing purposes we use 1 for now...
        currLvl.QuestCount = 1;/*questCount*/; 
    }
        
    public static unsafe ReadOnlySpan<char> BuildQuestMessageFrom(ref readonly Quest quest)
    {
        //the issue is, that there are Colors inside the internal TileColor-span
        //which exceed the length of the (TileColor) inside the QuestLog and hence he cannot replace those
        //placeholder names and then tries to create a color out of the litteral "TileColor" string
        //which is nonsense of course and throws an exception 
        var copiedLog = GameState.Instance.Logger.Enqueue(GameState.QuestLog);
 
        copiedLog.Mutable().Replace(Quest.TileColorName, quest.TileColor.ToString());
        scoped var questIterator = new FormatTextEnumerator(copiedLog, 5,true);
        
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
        
        return copiedLog;
    }    
}