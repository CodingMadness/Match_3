using System.Runtime.InteropServices;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes;

public enum EventType : byte
{
    Click, Swap, Match, RePainted
}

public struct CurrentEventData
{
    private TimeOnly? _prev, _current;
    private int _count;
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
}


public readonly struct Current
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public CurrentEventData? Click { get; private  init; }
    public CurrentEventData? Swaps { get; private init; }
    public CurrentEventData? Match { get; private init; }
    public CurrentEventData? RePainted { get; private init; }
    public readonly Current Modify(EventType type)
    {
        switch (type)
        {
            case EventType.Click:
                if (Click.HasValue)
                {
                    var click = Click.Value;
                    click.Count++;
                    Current tmp = this with { Click = click };
                    return tmp;
                }
                break;
            case EventType.Swap:
                if (Swaps.HasValue)
                {
                    var tmp = Swaps.Value;
                    tmp.Count++;
                    Current num = this with { Swaps = tmp };
                    return num;
                }
                break;
            case EventType.Match:
                if (Match.HasValue)
                {
                    var tmp = Match.Value;
                    tmp.Count++;
                    Current num = this with { Match = tmp };
                    return num;
                }
                break;
            case EventType.RePainted:
                if (RePainted.HasValue)
                {
                    var tmp = RePainted.Value;
                    tmp.Count++;
                    Current num = this with { RePainted = tmp };
                    return num;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        throw new ArgumentOutOfRangeException(nameof(type), type, null);
    }
}

public struct SubGoal
{
    public int Count { get; init; }
    public float Seconds { get; init; }
}

public readonly struct Goal
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public SubGoal? Click { get; private  init; }
    public SubGoal? Swaps { get; private init; }
    public SubGoal? Match { get;  init; }
    public SubGoal? RePainted { get; private init; }
    
    public bool ClickCompare(in Current state) => Click?.Count < state.Click?.Count;
    public bool SwapsCompare(in Current state) => Swaps?.Count < state.Swaps?.Count;
    public bool MatchCompare(in Current state) => Match?.Count < state.Match?.Count;
}

public readonly ref struct RefTuple<T> where T : unmanaged 
{
    public readonly ref  T Item1;
    public readonly ref  T Item2;

    public RefTuple(in T item1)
    {
        Item1 = item1;
    }
 
    public RefTuple(in T item1, in T item2) : this(item1)
    {
        Item2 = item2;
    }
}

public sealed class GameState
{
    public ref readonly Current DataByTile()
    {
        ref readonly var x = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeData, Current.Body.TileType , out _);
        return ref new RefTuple<Current>(x).Item1;
    }
    public ref readonly Current DataByType()
    {
        ref readonly var x = ref CollectionsMarshal.GetValueRefOrAddDefault(_typeData, Current.Body.TileType , out _);
        return ref new RefTuple<Current>(x).Item1;
    }
    
    public bool WasSwapped;
    private Dictionary<TileType, Current> _typeData { get; }
    private Dictionary<Tile, Current> _tileData { get; }
    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Tile Current;
    public MatchX? Matches;
    public Grid Grid;

    public GameState()
    {
        _tileData = new (Game.Level.GridWidth * Game.Level.GridHeight);
        _typeData = new((int)TileType.Length);
    }
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

public abstract class QuestHandler
{
    private static readonly Dictionary<Tile, Goal> TileData 
        = new(Game.Level.GridWidth * Game.Level.GridHeight);
 
    private static readonly Dictionary<TileType, Goal> TypeData 
        = new((int)TileType.Length);
    
    protected int TypeCount => TypeData.Count;
    protected int TileCount => TileData.Count;
    private Goal x;
    protected ref readonly Goal GetGoalBy(Tile key)
    {
        /*
        ref var tmp 
            = ref CollectionsMarshal.GetValueRefOrAddDefault(TileData, key, out _);
                return ref new RefTuple<Goal>( tmp).item1;
                */
        return ref x;
    }
    protected ref readonly Goal GetGoalBy(TileType key)
    {
        /*
        ref var found
            = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeData, key, out _);
                return ref new RefTuple<Goal>( found).item1;
                */
        return ref x;
    }
    protected bool IsActive {get; private set;}
    
