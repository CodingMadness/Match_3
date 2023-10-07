using System.Diagnostics.CodeAnalysis;
using DotNext.Reflection;
using Match_3.GameTypes;

using static Match_3.Utils;

namespace Match_3;

public enum EventType : byte
{
    Clicked,
    Swapped,
    Matched,
    RePainted,
    Destroyed,
    COUNT = Destroyed + 1
}

public struct EventStats : IComparable<EventStats>
{
    private TimeOnly? _prev, _current;
    private int _count;

    public EventStats(int count) : this()
    {
        Count = count;
    }

    public float Interval { get; private set; }

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            _current = TimeOnly.FromDateTime(DateTime.Now);
            _prev = _prev is null ? _current : TimeOnly.FromTimeSpan((_current - _prev).Value);
            Interval = _prev?.Second ?? 0f;
        }
    }

    public override string ToString()
    {
        return $"event: ({Count} was done in an Interval of: {Interval} seconds) {Environment.NewLine}";
    }

    public int CompareTo(EventStats other)
    {
        var countComparison = _count.CompareTo(other._count);
        return countComparison == 0 ? Interval.CompareTo(other.Interval) : countComparison;
    }
}

public struct AllStats : IComparable<AllStats>
{
    /// <summary>
    /// Count-Clicks, with maxTime in between them
    /// </summary>
    public EventStats? Clicked = new(count: 0);

    public EventStats? Swapped = new(count: 0);
    public EventStats? Matched = new(count: 0);
    public EventStats? RePainted = new(count: 0);
    public EventStats? Destroyed = new(count: 0);

    public AllStats()
    {
    }

    public int CompareTo(AllStats other)
    {
        for (EventType i = 0; i < EventType.COUNT; i++)
        {
            var comparison = this[i].CompareTo(other[i]);

            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    public override string ToString()
    {
        string output =
            $"Matches made ->(Count: {Matched?.Count}  - Interval: {Matched?.Interval} {Environment.NewLine}" +
            $"Clicks made  ->(Count: {Clicked?.Count}  - Interval: {Clicked?.Interval}{Environment.NewLine}" +
            $"Swapped made  ->(Count: {Swapped?.Count}  - Interval: {Swapped?.Interval}{Environment.NewLine}" +
            $"Repaints made ->(Count: {RePainted?.Count}  - Interval: {RePainted?.Interval}{Environment.NewLine}";
        return output;
    }

    public ref EventStats this[EventType index]
    {
        get
        {
            switch (index)
            {
                case EventType.Clicked when Clicked.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Clicked));
                    return ref tmp;
                }
                case EventType.Swapped when Swapped.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Swapped));
                    return ref tmp;
                }
                case EventType.Matched when Matched.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Matched));
                    return ref tmp;
                }
                case EventType.Destroyed when Destroyed.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Destroyed));
                    return ref tmp;
                }
                case EventType.RePainted when RePainted.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(RePainted));
                    return ref tmp;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }
        }
    }
}

public readonly record struct SubQuest(int Count, double Interval)  
{
    public int CompareTo(EventStats other)
    {
        int countCmp = Count.CompareTo(other.Count);
        return countCmp == 0 ? Interval.CompareTo(other.Interval) : countCmp;
    }
}

