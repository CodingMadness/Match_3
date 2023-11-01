using System.Diagnostics;
using DotNext;
using Match_3.Service;
using Match_3.StateHolder;
using Match_3.Variables.Extensions;

namespace Match_3.Workflow;

public static class QuestBuilder 
{
    /*
     * The Core algorithm is:
     *   - We (efficiently) replace the "TemplateQuestLog" with the quest-data
     *   - Then we store that replaced-version of the log into the "StringPool"
     *   - We return the newly logString
     *   - When we have successfully created "QuestCount" Questlogs we will
     *   - Just return from now on the respective string from the pool
     */
    private static readonly string QuestLog = $"(Black) You have to collect an amount of" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MatchCountName} {TileColor.Transparent.ToStringFast()} Matches" +
                                              $" (Black) and u have in between those only" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MatchIntervalName} seconds left" +
                                              $" (Black) and also just" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.SwapCountName} swaps available" +
                                              $" (Black) for each new match" +
                                              $" (Black) and furthermore, you only are allowed to replace any given tile" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.ReplacementCountName} times at max" +
                                              $" (Black) for your own help as well as there is the tolerance for" +
                                              $" ({TileColor.Transparent.ToStringFast()}) {Quest.MissMatchName} miss matches";
        
    private static int _questRunner;
   
    public static int QuestCount { get; private set; }
    
    public static bool ShallRecycle => _questRunner == QuestCount;
    
    public static void Init() =>  Grid.NotifyOnGridCreationDone += DefineQuest;
    
    private static void DefineQuest(Span<byte> maxCountPerType)
    {
        void Fill(Span<TileColor> toFill)
        {
            for (int i = 0; i < Utils.TileColorLen; i++)
                toFill[i] = i.ToColor();
        }

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

        const int tileCount = Utils.TileColorLen;
        // const int questLogParts = 4;
        scoped Span<TileColor> subset = stackalloc TileColor[tileCount];
        Fill(subset);
        subset.Shuffle(Utils.Randomizer);
        subset = subset.TakeRndItemsAtRndPos();
        scoped FastSpanEnumerator<TileColor> subsetEnumerator = new(subset);
        QuestCount = subset.Length;
        int trueIdx = 0;
        GameState.Quests = new Quest[QuestCount];
        GameState.StatePerQuest = new State[QuestCount];
        
        foreach (var color in subsetEnumerator)
        {
            int toEven = GetRandomInterval();
            SubEventData match = new(maxCountPerType[trueIdx] / Level.MaxTilesPerMatch, toEven);
            //these subQuests below are just placeholders until this class is done then I change them to
            //smth meaningful
            SubEventData swap = new(4, -1f);
            SubEventData replacement = new(5, -1f);
            SubEventData tolerance = new(6, -1f);
            GameState.Quests[trueIdx] = new Quest(color, GameTime.GetTimer(toEven) ,match, swap, replacement, tolerance);
            GameState.StatePerQuest[trueIdx] = new(color,false, default, new(0, 0f), new(0, 0f), new(0, 0f), new(0, 0f));
            trueIdx++;
        }
        GameState.QuestCount = QuestCount;
        
        GameState.Logger = new(QuestCount * (QuestLog.Length + 1)); 
    }
    
    public static ReadOnlySpan<char> BuildQuestMessageFrom(Quest Quest)
    {
        Debug.WriteLine($"There are:  {QuestCount} Quests to solve");
        
        //there is a defect in here....!
        var currLog = GameState.Logger!.Enqueue(QuestLog);  
        using scoped var questIterator = new QuestLineEnumerator(currLog, true);
        
        foreach (TextInfo questPiece in questIterator)
        {
            //swap (Transparent) with (<WhatEverColoIUse>)
            var placeHolderColor = questPiece.ColorAsText.AsWriteable();
            var newColor = Quest.TileColor.ToStringFast().AsSpan().AsWriteable();
            newColor.CopyTo(placeHolderColor);
            placeHolderColor[newColor.Length..].Clear();
            
            //e.g: Swap Quest.Member=Match.Count with <anyNumber>
            var memberName = questPiece.Variable2Replace.AsWriteable();
            var value = Quest.GetValueByMemberName(memberName).ToString();
            value.CopyTo(memberName);
            memberName[value.Length..].Clear();
        }
 
        _questRunner++;
        return currLog;
    }
    
    public static ReadOnlySpan<char> GetPooledQuestLog() => GameState.Logger!.Dequeue(true);
}