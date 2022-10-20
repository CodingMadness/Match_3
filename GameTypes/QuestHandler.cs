using System.Runtime.InteropServices;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes;

public enum EventType : byte
{
    Clicked, Swapped, Matched, RePainted, Destroyed
}

public struct EventStats
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
}

public struct Stats
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public EventStats? statsClick;
    public EventStats? Swaps;
    public EventStats? Match;
    public EventStats? RePainted;
    public EventStats? Destroyed;

    public void Inc(EventType type)
    {
        switch (type)
        {
            case EventType.Clicked:
                if (statsClick.HasValue )
                {
                    var tmp = statsClick.Value;
                    tmp.Count++;
                    statsClick = tmp;
                }
                else 
                    statsClick = null;
                break;
            case EventType.Swapped:
                if (Swaps.HasValue )
                {
                    var tmp = Swaps.Value;
                    tmp.Count++;
                    Swaps = tmp;
                }
                else
                    Swaps = null;
                break;
            case EventType.Matched:
                if (Match.HasValue )
                {
                    var tmp = Match.Value;
                    tmp.Count++;
                    Match = tmp;
                }
                else
                    Match = null;
                break;
            case EventType.RePainted:
                if (RePainted.HasValue )
                {
                    var tmp = RePainted.Value;
                    tmp.Count++;
                    RePainted = tmp;
                }
                else
                    RePainted = null;
                break;
            case EventType.Destroyed:
                if (Destroyed.HasValue )
                {
                    var tmp = Destroyed.Value;
                    tmp.Count++;
                    Destroyed = tmp;
                }
                else
                    Destroyed = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void Null(EventType type)
    {
        switch (type)
        {
            case EventType.Clicked:
                statsClick = null;
                break;
            case EventType.Swapped:
                Swaps = null;
                break;
            case EventType.Matched:
                Match = null;
                break;
            case EventType.RePainted:
                RePainted = null;
                break;
            case EventType.Destroyed:
                Destroyed = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    public override string ToString()
    {
        string output =
            $"Matches made ->(Count: {Match?.Count}  - Interval: {Match?.Interval}" +
            $"Clicks made  ->(Count: {statsClick?.Count}  - Interval: {statsClick?.Interval}"+
            $"Swaps made  ->(Count: {Swaps?.Count}  - Interval: {Swaps?.Interval}"+
            $"Repaints made ->(Count: {RePainted?.Count}  - Interval: {RePainted?.Interval}";
        return output;
    }

    public Stats()
    {
        Destroyed = new(count:0);
        statsClick = new(count: 0);
        Swaps = new(count: 0);
        Match = new(count: 0);
        RePainted = new(count: 0);
    }
}

public readonly record struct SubGoal(int Count, float Interval);

public readonly record struct Goal(SubGoal? Click, SubGoal? Swap, SubGoal? Match, SubGoal? RePaint, SubGoal? Destroyed)
{
    public int ClickCompare(in Stats? stats)
    {
        return stats switch
        {
            { statsClick: { } statsClick } when Click is { Count : var count } => count.CompareTo(
                statsClick.Count),
            null => 1,
            { statsClick: null } => 1,
            { statsClick: { } } when Click is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int SwapCompare(in Stats? stats)
    {
        return stats switch
        {
            { statsClick: { } statsClick } when Swap is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { statsClick: null } => 1,
            { statsClick: { } } when Swap is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int MatchCompare(in Stats? stats)
    {
        return stats switch
        {
            { statsClick: { } statsClick } when Match is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { statsClick: null } => 1,
            { statsClick: { } } when Match is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int DestroyCompare(in Stats? stats)
    {
        return stats switch
        {
            { statsClick: { } statsClick } when Destroyed is { Count : var count } => count.CompareTo(statsClick
                .Count),
            null => 1,
            { statsClick: null } => 1,
            { statsClick: { } } when Destroyed is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int RepaintCompare(in Stats? stats)
    {
        return stats switch
        {
            { statsClick: { } statsClick } when RePaint is { Count : var count } => count.CompareTo(statsClick
                .Count),
            null => 1,
            { statsClick: null } => 1,
            { statsClick: { } } when RePaint is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }
 
}

public unsafe ref struct RefTuple<T> where T : struct 
{
    public  ref  T Item1;
    //public ref readonly T Item2;
   //Span<T> mem;
    public RefTuple(ref T item1)
    {
        Item1 =  ref item1;
        //mem = new T[10];
        //Item2 = ref mem[0];
    }
}

public sealed class GameState
{
    public bool WasSwapped;
    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public Tile Current;
    public MatchX? Matches;
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

    protected virtual bool IsMainGoalReached { get; set; }
    
    protected static bool IsSubGoalReached(EventType eventType, in Goal goal, in Stats stats, out int direction)
    {
        direction = -int.MaxValue;
        
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
    protected abstract void DefineGoals();
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
    public static void InitGoal()
    {
        MatchQuestHandler.Instance.Init();
        SwapQuestHandler.Instance.Init();
        TileReplacerOnClickHandler.Instance.Init();
        DestroyOnClickHandler.Instance.Init();
    }
}

public sealed class SwapQuestHandler : QuestHandler
{
    protected override void DefineGoals()
    {
        var state = Game.State;
        
        for (TileType i = 0; i < TileType.Length; i++)
        {
            var goal = Game.Level.ID switch
            {
                0 => new Goal { Swap = new(Randomizer.Next(4, 7), 6f) },
                1 => new Goal { Swap = new(Randomizer.Next(3, 6), 4.5f) },
                2 => new Goal { Swap = new(Randomizer.Next(2, 4), 4.0f) },
                3 => new Goal { Swap = new(Randomizer.Next(2, 3), 3.0f) },
                _ => default
            };
            state.Current.UpdateGoal(EventType.Swapped, goal);
        }
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
    private static readonly Dictionary<TileType, Goal> TypeGoal = new((int)TileType.Length);

    private readonly int MaxGoalCount = (int)TileType.Length; 
    
    private static RefTuple<Goal> GetGoalBy(TileType t)
    {
        return new RefTuple<Goal>(ref CollectionsMarshal.GetValueRefOrAddDefault(TypeGoal, t, out _));
    }
    
    public MatchQuestHandler()
    {
        Game.OnMatchFound += HandleEvent;
    }
    
    public static MatchQuestHandler Instance => GetInstance<MatchQuestHandler>();
    
    protected override void DefineGoals()
    {
        GameState state = Game.State;

        for (TileType i = 0; i < TileType.Length; i++)
        {
            var goal = Game.Level.ID switch
            {
                0 => new Goal { Match = new(Randomizer.Next(2, 3), 8f) },
                1 => new Goal { Match = new(Randomizer.Next(3, 4), 6.5f) },
                2 => new Goal { Match = new(Randomizer.Next(5, 6), 5.0f) },
                3 => new Goal { Match = new(Randomizer.Next(8, 10), 2.5f) },
                _ => default
            };
            
            var matchValue = goal.Match!.Value;

            int matchSum = matchValue.Count * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = state.TotalAmountPerType[(int)i];

            if (matchSum > maxAllowed)
            {
                matchValue = matchValue with { Count = maxAllowed / Level.MAX_TILES_PER_MATCH };
                goal = goal with { Match = matchValue };
            }

            Console.WriteLine(goal.Match + "   for " + i + " tiles");
            TypeGoal.Add(i, goal);
        }
        int x = 10;
    }
    
    private static bool IsMatchGoalReached(out Goal? goal, in Stats stats, out int direction)
    {
        if (stats.Match is null)
        {
            goal = null;
            direction = -int.MaxValue;
            return false;
        }

        var type = Game.State.Current.Body.TileType;
        goal = GetGoalBy(type).Item1;
        return IsSubGoalReached(EventType.Matched, goal.Value, stats, out direction);
    }

    protected override bool IsMainGoalReached => SubGoalCounter == (int)TileType.Length;

    protected override void HandleEvent()
    {
        var state = Game.State;
        var type = state.Matches!.Body.TileType;
        ref var stats = ref Grid.GetStatsByType(type).Item1;
        stats.Inc(EventType.Matched);
        
        if (IsMatchGoalReached(out var goal, stats, out int direction))
        {
            stats.Null(EventType.Matched);
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
            Console.Clear();
            state.Matches.Clear();
        }
        else
        {
            if (direction == 0)
                Console.WriteLine($"Still {MaxGoalCount - (++SubGoalCounter)} matches to make, so hurry hurry");

            if (IsMainGoalReached)
                Console.WriteLine("NICE, YOU FINISHED THE ENTIRE MATCH-QUEST WELL DONE MAAAA BOOOY");
            
            if (stats.Match is null)
            {
                Console.WriteLine($"You already got ur goal finished for the {type} tiles!");
                state.Matches.Clear();
                return;
            }

            Console.WriteLine(direction);
            //Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"yea, we got {stats.Match!.Value.Count} match of type: {type} within");
            Console.WriteLine($"current match goal:  {goal?.Match}");
            Grid.Instance.Delete(state.Matches);
        }
    }
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
    protected override void DefineGoals()
    {
        var state = Game.State;

        var goal = Game.Level.ID switch
        {
           0 => new Goal { Click = new(Randomizer.Next(1, 3), 5f) },
           1 => new Goal { Click = new(Randomizer.Next(3, 5), 4.5f) },
           2 => new Goal { Click = new(Randomizer.Next(5, 6), 3.0f) },
           3 => new Goal { Click = new(Randomizer.Next(7, 10), 2.0f) },
           _ => default
        };

        state.Current.UpdateGoal(EventType.Clicked, goal);
    }
    protected static bool IsClickGoalReached(out Goal goal, in Stats stats, out int direction)
    {
        var gameState = Game.State;
        var type = gameState.Current.Body.TileType;
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
        ref   var stats = ref Grid.GetStatsByType(type).Item1;
        stats.Inc(EventType.Destroyed);

        Console.WriteLine($"Ok, 1 of {state.Current.Body.TileType} tiles was clicked!");
      
        if (IsClickGoalReached(out var goal, stats, out int direction))
        {
            var enemy = state.Current as EnemyTile;
            enemy!.Disable(true);
            enemy.BlockSurroundingTiles(Grid.Instance, false);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            stats.Null(EventType.Clicked);
        }
        else
        {
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.statsClick?.Count}");
        }
    }
}

public sealed class TileReplacerOnClickHandler : ClickQuestHandler
{        
    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        var state = Game.State;
        var type = state.Current.Body.TileType;
        ref   var stats = ref Grid.GetStatsByType(type).Item1;
        stats.Inc(EventType.Clicked);
        
        if (IsClickGoalReached(out var goal, stats, out int direction) && !IsSoundPlaying(AssetManager.Splash))
        {
            var tile = state.Current = Bakery.CreateTile(state.Current.GridCell, Randomizer.NextSingle());
            Grid.Instance[tile.GridCell] = tile;
            PlaySound(AssetManager.Splash);
            DefineGoals();
            stats.Null(EventType.Clicked);
            Console.WriteLine("Nice, you got a new tile!");
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {stats.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goal.Click?.Count - stats.statsClick?.Count} more clicks!");
        }
    }
    
    public static TileReplacerOnClickHandler Instance => GetInstance<TileReplacerOnClickHandler>();
}