    protected static THandler GetInstance<THandler>() 
        where THandler : QuestHandler, new() => SingletonManager.GetOrCreateInstance<THandler>();
    
    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the state is to make per new Level the Quests harder!!

    protected bool IsSubGoalReached<T>(T key, EventType eventType) where  T: notnull
    {
        switch (key)
        {
            case Tile tile:
            {
                var state = Game.State;
                ref readonly var tileData = ref state.DataByTile();
                ref readonly var goalData = ref GetGoalBy(tile);
                
                return eventType switch
                {
                    EventType.Click => goalData.ClickCompare(tileData),
                    EventType.Swap => goalData.SwapsCompare(tileData),
                    EventType.Match => goalData.MatchCompare(tileData),
                    _ => false
                };
            }
            case TileType t:
            {
                var state = Game.State;
                ref readonly var tileData = ref state.DataByType();
                ref readonly var goalData = ref GetGoalBy(t);
                
                return eventType switch
                {
                    EventType.Click => goalData.ClickCompare(tileData),
                    EventType.Swap => goalData.SwapsCompare(tileData),
                    EventType.Match => goalData.MatchCompare(tileData),
                    _ => false
                };
            }
            default:
                return false;
        }
    }
    
    /// <summary>
    /// This will be called automatically when Grid is done with its bitmap creation!
    /// </summary>
    /// <param name="state">The current State of the game, with all needed Data</param>
    protected abstract void DefineGoals();
    protected abstract void HandleEvent();
    protected virtual void Init() => Grid.NotifyOnGridCreationDone += DefineGoals;
    public static void InitGoal()
    {
        MatchQuestHandler.Instance.Init();
        SwapQuestHandler.Instance.Init();
        TileReplacerOnClickHandler.Instance.Init();
        DestroyOnClickHandler.Instance.Init();
    }
    public void Subscribe()
    {
        bool success = SingletonManager.Storage.TryAdd(GetType(), this);
        IsActive = true;
    }
    public void UnSubscribe()
    {
        if (IsActive)
        {
            bool success = SingletonManager.Storage.Remove(GetType());
            IsActive = false;
        }
    }
}

public sealed class SwapQuestHandler : QuestHandler
{ 
    protected override void DefineGoals()
    {
        for (TileType i = 0; i < TileType.Length; i++)
        {
            ref readonly var goal = ref GetGoalBy(i);

            if (goal.Swaps is null)
                continue;
             /*
            switch (Game.Level.ID)
            {
                case 0:
                    goal.Value.Count= Utils.Randomizer.Next(6, 8);
                    goal.Seconds = (int)(Game.Level.GameBeginAt / 6f);
                    break;
                case 1:
                    goal.Swaps.Count = Utils.Randomizer.Next(4, 6);
                    goal.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 4f);
                    break;
                case 2:
                    goal.Swaps.Count = Utils.Randomizer.Next(3, 5);
                    goal.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 3f);
                    break;
                case 3:
                    goal.Swaps.Count = Utils.Randomizer.Next(1, 2);
                    goal.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 10f);
                    break;
            }
            */
        }
    }

    public static SwapQuestHandler Instance => GetInstance<SwapQuestHandler>();
    
    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    private bool IsTmpSwapGoalReached()
    {
        return IsSubGoalReached(Game.State.Current.Body.TileType, EventType.Swap);
    }

    protected override void HandleEvent()
    {
        GameState state = Game.State;
        //... needs logic...//
    }
}

public sealed class MatchQuestHandler : QuestHandler
{
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
            ref readonly var current = ref GetGoalBy(i);

