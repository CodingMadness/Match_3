using System.Diagnostics;
using DotNext;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Variables.Extensions;

namespace Match_3.Workflow;

public static class QuestBuilder 
{
    /*
     * The Core algorithm is:
     *   - We (efficiently) replace the "TemplateQuestLog" with the quest-data
     *   - Then we store that replaced-version of the log into the "StringPool"
     *   - We return the newly logString
     *   - When we have successfully created "QuestCount" Quest Log'S we will
     *   - Just return from now on the respective string from the pool
     */
    private static readonly string QuestLog = $"(Black) You have to collect an amount of" +
                                              $" (                     ) {Quest.MatchCountName}                     Matches" +
                                              $" (Black) and u have in between those only" +
                                              $" (                     ) {Quest.MatchIntervalName} seconds left" +
                                              $" (Black) and also just" +
                                              $" (                     ) {Quest.SwapCountName} swaps available" +
                                              $" (Black) for each new match" +
                                              $" (Black) and furthermore, you only are allowed to replace any given tile" +
                                              $" (                     ) {Quest.ReplacementCountName} times at max" +
                                              $" (Black) for your own help as well as there is the tolerance for" +
                                              $" (                     ) {Quest.MissMatchName} miss matches";
        
    private static int _questRunner;

    private static void DebugQuestLog()
    {
        Debug.WriteLine("");
        Debug.WriteLine($"There are: {GameState.Lvl.QuestCount} Quests to solve, namely: \n");

        scoped var quests = GameState.GetQuests();
        
        foreach (var quest in quests)
        {
            Debug.Write(quest + "\t");
            Debug.WriteLine(new string('-', 30));
        }
        Debug.WriteLine("");
    }
    
    public static bool ShallRecycle => _questRunner == GameState.Lvl.QuestCount;
    
    public static void DefineQuests()
    {
        int GetRandomInterval()
        {
            //we do netSingle() * 10f to have a real representative value for interval, like:
            // 0.4f * 10f => 2f will be the time we have left to make a match! and so on....
            float rndValue = Utils.Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);
            return toEven;
        }

        const int tileCount = Utils.TileColorCount;
        // const int questLogParts = 4;
        scoped Span<TileColor> subset = stackalloc TileColor[tileCount];
        Utils.Fill(subset);
        subset.Shuffle(Utils.Randomizer);
        subset = subset.TakeRndItemsAtRndPos();
        scoped FastSpanEnumerator<TileColor> subsetEnumerator = new(subset);
        int questCount = subset.Length;
        int trueIdx = 0;
        GameState.Lvl.Quests = new Quest[questCount];
        GameState.CurrData!.StatePerQuest = new State[questCount];
        scoped Span<uint> maxCountPerType = stackalloc uint[tileCount];
        maxCountPerType.Fill(GameState.Lvl.CountForAllColors);
        
        foreach (var color in subsetEnumerator)
        {
            int toEven = GetRandomInterval();
            SubEventData match = new((int)(maxCountPerType[trueIdx] / DataOnLoad.MaxTilesPerMatch), toEven);
            //these subQuests below are just placeholders until this class is done then I change them to
            //smth meaningful
            SubEventData swap = new(4, -1f);
            SubEventData replacement = new(5, -1f);
            SubEventData tolerance = new(6, -1f);
            GameState.Lvl.Quests[trueIdx] = new Quest(color, GameTime.GetTimer(toEven) ,match, swap, replacement, tolerance);
            GameState.CurrData.StatePerQuest[trueIdx] = new(color,false, default, new(0, 0f), new(0, 0f), new(0, 0f), new(0, 0f));
            trueIdx++;
        }
        
        GameState.Lvl.QuestCount = questCount;
        GameState.Logger = new(questCount * (QuestLog.Length + 1));

        DebugQuestLog();
    }
    
    public static ReadOnlySpan<char> BuildQuestMessageFrom(in Quest quest)
    {
        //there is a defect in here....!
        var currLog = GameState.Logger!.Enqueue(QuestLog);  
        using scoped var questIterator = new QuestLineEnumerator(currLog, true);
        
        foreach (TextInfo questPiece in questIterator)
        {
            //swap (Transparent) with (<WhatEverColoIUse>)
            var placeHolderColor = questPiece.ColorAsText.AsWriteable();
            var newColor = quest.TileKind.ToStringFast().AsSpan().AsWriteable();
            newColor.CopyTo(placeHolderColor);
            placeHolderColor[newColor.Length..].Clear();
            
            //e.g: Swap Quest.Member=Match.Count with <anyNumber>
            var memberName = questPiece.Variable2Replace.AsWriteable();
            var value = quest.GetValueByMemberName(memberName).ToString();
            value.CopyTo(memberName);
            memberName[value.Length..].Clear();
        }
        currLog.Replace(TileColor.Transparent.ToStringFast(), quest.TileKind.ToStringFast());
        
        _questRunner++;
        return currLog;
    }
    
    public static ReadOnlySpan<char> GetPooledQuestLog() => GameState.Logger!.Dequeue(true);
}