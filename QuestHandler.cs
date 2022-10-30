using System.Collections;
using FastEnumUtility;
using Match_3.GameTypes;
using NoAlloq;
using static Match_3.Utils;

namespace Match_3;

public enum EventType : byte
{
    Clicked, Swapped, Matched, RePainted, Destroyed, COUNT = Destroyed + 1
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
            $"Clicks made  ->(Count: {Clicked?.Count}  - Interval: {Clicked?.Interval}{Environment.NewLine}"+
            $"Swapped made  ->(Count: {Swapped?.Count}  - Interval: {Swapped?.Interval}{Environment.NewLine}"+
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
        Destroyed = new(count:0);
        Clicked = new(count: 0);
        Swapped = new(count: 0);
        Matched = new(count: 0);
        RePainted = new(count: 0);
    }
}

public readonly record struct SubGoal(int Count, float Interval) : IComparable<SubGoal>
{
    public int CompareTo(SubGoal other)
    {
        return Count.CompareTo(other.Count);
    }
}

public readonly record struct Goal(SubGoal? Click, SubGoal? Swap, SubGoal? Match)  
{
    public int ClickCompare(in Stats? stats)
    {
        return stats switch
        {
            { Clicked: { } statsClick } when Click is { Count : var count } => count.CompareTo(
                statsClick.Count),
            null => 1,
            { Clicked: null } => 1,
            { Clicked: { } } when Click is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int SwapCompare(in Stats? stats)
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

    public int MatchCompare(in Stats? stats)
    {
        return stats switch
        {
            /*stats.Matched*/
            { Matched: { } statsClick } when /*goal.Matched*/Match is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { Matched: null } => 1,
            { Matched: { } } when Match is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }
}

public sealed class GameState
{
    public bool WasSwapped;
    public bool EnemiesStillPresent;
    public bool WasGameWonB4Timeout;
    public Tile Current;
    public MatchX? Matches;
    public bool? WasFeatureBtnPressed;
    public float CurrentTime;
    public bool IsGameOver;
    public string GameOverMessage;
}

public static class SingletonManager
{
    public static readonly Dictionary<Type, QuestHandler> Storage = new();

    public static T GetOrCreateInstance<T>() where T : QuestHandler, new()
    {
        lock(Storage)
        {
            if (Storage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }

            toReturn = new T();
            Storage.Add(typeof(T), toReturn);

            return (T)toReturn;
        }
    }
}


/// <summary>
///The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
///or about other events
///Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
/// For instance
/// Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
///----->within a TimeSpan of X-sec
///----->without any miss-swap!
///the stats is to make per new Level the Quests harder!!
/// </summary>
public abstract class QuestHandler
{
    protected bool IsActive { get; private set; }
    protected static THandler GetInstance<THandler>() 
        where THandler : QuestHandler, new() => SingletonManager.GetOrCreateInstance<THandler>();
    protected int SubGoalCounter { get; set; }
    public virtual int GoalCountToReach { get; set; }
    protected int GoalsLeft => GoalCountToReach - SubGoalCounter;
    protected bool IsMainGoalReached => GoalsLeft == 0;
    protected static bool IsSubGoalReached(EventType eventType, in Goal goal, in Stats stats, out int direction)
    {
        direction = int.MaxValue;
        
        return eventType switch
        {
            EventType.Clicked => (direction = goal.ClickCompare(stats)) == 0,
            EventType.Swapped => (direction = goal.SwapCompare(stats)) == 0,
            EventType.Matched => (direction = goal.MatchCompare(stats))== 0,
            _ => false
        };
    }
    
    /// <summary>
    /// This will be called automatically when Grid is done with its bitmap creation!
    /// </summary>
    protected abstract void DefineGoals(Span<byte> countPerType);
    protected abstract void HandleEvent();
    public void Subscribe()
    {
        SingletonManager.Storage.TryAdd(GetType(), this);
        IsActive = true;
    }
    public void UnSubscribe()
    {
        if (IsActive)
        {
            SingletonManager.Storage.Remove(GetType());
            IsActive = false;
        }
    }
    protected virtual void Init() => Grid.NotifyOnGridCreationDone += DefineGoals;
    public static void InitGoals()
    {
        MatchQuestHandler.Instance.Init();
        SwapQuestHandler.Instance.Init();
        TileReplacerOnClickHandler.Instance.Init();
        DestroyOnClickHandler.Instance.Init();
    }
}

public sealed class SwapQuestHandler : QuestHandler
{
    protected override void DefineGoals(Span<byte> countPerType)
    {
        //Define Ruleset for SwapQuestHandler:
        /*
         * Swap 3 types only
         * and if other types ar
         * 
         */
        var state = Game.State;
        
        var goal = Game.Level.ID switch
        {
            0 => new Goal { Swap = new(Randomizer.Next(4, 7), 6f) },
            1 => new Goal { Swap = new(Randomizer.Next(3, 6), 4.5f) },
            2 => new Goal { Swap = new(Randomizer.Next(2, 4), 4.0f) },
            3 => new Goal { Swap = new(Randomizer.Next(2, 3), 3.0f) },
            _ => default
        };
        GoalCountToReach++;
        state.Current.UpdateGoal(EventType.Swapped, goal);
    }

    public static SwapQuestHandler Instance => GetInstance<SwapQuestHandler>();
    
    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    private bool IsSwapGoalReached(out Goal goal, out Stats stats, out int direction)
    {
        var type = Game.State.Current.Body.TileType;
        goal = Grid.Instance.GetTileGoalBy(Game.State.Current);
        stats = Grid.Instance.GetTileStatsBy(type);
        return IsSubGoalReached(EventType.Swapped, goal, stats, out direction);
    }

    protected override void HandleEvent()
    {
        GameState state = Game.State;
        //... needs logic...//
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
    private static (TileType, Goal)[] TypeGoal;
    private static readonly (TileType, Goal) Empty = default;
    
    public MatchQuestHandler()
    {
        Game.OnMatchFound += HandleEvent;
        Game.OnGameOver += CompareResults;
    }

    private void CompareResults()
    {
        var state = Game.State;
        
        switch (IsMainGoalReached)
        {
            case true when !state.IsGameOver:
                state.WasGameWonB4Timeout = true;
                state.GameOverMessage = "NICE, YOU FINISHED THE ENTIRE MATCH-QUEST BEFORE THE GAME ENDED! WELL DONE Man!";
                break;
            case false when state.IsGameOver:    
                state.WasGameWonB4Timeout = false;
                state.GameOverMessage =
                    "Unlucky, very close, you will do it next time, i am sure :-), but here your results" +
                    $"So far you have gotten at least {SubGoalCounter} done and you needed actually still {GoalsLeft} more";
                break;
            case true when state.IsGameOver:
                state.WasGameWonB4Timeout = false;
                state.GameOverMessage = "Good job, you did in time atlast, you pice of shit!!";
                break;
        }
    }

    public static MatchQuestHandler Instance => GetInstance<MatchQuestHandler>();

    private static ref readonly (TileType, Goal) GetGoalBy(TileType key)
    {
        foreach (ref readonly var pair in GetSpanEnumerator())
        {
            if (pair.Item1 != TileType.Empty)
                return ref pair;
        }

        return ref Empty;
    }

    //did some changes here..!
    protected override void DefineGoals(Span<byte> countPerType)
    {
        void LocalFunction(in Span<TileType> dest)
        {
            Span<TileType> local = stackalloc TileType[(int)(TileType.Length)];
         
            for (int i = 1; i < local.Length; i++)
            {
                local[i] = (TileType)i; //just some random value..
            }
            local.CopyTo(dest);
        }
        
        Span<TileType> Difference(Span<TileType> a, Span<TileType> b)
        {
            LinkedSpan<byte> ls =new(); 
            //get the elements from a which are not in b
            var bRunner = new SpanEnumerator<TileType>(b);
            var aRunner = new SpanEnumerator<TileType>(a);
            Span<byte> bytes = MemoryMarshal.Cast<TileType, byte>(b);
            
            foreach (var aValue in aRunner)
            {
                if (!bytes.Contains((byte)aValue))
                    ls.Append((byte)aValue);
            }

            Span<byte> tmp = ls;
            return MemoryMarshal.Cast<byte, TileType>(tmp);
        }
        
        Span<TileType> local = stackalloc TileType[(int)(TileType.Length)];
        LocalFunction(local);
        
        var countToMatch = Game.Level.ID switch
        {
            0 => Randomizer.Next(2, 4),
            1 => Randomizer.Next(3, 5),
            2 => Randomizer.Next(5, 7),
            3 => Randomizer.Next(7, 9),
            _ => default
        };
        
        TypeGoal = new (TileType, Goal)[countToMatch];
        var allTypes = local.Slice(1, (int)TileType.Length-1);
        allTypes.Shuffle(Randomizer);
        var nextPiece = allTypes[..countToMatch];
        var iterator = new SpanEnumerator<TileType>(nextPiece);
        
        LOOP_AGAIN:
        foreach (var typeNr in iterator)
        {
            var goal = Game.Level.ID switch
            {
                0 => new Goal { Match = new(Randomizer.Next(2, 2), 8f) },
                1 => new Goal { Match = new(Randomizer.Next(3, 4), 6.5f) },
                2 => new Goal { Match = new(Randomizer.Next(5, 6), 5.0f) },
                3 => new Goal { Match = new(Randomizer.Next(8, 10), 2.5f) },
                _ => default
            };
            
            int index = (int)(typeNr < (TileType)countPerType.Length ? typeNr : typeNr - 1); 
            int maxAllowed = countPerType[index];

            var matchValue = goal.Match!.Value;
            int matchSum = matchValue.Count * Level.MAX_TILES_PER_MATCH;
            
            if (matchSum > maxAllowed)
            {
                matchValue = matchValue with { Count = maxAllowed / Level.MAX_TILES_PER_MATCH };

                if (matchValue.Count == 0)
                {
                    //var diff = allTypes - nextPiece
                    var difference = Difference(allTypes, nextPiece);
                    
                    TileType newOne = difference.Where(x => x >= (TileType)2).First();
                    nextPiece[(int)iterator.Current] = newOne;
                    GoalCountToReach = 0;
                    goto LOOP_AGAIN;
                }
                goal = goal with { Match = matchValue };
            }
          
            TypeGoal[GoalCountToReach++] = (typeNr, goal);
        }
    }

    private static bool IsMatchGoalReached(out Goal? goal, in Stats stats, out int direction)
    {
        if (stats.Matched is null)
        {
            goal = null;
            direction = int.MaxValue;
            return false;
        }

        var type = Game.State.Current.Body.TileType;
        goal = GetGoalBy(type).Item2;
        return IsSubGoalReached(EventType.Matched, goal.Value, stats, out direction);
    }
    
    protected override void HandleEvent()
    {
        var state = Game.State;
        var type = state.Matches!.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);

        if (stats.Matched is null)
        {
            state.Matches.Clear();
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
            Grid.Instance.Delete(state.Matches);
        }
        else
        {
            CompareResults();
            
            //error-code
            if (compareResult > 1)
            {
                state.Matches.Clear();
            }
            //Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"yea, we got {stats.Matched!.Value.Count} match of type: {type} within");
            Console.WriteLine($"current match goal:  {goal?.Match}");
            Grid.Instance.Delete(state.Matches);
        }
    }

    public static SpanEnumerator<(TileType, Goal)> GetSpanEnumerator() => new (TypeGoal.AsSpan());
}

public abstract class ClickQuestHandler : QuestHandler
{
    protected ClickQuestHandler()
    {
        Game.OnTileClicked += HandleEvent;
    }
    protected override void Init()
    {
        Grid.OnTileCreated += DefineGoals;
    }
    protected override void DefineGoals(Span<byte> countPerType)
    {
        var state = Game.State;

        var goal = Game.Level.ID switch
        {
           0 => new Goal { Click = new(Randomizer.Next(2, 4), 5f) },
           1 => new Goal { Click = new(Randomizer.Next(4, 5), 4.5f) },
           2 => new Goal { Click = new(Randomizer.Next(5, 6), 3.0f) },
           3 => new Goal { Click = new(Randomizer.Next(7, 10), 2.0f) },
           _ => default
        };
        GoalCountToReach++;
        state.Current.UpdateGoal(EventType.Clicked, goal);
    }
    protected static bool IsClickGoalReached(out Goal goal, in Stats stats, out int direction)
    {
        var type = Game.State.Current.Body.TileType;
        goal = Grid.Instance.GetTileGoalBy(type);
        return IsSubGoalReached(EventType.Clicked, goal, stats, out direction);
    }
}

public sealed class DestroyOnClickHandler : ClickQuestHandler
{
    private byte _matchXCounter;

    public DestroyOnClickHandler()
    {
         Bakery.OnEnemyTileCreated += DefineGoals;
    }

    public static DestroyOnClickHandler Instance => GetInstance<DestroyOnClickHandler>();

    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        var state = Game.State;
        var type = state.Current.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;
        
        Console.WriteLine($"Ok, 1 of {state.Current.Body.TileType} tiles was clicked!");
      
        if (IsClickGoalReached(out var goal, stats, out _))
        {
            var enemy = state.Current as EnemyTile;
            enemy!.Disable(true);
            enemy.BlockSurroundingTiles(Grid.Instance, false);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            stats[EventType.Clicked] = default;
        }
        else
        {
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.Clicked?.Count}");
        }
    }
}

public sealed class TileReplacerOnClickHandler : ClickQuestHandler
{
    public static TileReplacerOnClickHandler Instance => GetInstance<TileReplacerOnClickHandler>();
        
    protected override void HandleEvent()
    {
        var state = Game.State;

        if (!IsActive && !state.WasFeatureBtnPressed == true)
            return;

        var type = state.Current.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type);
        stats[EventType.Clicked].Count++;
        
        if (IsClickGoalReached(out var goal, stats, out _) && !IsSoundPlaying(AssetManager.SplashSound))
        {
            state.Current.TileState &= TileState.Selected; 
            var tile  = Bakery.CreateTile(state.Current.GridCell, Randomizer.NextSingle());
            Grid.Instance[tile.GridCell] = tile;
            PlaySound(AssetManager.SplashSound);
            state.Current = tile;
            DefineGoals(Span<byte>.Empty);
            stats[EventType.Clicked] = default;
            Console.WriteLine($"Nice, you got a new {tile.Body.TileType} tile!");
            state.WasFeatureBtnPressed = false;
            tile.TileState = state.Current.TileState;
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {stats.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.Clicked?.Count} more clicks!");
        }
    }
}