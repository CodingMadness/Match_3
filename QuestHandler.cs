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

public struct Stats : IComparable<Stats>
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public EventStats? Clicked;

    public EventStats? Swapped;
    public EventStats? Matched;
    public EventStats? RePainted;
    public EventStats? Destroyed;

    public int CompareTo(Stats other)
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

    public Stats()
    {
        Destroyed = new(count: 0);
        Clicked = new(count: 0);
        Swapped = new(count: 0);
        Matched = new(count: 0);
        RePainted = new(count: 0);
    }
}

public readonly record struct SubQuest(int Count, float Interval) : IComparable<SubQuest>
{
    public int CompareTo(SubQuest other)
    {
        return Count.CompareTo(other.Count);
    }
}

public readonly record struct Quest(TileType ItemType, SubQuest? Click, SubQuest? Swap, SubQuest? Match)
{
    public int CompareClicks(in Stats? stats)
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

    public int CompareSwaps(in Stats? stats)
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

    public int CompareMatches(in Stats? stats)
    {
        return stats switch
        {
            /*stats.Matched*/
            { Matched: { } statsClick } when /*goal.Matched*/
                Match is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { Matched: null } => 1,
            { Matched: { } } when Match is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }
}

public static class GameState
{
    public static bool WasSwapped;
    public static bool EnemiesStillPresent;
    public static bool WasGameWonB4Timeout;
    public static Tile Current;
    public static MatchX? Matches;
    public static bool? WasFeatureBtnPressed;
    public static float CurrentTime;
    public static bool IsGameOver;
    public static string GameOverMessage;
}

public static class SingletonManager
{
    private const byte MaxQuestHandlerInstances = 5;
    private const byte MaxRuleHandlerInstances = 1;

    public static readonly Dictionary<Type, QuestHandler> QuestHandlerStorage = new(MaxQuestHandlerInstances);
    public static readonly Dictionary<Type, RuleHandler> RuleHandlerStorage = new(MaxRuleHandlerInstances);

    public static T GetOrCreateQuestHandler<T>() where T : QuestHandler, new()
    {
        lock (QuestHandlerStorage)
        {
            if (QuestHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = new T();
            QuestHandlerStorage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }
    
    public static T GetOrCreateRuleHandler<T>() where T : RuleHandler, new()
    {
        lock (QuestHandlerStorage)
        {
            if (RuleHandlerStorage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = new T();
            RuleHandlerStorage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }
}

public readonly struct LogData
{
    internal readonly TileType Type;
    internal readonly int Count;
    internal readonly int Interval;
    internal readonly int MaxSwapsAllowed;

    private LogData(in Quest quest)
    {
        Type = quest.ItemType;
        Count = quest.Match!.Value.Count;
        Interval = (int)quest.Match.Value.Interval;
        MaxSwapsAllowed = quest.Swap!.Value.Count;
    }

    public static implicit operator LogData(in Quest quest) => new(quest);
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
    protected virtual Type Cached_Type { get; }
    
    protected bool IsActive { get; private set; }

    protected static THandler GetInstance<THandler>() where THandler : QuestHandler, new()
        => SingletonManager.GetOrCreateQuestHandler<THandler>();

    protected int SubGoalCounter { get; set; }
    public int GoalCountToReach { get; protected set; }
    protected int GoalsLeft => GoalCountToReach - SubGoalCounter;
    protected bool IsMainGoalReached => GoalsLeft == 0;

    protected static bool IsSubGoalReached(EventType eventType, in Quest quest, in Stats stats, out int direction)
    {
        direction = int.MaxValue;

        return eventType switch
        {
            EventType.Clicked => (direction = quest.CompareClicks(stats)) == 0,
            EventType.Swapped => (direction = quest.CompareSwaps(stats)) == 0,
            EventType.Matched => (direction = quest.CompareMatches(stats)) == 0,
            _ => false
        };
    }

    /// <summary>
    /// This will be called automatically when Grid is done with its bitmap creation!
    /// </summary>
    protected abstract void DefineQuest(Span<byte> countPerType);

    protected abstract void HandleEvent();

    public void Subscribe()
    {
        if (IsActive) return;
        
        SingletonManager.QuestHandlerStorage.TryAdd(Cached_Type, this);
        IsActive = true;
    }

    public void UnSubscribe()
    {
        if (!IsActive) return;
        
        SingletonManager.QuestHandlerStorage.Remove(Cached_Type);
        IsActive = false;
    }

    protected virtual void Init() => Grid.NotifyOnGridCreationDone += DefineQuest;

    public static void InitGoals()
    {
        MatchQuestHandler.Instance.Init();
        SwapQuestHandler.Instance.Init();
        //TileReplacementOnClickHandler.Instance.Init();
        DestroyOnClickHandler.Instance.Init();
    }
}

public sealed class SwapQuestHandler : QuestHandler
{
    private static readonly Type _cachedType = Type<SwapQuestHandler>.RuntimeType;

    protected override Type Cached_Type => _cachedType;

    protected override void DefineQuest(Span<byte> countPerType)
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
        GoalCountToReach++;
        GameState.Current.UpdateGoal(EventType.Swapped, goal);
    }

    public static SwapQuestHandler Instance => GetInstance<SwapQuestHandler>();

    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    private bool IsSwapGoalReached(out Quest quest, out Stats stats, out int direction)
    {
        var type = GameState.Current.Body.TileType;
        quest = Grid.Instance.GetTileQuestBy(GameState.Current);
        stats = Grid.Instance.GetTileStatsBy(type);
        return IsSubGoalReached(EventType.Swapped, quest, stats, out direction);
    }

    protected override void HandleEvent()
    {
        //... needs logic!...//
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
    private static Quest[] TypeGoal;
    private static readonly Quest Empty;
    private static readonly Type _cachedType = Type<MatchQuestHandler>.RuntimeType;

    public MatchQuestHandler()
    {
        Game.OnMatchFound += HandleEvent;
        Game.OnGameOver += CompareResults;
    }

    private void CompareResults()
    {
        switch (IsMainGoalReached)
        {
            case true when !GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = true;
                GameState.GameOverMessage = "NICE, YOU FINISHED THE ENTIRE MATCH-QUEST BEFORE THE GAME ENDED! WELL DONE Man!";
                break;
            case false when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.GameOverMessage = "Unlucky, very close, you will do it next time, i am sure :-), but here your results" + 
                                            $"So far you have gotten at least {SubGoalCounter} done and you needed actually still {GoalsLeft} more";
                break;
            case true when GameState.IsGameOver:
                GameState.WasGameWonB4Timeout = false;
                GameState.GameOverMessage = "Good job, you did in time at least, you piece of shit!!";
                break;
        }
    }

    protected override Type Cached_Type => _cachedType;

    public static MatchQuestHandler Instance => GetInstance<MatchQuestHandler>();

    private ref readonly Quest GetGoalFrom(TileType key)
    {
        var enumerator = GetSpanEnumerator();
        
        foreach (ref readonly var pair in enumerator)
        {
            if (pair.ItemType == key)
                return ref pair;
        }

        return ref Empty;
    }

    //did some changes here..!
    protected override void DefineQuest(Span<byte> countPerType)
    {
        void Fill(Span<TileType> toFill)
        {
            Span<TileType> allTypes = stackalloc TileType[(int)TileType.Length - 1];

            for (int i = 1; i < allTypes.Length; i++)
                allTypes[i] = (TileType)i;

            allTypes.CopyTo(toFill);
        }

        var countToMatch = Game.Level.ID switch
        {
            0 => Randomizer.Next(2, 4),
            1 => Randomizer.Next(3, 5),
            2 => Randomizer.Next(5, 7),
            3 => Randomizer.Next(7, 9),
            _ => default
        };
        Span<TileType> allTypes = stackalloc TileType[(int)TileType.Length - 1];
        Fill(allTypes);
        allTypes = allTypes[1..];
        allTypes[1..].Shuffle(Randomizer);
        TypeGoal = new Quest[allTypes.Length];
        FastSpanEnumerator<TileType> enumerator = new(allTypes[..(countToMatch)]);

        foreach (var value in enumerator)
        {
            SubQuest match = new(countPerType[(int)value] / Level.MAX_TILES_PER_MATCH, 4.5f);
            SubQuest test_swap = new(4, 5f);
            TypeGoal[GoalCountToReach++] = new Quest(value, null, test_swap, match);
        }
    }

    private bool IsMatchGoalReached(out Quest? goal, in Stats stats, out int direction)
    {
        if (stats.Matched is null)
        {
            goal = null;
            direction = int.MaxValue;
            return false;
        }

        var type = GameState.Current.Body.TileType;
        goal = GetGoalFrom(type);
        return IsSubGoalReached(EventType.Matched, goal.Value, stats, out direction);
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

        if (IsMatchGoalReached(out var goal, stats, out int compareResult))
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

    public FastSpanEnumerator<Quest> GetSpanEnumerator() => new(TypeGoal.AsSpan(..GoalsLeft));
}

public abstract class ClickQuestHandler : QuestHandler
{
    protected ClickQuestHandler()
    {
        Game.OnTileClicked += HandleEvent;
    }

    protected override void Init()
    {
        Grid.OnTileCreated += DefineQuest;
    }

    protected override void DefineQuest(Span<byte> countPerType)
    {
        var goal = Game.Level.ID switch
        {
            0 => new Quest { Click = new(Randomizer.Next(2, 4), 5f) },
            1 => new Quest { Click = new(Randomizer.Next(4, 5), 4.5f) },
            2 => new Quest { Click = new(Randomizer.Next(5, 6), 3.0f) },
            3 => new Quest { Click = new(Randomizer.Next(7, 10), 2.0f) },
            _ => default
        };
        GoalCountToReach++;
        GameState.Current.UpdateGoal(EventType.Clicked, goal);
    }

    protected static bool IsClickGoalReached(out Quest quest, in Stats stats, out int direction)
    {
        var type = GameState.Current.Body.TileType;
        quest = Grid.Instance.GetTileQuestBy(type);
        return IsSubGoalReached(EventType.Clicked, quest, stats, out direction);
    }
}

public sealed class DestroyOnClickHandler : ClickQuestHandler
{
    private byte _matchXCounter;
    private static readonly Type _cachedType = Type<DestroyOnClickHandler>.RuntimeType;

    public DestroyOnClickHandler()
    {
        Bakery.OnEnemyTileCreated += DefineQuest;
    }

    protected override Type Cached_Type => _cachedType;

    public static DestroyOnClickHandler Instance => GetInstance<DestroyOnClickHandler>();

    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        var type = GameState.Current.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;

        Console.WriteLine($"Ok, 1 of {GameState.Current.Body.TileType} tiles was clicked!");

        if (IsClickGoalReached(out var goal, stats, out _))
        {
            var enemy = GameState.Current as EnemyTile;
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

public sealed class TileReplacementOnClickHandler : ClickQuestHandler
{
    private static readonly Type _cachedType = Type<TileReplacementOnClickHandler>.RuntimeType;

    public static TileReplacementOnClickHandler Instance => GetInstance<TileReplacementOnClickHandler>();

    protected override Type Cached_Type => _cachedType;

    protected override void HandleEvent()
    {
        if (!IsActive && !GameState.WasFeatureBtnPressed == true)
            return;

        var type = GameState.Current.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;

        if (IsClickGoalReached(out var goal, stats, out _) && !IsSoundPlaying(AssetManager.SplashSound))
        {
            GameState.Current.TileState &= TileState.Selected;
            var tile = Bakery.CreateTile(GameState.Current.GridCell, Randomizer.NextSingle());
            Grid.Instance[tile.GridCell] = tile;
            PlaySound(AssetManager.SplashSound);
            GameState.Current = tile;
            DefineQuest(Span<byte>.Empty);
            stats[EventType.Clicked] = default;
            Console.WriteLine($"Nice, you got a new {tile.Body.TileType} tile!");
            GameState.WasFeatureBtnPressed = false;
            tile.TileState = GameState.Current.TileState;
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {stats.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.Clicked?.Count} more clicks!");
        }
    }
}