            (int count, float? seconds) match = Game.Level.ID switch
            {
                //at some later point I will decide how if and how to check if a 
                //certain match was finished in an intervall! but for now we only check 
                //until gameover, if the needed matchtypes were collected
                0 => (2, null),
                1 => (6, null),
                2 => (7, null),
                3 => (9, null),
            };
            var matchValue = current.Match!.Value;

            int matchSum = matchValue.Count * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = state.TotalAmountPerType[(int)i];

            if (matchSum > maxAllowed)
            {
                //matchValue.Count = maxAllowed / Level.MAX_TILES_PER_MATCH;
                //numbers.Match = matchValue;
            }

            //Console.WriteLine(numbers.Match);
        }
    }
    
    private bool IsMatchGoalReached()
    {
        return IsSubGoalReached(Game.State.Current.Body.TileType, EventType.Match);
    }
    
    protected override void HandleEvent()
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        GameState state = Game.State;
        ref readonly var current = ref state.DataByType();
        var type = state.Current.Body.TileType;
        ref readonly var goal = ref GetGoalBy(type);
        
        if (IsMatchGoalReached())
        {
            //goal = goal with { Match = null };
            state.WasGameWonB4Timeout = TypeCount == 0;
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
        }

        //Console.WriteLine(current?.Count);
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
        GameState state = Game.State;

        ref readonly var goal = ref GetGoalBy(state.Current);

        (int x, float y) click = Game.Level.ID switch
        {
            //you have to click Count-times with only "maxTime" seconds in-between for the next click
            0 => (Utils.Randomizer.Next(1, 4), 3f),
            1 => (Utils.Randomizer.Next(5, 7), 2.5f),
            2 => (Utils.Randomizer.Next(7, 9), 3.5f),
            3 => (Utils.Randomizer.Next(9, 12), 4f),
            _ =>  default
        };
    }
    protected bool IsTmpClickGoalReached()
    {
        return IsSubGoalReached(Game.State.Current, EventType.Click);
    }
}

public sealed class DestroyOnClickHandler : ClickQuestHandler
{
    private byte _matchXCounter;

    public DestroyOnClickHandler()
    {
         Bakery.OnEnemyTileCreated += DefineGoals;
    }

    protected override void HandleEvent()
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        if (!IsActive)
            return;

        GameState state = Game.State;
        ref readonly var currClicks = ref state.DataByTile();

        if (IsTmpClickGoalReached())
        {
            state.Enemy.Disable(true);
            state.Enemy.BlockSurroundingTiles(state.Grid, false);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            //currClicks.Click = default;
        }
        else
        {
            Console.WriteLine(currClicks.Click);
            //Console.WriteLine($"SADLY YOU STILL NEED {_goal.DataByType(state.CurrentType).Click.Count}");
        }
    }

    public static DestroyOnClickHandler Instance => GetInstance<DestroyOnClickHandler>();
}

public sealed class TileReplacerOnClickHandler : ClickQuestHandler
{        
    protected override void HandleEvent()
    {
        if (!IsActive)
            return;

        GameState state = Game.State;

        //The Game notifies the QuestHandler, when smth happened to a tile on the map!
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        ref readonly var currClick = ref state.DataByType();
        ref readonly var goalClick = ref GetGoalBy(state.Current);
        
        if (IsTmpClickGoalReached() && !IsSoundPlaying(AssetManager.Splash))
        {
            //state.DefaultTile.Body.ToConstColor(rndColors[Utils.Randomizer.Next(0,rndColors.Length-1)]);
            var tile = Bakery.CreateTile(state.Current.GridCell, Utils.Randomizer.NextSingle());
            state.Grid[tile.GridCell] = tile;
            PlaySound(AssetManager.Splash);
            state.Current = tile;
            DefineGoals();
            //currClick = default;
            Console.WriteLine("Nice, you got a new tile!");
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {state.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goalClick.Click!.Value.Count - currClick.Click.Value.Count} more clicks!");
        }
    }
    
    public static TileReplacerOnClickHandler Instance => GetInstance<TileReplacerOnClickHandler>();
}