public readonly record struct Quest(TileType ItemType, SubQuest? Click, SubQuest? Swap, SubQuest? Match)
{
    public int CompareClicks(in AllStats? stats)
    {
        return stats switch
        {
            { Clicked: { } statsClick } when Click is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { Clicked: null } => 1,
            { Clicked: not null } when Click is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int CompareSwaps(in AllStats? stats)
    {
        return stats switch
        {
            { Swapped: { } statsClick } when Swap is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { Swapped: null } => 1,
            { Swapped: { } } when Swap is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int CompareMatches(in AllStats? stats)
    {
        return stats switch
        {
            /*stats.Matched*/
            { Matched: var matchedStats } when Match is not null 
                => Match.Value.CompareTo(matchedStats!.Value),
            null => 1,
            { Matched: null } => 1,
            { Matched: not null } when Match is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }
}

public static class GameState
{
    public static bool WasSwapped;
    public static bool EnemiesStillPresent;
    public static bool WasGameWonB4Timeout;
    public static Tile Tile;
    public static MatchX? Matches;
    public static bool? WasFeatureBtnPressed;
    public static float CurrentTime;
    public static bool IsGameOver;
    public static string? GameOverMessage;
}

public static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;
    private const byte MaxRuleHandlerInstances = 1;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);
    public static readonly Dictionary<Type, RuleHandler> RuleHandlerStorage = new(MaxRuleHandlerInstances);

    public static T GetOrCreateQuestHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : QuestHandler 
    {
        lock (QuestHandlerStorage)
        {
            if (QuestHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = Activator.CreateInstance<T>();
           
            QuestHandlerStorage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }

    public static T GetOrCreateRuleHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : RuleHandler
    {
        lock (QuestHandlerStorage)
        {
            if (RuleHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = Activator.CreateInstance<T>();

            RuleHandlerStorage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }
}

public readonly struct QuestLog
{
    internal readonly TileType Type;
    internal readonly int TotalMatchCount;
    internal readonly double MatchInterval;
    internal readonly int MaxSwapsAllowed;

    private QuestLog(in Quest quest)
    {
        Type = quest.ItemType;
        TotalMatchCount = quest.Match!.Value.Count;
        MatchInterval = quest.Match.Value.Interval;
        //-1 for now...!
        MaxSwapsAllowed = -1; //quest.Swap!.Value.Count;
    }

    public static implicit operator QuestLog(in Quest quest) => new(quest);
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
    protected bool IsActive { get; private set; }

    protected static THandler GetInstance<[DynMembers(DynMemberTypes.PublicParameterlessConstructor)]THandler>() where THandler : QuestHandler 
        => SingletonManager.GetOrCreateQuestHandler<THandler>();
    
    protected int SubGoalCounter { get; set; }
    protected int QuestCountToReach { get; set; }
    protected int GoalsLeft => QuestCountToReach - SubGoalCounter;
    protected bool IsMainGoalReached => GoalsLeft == 0;

    public readonly GameTime[]? QuestTimers;
    
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
        QuestTimers = new GameTime[(int)TileType.Length - 1];
        Grid.NotifyOnGridCreationDone += DefineQuest;
    }
    
    protected static bool IsSubQuestReached(EventType eventType, in Quest quest, in AllStats allStats, out int direction)
    {
        direction = default;

        return eventType switch
        {
            EventType.Clicked => (direction = quest.CompareClicks(allStats)) == 0,
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

    public void Subscribe()
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
         * and if other types ar
         */

        var goal = Game.Level.ID switch
        {
            0 => new Quest { Swap = new(Randomizer.Next(4, 7), 6f) },
            1 => new Quest { Swap = new(Randomizer.Next(3, 6), 4.5f) },
            2 => new Quest { Swap = new(Randomizer.Next(2, 4), 4.0f) },
            3 => new Quest { Swap = new(Randomizer.Next(2, 3), 3.0f) },
            _ => default
        };
        QuestCountToReach++;
        GameState.Tile.UpdateGoal(EventType.Swapped, goal);
    }

    public static SwapQuestHandler Instance => GetInstance<SwapQuestHandler>();

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
    private static Quest[] Quests = null!;
    private static readonly Quest Empty = default;

    private void CompareResults()
    {
        switch (IsMainGoalReached)
        {
            case true when !GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = true;
                GameState.GameOverMessage =
                    "NICE, YOU FINISHED THE ENTIRE MATCH-QUEST BEFORE THE GAME ENDED! WELL DONE Man!";
                break;
            case false when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.GameOverMessage =
                    "Unlucky, very close, you will do it next time, i am sure :-), but here your results" +
                    $"So far you have gotten at least {SubGoalCounter} done and you needed actually still {GoalsLeft} more";
                break;
            case true when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.GameOverMessage = "Good job, you did in time at least, you piece of shit!!";
                break;
        }
    }

    private MatchQuestHandler(): base(typeof(MatchQuestHandler))
    {
        Game.OnMatchFound += HandleEvent;
        Game.OnGameOver += CompareResults;
    }

    public static MatchQuestHandler Instance => GetInstance<MatchQuestHandler>();

    private ref readonly Quest GetQuestFrom(TileType key)
    {
        var enumerator = GetSpanEnumerator();

        foreach (ref readonly var pair in enumerator)
        {
            if (pair.ItemType == key)
                return ref pair;
        }

        return ref Empty;
    }

    protected override void DefineQuest(Span<byte> maxCountPerType)
    {
        void Fill(Span<TileType> toFill)
        {
            Span<TileType> allTypes = stackalloc TileType[(int)TileType.Length - 1];

            for (int i = 1; i < allTypes.Length + 1; i++)
                allTypes[i - 1] = (TileType)i;

            allTypes.CopyTo(toFill);
        }
        
        int tileCount = (int)TileType.Length - 1;
        scoped Span<TileType> allTypes = stackalloc TileType[tileCount];
        Fill(allTypes);
        allTypes.Shuffle(Randomizer);
        Quests = new Quest[allTypes.Length];
        scoped FastSpanEnumerator<TileType> enumerator = new(allTypes);
        
        foreach (var type in enumerator)
        {
            int trueIdx = (int)type - 1;

            //we do netSingle() * 7f to have a real representative value for interval, like:
            // 0.4f * 5f => 2f will be the time we have left to make a match! and so on....
            float rndValue = Randomizer.NextSingle().Trunc(1);
            rndValue = rndValue.Equals(0f, 0.0f) ? 0.25f : rndValue;
            float finalInterval = MathF.Round(rndValue * 10f);
            finalInterval = finalInterval <= 2.5f ? 2.5f : finalInterval;
            int toEven = (int)MathF.Round(finalInterval, MidpointRounding.ToEven);
            
            SubQuest match = new(maxCountPerType[trueIdx] / Level.MAX_TILES_PER_MATCH, finalInterval);
            //the both "null" are just for now, to keep it simple, so we focus on handling only the matches for now!
            Quests[QuestCountToReach] = new Quest(type, null, null, match);
            QuestTimers![QuestCountToReach] = GameTime.GetTimer(toEven);
            QuestCountToReach++;
        }
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
            CompareResults();

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

    public FastSpanEnumerator<Quest> GetSpanEnumerator() => new(Quests.AsSpan(..GoalsLeft));
}

public abstract class ClickQuestHandler : QuestHandler
{
    protected ClickQuestHandler(Type evenMoreConcrete): base(evenMoreConcrete)
    {
        Grid.OnTileCreated += DefineQuest;
        Game.OnTileClicked += HandleEvent;
    }

    // protected override void Init()
    // {
    //     Grid.OnTileCreated += DefineQuest;
    // }

    protected override void DefineQuest(Span<byte> maxCountPerType)
    {
        var goal = Game.Level.ID switch
        {
            0 => new Quest { Click = new(Randomizer.Next(2, 5), Randomizer.NextDouble() * 10f) },
            1 => new Quest { Click = new(Randomizer.Next(5, 7), Randomizer.NextDouble() * 10f) },
            2 => new Quest { Click = new(Randomizer.Next(7, 10), Randomizer.NextDouble() * 10f) },
            3 => new Quest { Click = new(Randomizer.Next(10, 14), Randomizer.NextDouble() * 10f) },
            _ => default
        };
        QuestCountToReach++;
        GameState.Tile.UpdateGoal(EventType.Clicked, goal);
    }

    protected override void HandleEvent()
    {
        throw new NotImplementedException();
    }

    protected static bool IsClickGoalReached(out Quest quest, in AllStats allStats, out int direction)
    {
        var type = GameState.Tile.Body.TileType;
        quest = Grid.Instance.GetTileQuestBy(type);
        return IsSubQuestReached(EventType.Clicked, quest, allStats, out direction);
    }
}

public sealed class DestroyOnClickHandler : ClickQuestHandler
{
    private byte _matchXCounter;

    private DestroyOnClickHandler() : base(typeof(DestroyOnClickHandler))
    {
        Bakery.OnEnemyTileCreated += DefineQuest;
    }

    public static DestroyOnClickHandler Instance => GetInstance<DestroyOnClickHandler>();

    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        var type = GameState.Tile.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;

        Console.WriteLine($"Ok, 1 of {GameState.Tile.Body.TileType} tiles was clicked!");

        if (IsClickGoalReached(out var goal, stats, out _))
        {
            var enemy = GameState.Tile as EnemyTile;
            enemy!.Disable(true);
            enemy.BlockSurroundingTiles(Grid.Instance, false);
            GameState.EnemiesStillPresent = ++_matchXCounter < GameState.Matches!.Count;
            _matchXCounter = (byte)(GameState.EnemiesStillPresent ? _matchXCounter : 0);
            stats[EventType.Clicked] = default;
        }
        else
        {
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.Clicked?.Count}");
        }
    }
}

public sealed class TileReplacementOnClickHandler() : ClickQuestHandler(typeof(TileReplacementOnClickHandler))
{
    public static TileReplacementOnClickHandler Instance => GetInstance<TileReplacementOnClickHandler>();
    
    protected override void HandleEvent()
    {
        if (!IsActive && !GameState.WasFeatureBtnPressed == true)
            return;

        var type = GameState.Tile.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;

        if (IsClickGoalReached(out var goal, stats, out _) && !IsSoundPlaying(AssetManager.SplashSound))
        {
            GameState.Tile.TileState &= TileState.Selected;
            var tile = Bakery.CreateTile(GameState.Tile.GridCell, Randomizer.NextSingle());
            Grid.Instance[tile.GridCell] = tile;
            PlaySound(AssetManager.SplashSound);
            GameState.Tile = tile;
            DefineQuest(Span<byte>.Empty);
            stats[EventType.Clicked] = default;
            Console.WriteLine($"Nice, you got a new {tile.Body.TileType} tile!");
            GameState.WasFeatureBtnPressed = false;
            tile.TileState = GameState.Tile.TileState;
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {stats.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.Clicked?.Count} more clicks!");
        }
    }
}