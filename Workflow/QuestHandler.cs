using DotNext;
using Match_3.Service;
using Match_3.Variables;
using NoAlloq;
using static Match_3.Service.Utils;

namespace Match_3.Workflow;

public static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;
    private const byte MaxRuleHandlerInstances = 1;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);
    public static readonly Dictionary<Type, RuleHandler> RuleHandlerStorage = new(MaxRuleHandlerInstances);

    public static T GetOrCreateQuestHandler<T>() where T : QuestHandler
    {
        lock (QuestHandlerStorage)
        {
            if (QuestHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = Activator.CreateInstance(typeof(T), true) as QuestHandler;

            QuestHandlerStorage.Add(typeof(T), toReturn!);

            return (T)toReturn!;
        }
    }

    public static T GetOrCreateRuleHandler<T>() where T : RuleHandler
    {
        lock (QuestHandlerStorage)
        {
            if (RuleHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = (RuleHandler?)Activator.CreateInstance(typeof(T), true);

            RuleHandlerStorage.Add(typeof(T), toReturn!);

            return (T)toReturn;
        }
    }
}


/// <summary>
///The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
///or about other events
///Game -------->(notifies) QuestHandler-------->(compares) "GameState" with "Goal" and based on the comparison, it decides what to do!
/// For instance
/// Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
///----->within a TimeSpan of X-sec
///----->without any miss-swap!
///the stats is to make per new Level the Quests harder!!
/// </summary>
public abstract class QuestHandler
{
    private readonly Type _self;
   
    public static readonly GameTime[]? QuestTimers = new GameTime[(int)TileType.Length - 1];
    
    protected static readonly Quest[] Quests = new Quest[(int)TileType.Length - 1];

    private bool IsActive { get; set; }

    protected static THandler GetInstance<THandler>() where THandler : QuestHandler
        => SingletonManager.GetOrCreateQuestHandler<THandler>();

    protected int SubGoalCounter { get; set; }
    public int QuestCount { get; protected set; }
    protected int GoalsLeft => QuestCount - SubGoalCounter;
    protected bool IsMainGoalReached => GoalsLeft == 0;

    /// <summary>
    ///The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
    ///or about other events
    ///Game -------->(notifies) QuestHandler-------->(compares) "GameState" with "Goal" and based on the comparison, it decides what to do!
    /// For instance
    /// Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    ///----->within a TimeSpan of X-sec
    ///----->without any miss-swap!
    ///the stats is to make per new Level the Quests harder!!
    /// </summary>
    protected QuestHandler(Type self)
    {
        _self = self;
        Grid.NotifyOnGridCreationDone += DefineQuest;
    }

    protected static bool IsSubQuestReached(EventType eventType, in Quest quest, in AllStats allStats,
        out int direction)
    {
        direction = default;

        return eventType switch
        {
            EventType.Swapped => (direction = quest.CompareSwaps(allStats)) == 0,
            EventType.Matched => (direction = quest.CompareMatches(allStats)) == 0,
            _ => false
        };
    }

    /// <summary>
    /// This will be called automatically when Grid is done with its bitmap creation!
    /// </summary>
    protected abstract void DefineQuest(Span<byte> maxCountPerType);

    protected abstract void HandleEvent();


    public FastSpanEnumerator<Quest> GetQuests() => new(Quests.AsSpan(0, QuestCount));

    public static void ActivateHandlers()
    {
        MatchQuestHandler.Instance.Subscribe();
        SwapQuestHandler.Instance.Subscribe();
    }

    //its private for now until I will need my other Handlers for Clicks and Destruction events, which has to be 
    //subbed and un-subbed at certain times..
    private void Subscribe()
    {
        if (IsActive) return;

        SingletonManager.QuestHandlerStorage.TryAdd(_self, this);
        IsActive = true;
    }

    public void UnSubscribe()
    {
        if (!IsActive) return;

        SingletonManager.QuestHandlerStorage.Remove(_self);
        IsActive = false;
    }
}

public sealed class SwapQuestHandler : QuestHandler
{
    protected override void DefineQuest(Span<byte> maxCountPerType)
    {
        //Define Ruleset for SwapQuestHandler:
        /*
         * Swap 3 types only
         * and if other types are swapped as well, you get punished
         */

        var goal = Game.Level.Id switch
        {
            0 => new Quest
            {
                Swap = new(Randomizer.Next(4, 6), 5f),
                TileColor = TileType.Red
            },
            1 => new Quest { Swap = new(Randomizer.Next(3, 6), 4.5f) },
            2 => new Quest { Swap = new(Randomizer.Next(2, 4), 4.0f) },
            3 => new Quest { Swap = new(Randomizer.Next(2, 3), 3.0f) },
            _ => default
        };
        QuestCount++;
        GameState.Tile.UpdateGoal(EventType.Swapped, goal);
    }

    public static SwapQuestHandler Instance { get; } = GetInstance<SwapQuestHandler>();

    private SwapQuestHandler() : base(typeof(SwapQuestHandler)) => Game.OnTileSwapped += HandleEvent;

    private static bool IsSwapGoalReached(out Quest quest, out AllStats allStats, out int direction)
    {
        var type = GameState.Tile.Body.TileType;
        quest = Grid.Instance.GetTileQuestBy(GameState.Tile);
        allStats = Grid.Instance.GetTileStatsBy(type);
        return IsSubQuestReached(EventType.Swapped, quest, allStats, out direction);
    }

    protected override void HandleEvent()
    {
        //... needs logic!...//
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
    private static readonly Quest Empty = default;

    private void CompareFinalResults()
    {
        //clear whatever was written inside the logger, because we are done now anyways and we wanna store inside 
        //to see what is the final GameState
        
        GameState.Logger.Clear();

        switch (IsMainGoalReached)
        {
            case true when !GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = true;
                GameState.Logger.Append("NICE, YOU FINISHED THE ENTIRE MATCH-QUEST BEFORE THE GAME ENDED! WELL DONE Man!");
                break;

            case false when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.Logger.Append(
                    "Unlucky, very close, you will do it next time, i am sure :-), but here your results" +
                    $"So far you have gotten at least {SubGoalCounter} done and you needed actually still {GoalsLeft} more");
                break;

            case true when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.Logger.Append("Good job, you did in time at least, you piece of shit!!");
                break;
        }
    }

    private MatchQuestHandler() : base(typeof(MatchQuestHandler))
    {
        Game.OnMatchFound += HandleEvent;
        Game.OnGameOver += CompareFinalResults;
    }

    public static MatchQuestHandler Instance { get; } = GetInstance<MatchQuestHandler>();

    private ref readonly Quest GetQuestFrom(TileType key)
    {
        var enumerator = GetQuests();

        foreach (ref readonly var pair in enumerator)
        {
            if (pair.TileColor == key)
                return ref pair;
        }

        return ref Empty;
    }

    protected override void DefineQuest(Span<byte> maxCountPerType)
    {
        int tileCount = (int)TileType.Length - 1;

        void Fill(Span<TileType> toFill)
        {
            Span<TileType> allTypes = stackalloc TileType[(int)TileType.Length - 1];

            for (int i = 1; i < allTypes.Length + 1; i++)
                allTypes[i - 1] = (TileType)i;

            allTypes.CopyTo(toFill);
        }

        scoped Span<TileType> subset = stackalloc TileType[tileCount];
        Fill(subset);
        subset.Shuffle(Randomizer);
        subset = subset.TakeRndItemsAtRndPos();
        scoped FastSpanEnumerator<TileType> subsetEnumerator = new(subset);
        QuestCount = subset.Length;

        foreach (var type in subsetEnumerator)
        {
            int trueIdx = (int)type - 1;

            //we do netSingle() * 10f to have a real representative value for interval, like:
            // 0.4f * 10f => 2f will be the time we have left to make a match! and so on....
            float rndValue = Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);

            SubQuest match = new(maxCountPerType[trueIdx] / Level.MaxTilesPerMatch, finalInterval);
            //-1f is same as to say null, but for comfort i skip float? checks..
            //3 is just placeholder and is subject to change for "swap" and "replacement"
            SubQuest swap =  new(3, -1f);
            SubQuest replacement =  new(4, -1f); 
            Quests[trueIdx] = new Quest(type, match, swap, replacement);
            QuestTimers![trueIdx] = GameTime.GetTimer(toEven);
        }

        //sort and filter the null's out
        Quests.AsSpan().Where(x => x.Match.HasValue).Select(x => x).TakeInto(Quests);
        QuestTimers.AsSpan().Where(x => x.IsInitialized).Select(x => x).TakeInto(QuestTimers);
    }

    private bool IsMatchQuestReached(out Quest? quest, in AllStats allStats, out int direction)
    {
        if (allStats.Matched is null)
        {
            quest = null;
            direction = int.MaxValue;
            return false;
        }

        var type = GameState.Tile.Body.TileType;
        quest = GetQuestFrom(type);
        return IsSubQuestReached(EventType.Matched, quest.Value, allStats, out direction);
    }

    protected override void HandleEvent()
    {
        var type = GameState.Matches!.Body!.TileType;
        ref var stats = ref Grid.GetStatsByType(type);

        if (stats.Matched is null)
        {
            GameState.Matches.Clear();
            return;
        }

        stats[EventType.Matched].Count++;

        if (IsMatchQuestReached(out var goal, stats, out int compareResult))
        {
            SubGoalCounter++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Sub goals done:  {SubGoalCounter}");
            Console.ResetColor();
            //Console.WriteLine($"yea, we got {stats.Matched!.Value.Count} match of type: {type} within");
            //Console.WriteLine($"current match goal:  {goal?.Match}");
            stats.Matched = null;
            //Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
            Console.Clear();
            // Grid.Instance.Delete(GameState.Matches);
        }
        else
        {
            CompareFinalResults();

            //error-code
            if (compareResult > 1)
            {
                GameState.Matches.Clear();
            }

            //Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"yea, we got {stats.Matched!.Value.Count} match of type: {type} within");
            Console.WriteLine($"current match goal:  {goal?.Match}");
            // Grid.Instance.Delete(GameState.Matches);
        }
    }
}
 
