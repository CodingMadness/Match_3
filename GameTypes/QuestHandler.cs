using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Raylib_CsLo.Raylib;

namespace Match_3.GameTypes;

public struct Numbers
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public (int Count, float? Seconds) Click;
    public (int AllowedSwaps, float? Seconds) Swaps;
    public (int Count, float? Seconds) Match;

    public bool ClickCompare(ref Numbers goal)
    {
        return Click.Count >= goal.Click.Count;
    }
    public bool SwapsCompare(ref Numbers goal) => Swaps == goal.Swaps;
    public bool MatchCompare(ref Numbers goal) => Match == goal.Match;
}

public ref struct RefTuple<T> where T : unmanaged
{
    public ref T item1;
    public ref T item2;

    public RefTuple(ref T item1)
    {
        this.item1 = ref item1;
    }
    public RefTuple(ref T item1, ref T item2) : this(ref item1)
    {
        this.item2 = ref item2;
    }
}

public sealed class GameState
{
    public ref Numbers DataByTile()
    {
        return ref new RefTuple<Numbers>(ref CollectionsMarshal.GetValueRefOrAddDefault(_tileData, Current, out _)).item1;
    }
    public ref Numbers DataByType()
    {
        return ref 
            new RefTuple<Numbers>(
                ref CollectionsMarshal.GetValueRefOrAddDefault
                                    (_typeData, Current.Body.TileType , out _)).item1;
    }

    public bool WasSwapped;
    private Dictionary<TileType, Numbers> _typeData { get; }
    private Dictionary<Tile, Numbers> _tileData { get; }
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

public enum EventType : byte
{
    Click, Swap, Match
}

public static class SingletonManager
{
    public static readonly Dictionary<Type, QuestHandler> _storage = new();

    public static T GetOrCreateInstance<T>() where T : QuestHandler, new()
    {
        lock(_storage)
        {
            if (_storage.TryGetValue(typeof(T), out var toReturn))
            {
                return (T)toReturn;
                //got val to return
            }
            else
            {

                toReturn = new T();
                _storage.Add(typeof(T), toReturn);
            }

            return (T)toReturn;
        }
    }
}

public abstract class QuestHandler
{
    private static readonly Dictionary<Tile, Numbers> TileData = new(Game.Level.GridWidth * Game.Level.GridHeight);
    private static readonly Dictionary<TileType, Numbers> TypeData = new((int)TileType.Length);

    protected internal ref Numbers DataBy(Tile key)
    {
        ref var tmp = ref CollectionsMarshal.GetValueRefOrAddDefault(TileData, key, out _);
        return ref new RefTuple<Numbers>(ref tmp).item1;
    }
    protected internal ref Numbers DataByType(TileType key)
    {
        ref var tmp = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeData, key, out _);
        return ref new RefTuple<Numbers>(ref tmp).item1;
    }
    
    public bool IsActive {get; private set;}
    
    protected static THandler GetInstance<THandler>() 
        where THandler : QuestHandler, new() => SingletonManager.GetOrCreateInstance<THandler>();
    
    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!

    protected bool IsSubGoalReached<T>(T key, EventType eventType) where  T: notnull
    {
        switch (key)
        {
            case Tile tile:
            {
                var state = Game.State;
                ref var tileData = ref state.DataByTile();
                ref var goalData = ref DataBy(tile);

                return eventType switch
                {
                    EventType.Click => tileData.ClickCompare(ref goalData),
                    EventType.Swap => tileData.SwapsCompare(ref goalData),
                    EventType.Match => tileData.MatchCompare(ref goalData),
                    _ => false
                };
            }
            case TileType t:
            {
                var state = Game.State;
                ref var typeData = ref state.DataByType();
                ref var goalData = ref DataByType(t);

                return eventType switch
                {
                    EventType.Click => typeData.ClickCompare(ref goalData),
                    EventType.Swap => typeData.SwapsCompare(ref goalData),
                    EventType.Match => typeData.MatchCompare(ref goalData),
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
        bool success = SingletonManager._storage.TryAdd(GetType(), this);
        IsActive = true;
    }
    public void UnSubscribe()
    {
        if (IsActive)
        {
            bool success = SingletonManager._storage.Remove(GetType());
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
            ref Numbers numbers = ref DataByType(i);

            switch (Game.Level.ID)
            {
                case 0:
                    numbers.Swaps.AllowedSwaps = Utils.Randomizer.Next(6, 8);
                    numbers.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 6f);
                    break;
                case 1:
                    numbers.Swaps.AllowedSwaps = Utils.Randomizer.Next(4, 6);
                    numbers.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 4f);
                    break;
                case 2:
                    numbers.Swaps.AllowedSwaps = Utils.Randomizer.Next(3, 5);
                    numbers.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 3f);
                    break;
                case 3:
                    numbers.Swaps.AllowedSwaps = Utils.Randomizer.Next(1, 2);
                    numbers.Swaps.Seconds = (int)(Game.Level.GameBeginAt / 10f);
                    break;
            }
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
            ref Numbers _numbers = ref DataByType(i);

            _numbers.Match = Game.Level.ID switch
            {
                //at some later point I will decide how if and how to check if a 
                //certain match was finished in an intervall! but for now we only check 
                //until gameover, if the needed matchtypes were collected
                0 => (4, null),
                1 => (6, null),
                2 => (7, null),
                3 => (9, null),
                _ => _numbers.Match
            };
            
            int matchesNeeded = _numbers.Match.Count;

            int matchSum = matchesNeeded * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = state.TotalAmountPerType[(int)i];

            if (matchSum < maxAllowed)
                _numbers.Match.Count = maxAllowed / Level.MAX_TILES_PER_MATCH;
        }
    }
    
    private bool IsMatchGoalReached()
    {
        return false;
    }
    
    protected override void HandleEvent()
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        GameState state = Game.State;

        if (IsMatchGoalReached())
        {
            ref var current = ref state.DataByType().Match;
            current = default;
            //state.WasGameWonB4Timeout = self.Count == 0;
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
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
        GameState state = Game.State;

        ref Numbers goal = ref DataBy(state.Current);

        goal.Click = Game.Level.ID switch
        {
            //you have to click Count-times with only "maxTime" seconds in-between for the next click
            0 => (Utils.Randomizer.Next(1, 4), 3f),
            1 => (Utils.Randomizer.Next(5, 7), 2.5f),
            2 => (Utils.Randomizer.Next(7, 9), 3.5f),
            3 => (Utils.Randomizer.Next(9, 12), 4f),
            _ => goal.Click
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
        ref var currClicks = ref state.DataByTile();

        if (IsTmpClickGoalReached())
        {
            state.Enemy.Disable(true);
            state.Enemy.BlockSurroundingTiles(state.Grid, false);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            currClicks.Click = default;
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
        ref var currClick = ref state.DataByType().Click;
        ref var goalClick = ref DataBy(state.Current).Click;
        
        if (IsTmpClickGoalReached() && !IsSoundPlaying(AssetManager.Splash))
        {
            //state.DefaultTile.Body.ToConstColor(rndColors[Utils.Randomizer.Next(0,rndColors.Length-1)]);
            var tile = Bakery.CreateTile(state.Current.GridCell, Utils.Randomizer.NextSingle());
            state.Grid[tile.GridCell] = tile;
            PlaySound(AssetManager.Splash);
            state.Current = tile;
            DefineGoals();
            currClick = default;
            Console.WriteLine("Nice, you got a new tile!");
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {state.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goalClick.Count - currClick.Count} more clicks!");
        }
    }
    
    public static TileReplacerOnClickHandler Instance => GetInstance<TileReplacerOnClickHandler>();
}