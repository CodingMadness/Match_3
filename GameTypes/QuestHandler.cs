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
    public EventStats? Click;
    public EventStats? Swaps;
    public EventStats? Match;
    public EventStats? RePainted;
    public EventStats? Destroyed;

    public void Inc(EventType type, bool setToNull = false)
    {
        switch (type)
        {
            case EventType.Clicked:
                if (Click.HasValue && !setToNull)
                {
                    var tmp = Click.Value;
                    tmp.Count++;
                    Click = tmp;
                }
                else 
                    Click = null;
                break;
            case EventType.Swapped:
                if (Swaps.HasValue && !setToNull)
                {
                    var tmp = Swaps.Value;
                    tmp.Count++;
                    Swaps = tmp;
                }
                else
                    Swaps = null;
                break;
            case EventType.Matched:
                if (Match.HasValue && !setToNull)
                {
                    var tmp = Match.Value;
                    tmp.Count++;
                    Match = tmp;
                }
                else
                    Swaps = null;
                break;
            case EventType.RePainted:
                if (RePainted.HasValue && !setToNull)
                {
                    var tmp = RePainted.Value;
                    tmp.Count++;
                    RePainted = tmp;
                }
                else
                    Swaps = null;
                break;
            case EventType.Destroyed:
                if (Destroyed.HasValue && !setToNull)
                {
                    var tmp = Destroyed.Value;
                    tmp.Count++;
                    Destroyed = tmp;
                }
                else
                    Swaps = null;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public override string ToString()
    {
        string output =
            $"Matches made ->(Count: {Match?.Count}  - Interval: {Match?.Interval}" +
            $"Clicks made  ->(Count: {Click?.Count}  - Interval: {Click?.Interval}"+
            $"Swaps made  ->(Count: {Swaps?.Count}  - Interval: {Swaps?.Interval}"+
            $"Repaints made ->(Count: {RePainted?.Count}  - Interval: {RePainted?.Interval}";
        return output;
    }

    public Stats()
    {
        Click = new(count: 0);
        Swaps = new(count: 0);
        Match = new(count: 0);
        RePainted = new(count: 0);
    }
    
    /*
    [UnscopedRef]
    public ref readonly EventStats GetCountOf(EventType eventType)
    {
        switch (eventType)
        {
            case EventType.Clicked:
                if (Clicked.HasValue)
                {
                    return ref new RefTuple<EventStats>(Nullable.GetValueRefOrDefaultRef(Clicked)).Item1;
                }
                break;
            case EventType.Swapped:
                break;
            case EventType.Matched:
                break;
            case EventType.RePainted:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
        }
        throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null); 
    }
*/
}

public readonly record struct SubGoal(int Count, float Interval);

public  readonly record struct Goal(SubGoal? Click, SubGoal? Swap, SubGoal? Match, SubGoal? RePaint, SubGoal? Destroyed)
{
    public bool ClickCompare(in Stats? state) => state?.Click is not null && 
                                                 Click?.Count < state.Value.Click.Value.Count;
    public bool SwapsCompare(in Stats? state) => state?.Swaps is not null &&
                                                 Swap?.Count < state.Value.Swaps.Value.Count;
    public bool MatchCompare(in Stats? state) => state?.Match is not null &&
                                                 Match?.Count < state.Value.Match.Value.Count;

    public bool RePaintedCompare(in Stats? state) => state?.RePainted is not null &&
                                                     RePaint?.Count < state.Value.RePainted.Value.Count;

    public bool DestroyedCompare(in Stats? state) => state?.Destroyed is not null &&
                                                     Destroyed?.Count < state.Value.Destroyed.Value.Count;
}

public ref struct RefTuple<T> where T : unmanaged 
{
    public ref readonly T Item1;
    public RefTuple(ref T item1)
    {
        Item1 =  ref item1;
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
///the state is to make per new Level the Quests harder!!
/// </summary>
public abstract class QuestHandler
{
    protected bool IsActive { get; private set; }
    protected static THandler GetInstance<THandler>() 
        where THandler : QuestHandler, new() => SingletonManager.GetOrCreateInstance<THandler>();
    
    protected static bool IsSubGoalReached(EventType eventType, in Goal goal, ref Stats stats)
    {
        return eventType switch
        {
            EventType.Clicked => goal.ClickCompare(stats),
            EventType.Swapped => goal.SwapsCompare(stats),
            EventType.Matched => goal.MatchCompare(stats),
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
            state.Current.SetGoal(goal);
        }
    }

    public static SwapQuestHandler Instance => GetInstance<SwapQuestHandler>();
    
    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    private bool IsSwapGoalReached(out Goal goal, out Stats stats)
    {
        var type = Game.State.Current.Body.TileType;
        goal = Grid.Instance.GetTileGoalBy(Game.State.Current);
        stats = Grid.Instance.GetTileStatsBy(type);
        return IsSubGoalReached(EventType.Swapped, goal, ref stats);
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

    private RefTuple<Goal> GetGoalBy(TileType t)
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
            TypeGoal.Add(i, goal);
        }
    }
    
    private bool IsMatchGoalReached(out Goal goal, out Stats stats)
    {
        var gameState = Game.State;
        var type = gameState.Current.Body.TileType;
        goal = GetGoalBy(type).Item1;
        stats = Grid.GetStatsByType(type).Item1;
        return IsSubGoalReached(EventType.Swapped, goal, ref stats);
    }
    
    protected override void HandleEvent()
    {
        var state = Game.State;
        
        if (IsMatchGoalReached(out var goal, out var stats))
        {
            stats.Inc(EventType.Matched, true);
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
        }
        else
        {
            stats.Inc(EventType.Matched);
            Console.WriteLine($"yea, we got {stats.Match!.Value.Count} match of type: {state!.Matches!.Body.TileType} within");
            Console.WriteLine($"current match goal:  {goal.Match}");
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
           0 => new Goal { Swap = new(Randomizer.Next(1, 3), 5f) },
           1 => new Goal { Swap = new(Randomizer.Next(3, 5), 4.5f) },
           2 => new Goal { Swap = new(Randomizer.Next(5, 6), 3.0f) },
           3 => new Goal { Swap = new(Randomizer.Next(7, 10), 2.0f) },
           _ => default
        };
       
        state.Current.SetGoal(goal);
    }
    protected static bool IsClickGoalReached(out Goal goal, out Stats stats)
    {
        var gameState = Game.State;
        var type = gameState.Current.Body.TileType;
        goal = Grid.Instance.GetTileGoalBy(type);
        stats = Grid.GetStatsByType(type).Item1;
        return IsSubGoalReached(EventType.Swapped, goal, ref stats);
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
        Console.WriteLine($"Ok, 1 of {state.Current.Body.TileType} tiles was clicked!");
      
        if (IsClickGoalReached(out _, out var stats))
        {
            var enemy = state.Current as EnemyTile;
            enemy!.Disable(true);
            enemy.BlockSurroundingTiles(Grid.Instance, false);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            stats.Inc(EventType.Clicked, true);
        }
        else
        {
            stats.Inc(EventType.Destroyed);
            stats.Inc(EventType.Clicked);
            //Console.WriteLine($"SADLY YOU STILL NEED {_goal.DataByType(state.CurrentType).Clicked.Count}");
        }
    }
}

public sealed class TileReplacerOnClickHandler : ClickQuestHandler
{        
    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        GameState state = Game.State;
        
        if (IsClickGoalReached(out var goal, out var stats) && !IsSoundPlaying(AssetManager.Splash))
        {
            var tile = Bakery.CreateTile(state.Current.GridCell, Randomizer.NextSingle());
            Grid.Instance[tile.GridCell] = tile;
            PlaySound(AssetManager.Splash);
            state.Current = tile;
            DefineGoals();
            stats.Inc(EventType.Clicked, true);
            Console.WriteLine("Nice, you got a new tile!");
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {state.DefaultTile.GridCell}" );
            //Console.WriteLine($"SADLY YOU STILL NEED {goal.Clicked!.Value.Count - stats.GetCountOf(EventType.Clicked).Count} more clicks!");
        }
    }
    
    public static TileReplacerOnClickHandler Instance => GetInstance<TileReplacerOnClickHandler>();
}