using System.Runtime.InteropServices;
using Raylib_CsLo;
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
}

public ref struct Ref<T> where T : unmanaged
{
    public ref T _ref;

    public Ref(ref T @ref)
    {
        _ref = ref @ref;
    }
}

public sealed class GameState
{
    public ref Numbers GetData()
    {
        return ref new Ref<Numbers>(ref CollectionsMarshal.GetValueRefOrAddDefault((Dictionary<Type, Numbers>)_eventData, CurrentType, out _))._ref;
    }

    public bool WasSwapped;
    public Tile DefaultTile { get; set; }
    public Type CurrentType => DefaultTile.Body.TileType;
    private IDictionary<Type, Numbers> _eventData { get; }
    public MatchX? Matches { get; set; }

    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Grid;

    public GameState(int initSize)
    {
        _eventData = new Dictionary<Type, Numbers>(initSize);
    }
}

static file class SingletonManager
{
    public static readonly Dictionary<System.Type, QuestHandler> _storage = new();

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
    public bool IsActive {get; set;}

    private static readonly IDictionary<Type, Numbers> _goal = 
            new Dictionary<Type, Numbers>((int)Type.Length * 5);
    
    protected ref Numbers GetData<T>(T key) where T: notnull
    {
        ref var tmp = ref CollectionsMarshal.GetValueRefOrAddDefault((Dictionary<T, Numbers>)_goal, key, out _);
        return ref new Ref<Numbers>(ref tmp)._ref;
    }

    protected static THandler GetInstance<THandler>() 
        where THandler : QuestHandler, new() => SingletonManager.GetOrCreateInstance<THandler>();

    protected virtual int Count => _goal.Count;

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    protected QuestHandler()
    {
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
        //DestroyOnClickHandler.Instance.Init();
    }
    public void Subscribe()
    {
        bool success = SingletonManager._storage.TryAdd(this.GetType(), this);
        IsActive = true;
    }
    public void UnSubscribe()
    {
        if (IsActive)
        {
            bool success = SingletonManager._storage.Remove(this.GetType());
            IsActive = false;
        }
    }
}

public class SwapQuestHandler : QuestHandler
{
    protected override void DefineGoals()
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            ref Numbers numbers = ref GetData(i);

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

    protected override void HandleEvent()
    {
        GameState state = Game.State;

        for (Type i = 0; i < Type.Length; i++)
        {
            ref Numbers numbers = ref GetData(i);
            //The Game notifies the QuestHandler, when something happens to the tile!
            //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
            ref var currSwaps = ref state.GetData().Swaps;
            
            if (currSwaps == numbers.Swaps)
            {
                //EventData.Remove(state.CollectPair.ballType);
                Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
            }
        }
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
        ref Numbers _numbers = ref GetData(Type.Empty);
        GameState state = Game.State;

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
        
        for (Type i = 0; i < Type.Length; i++)
        {
            int matchesNeeded = _numbers.Match.Count;

            int matchSum = matchesNeeded * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = state.TotalAmountPerType[(int)i];

            if (matchSum < maxAllowed)
                _numbers.Match.Count = matchesNeeded;
            else
                _numbers.Match.Count = maxAllowed / Level.MAX_TILES_PER_MATCH;
        }
    }
    
    private bool IsMatchGoalReached()
    {
        GameState state = Game.State;
        ref var goal = ref GetData(state.CurrentType).Match;
        ref var current = ref state.GetData().Match;
        return goal.Count == current.Count;
    }
    
    protected override void HandleEvent()
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        GameState state = Game.State;

        if (IsMatchGoalReached())
        {
            state.WasGameWonB4Timeout = Count == 0;
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}

public abstract class ClickQuestHandler : QuestHandler
{
    protected readonly Dictionary<Tile, Numbers> _tileGoal;
    protected override int Count => _tileGoal.Count;
    protected ClickQuestHandler()
    {
        _tileGoal = new(((int)Type.Length));
        Game.OnTileClicked += HandleEvent;
    }
    protected override void Init() => Grid.OnTileCreated += DefineGoals;
    protected ref Numbers GetData(Tile key)
    {
        ref var tmp = ref CollectionsMarshal.GetValueRefOrAddDefault(_tileGoal, key, out _);
        return ref new Ref<Numbers>(ref tmp)._ref;
    }
    protected override void DefineGoals()
    {
        GameState state = Game.State;

        ref Numbers goal = ref GetData(state.DefaultTile);

        goal.Click = Game.Level.ID switch
        {
            //you have to click Count-times with only "maxTime" seconds in-between for the next click
            0 => (Utils.Randomizer.Next(1, 4), 3f),
            1 => (Utils.Randomizer.Next(5, 7), 2.5f),
            2 => (Utils.Randomizer.Next(7, 9), 3.5f),
            3 => (Utils.Randomizer.Next(9, 12), 4f),
            _ => goal.Click
        };
        System.Console.WriteLine($"{state.DefaultTile.GridCell} + {goal.Click.Count}");
        //System.Console.WriteLine($"{state.DefaultTile.GridCell}");
    }
    protected bool ClickGoalReached()
    {
        GameState state = Game.State;
        ref var currClick = ref state.GetData().Click;
        ref var goalClick = ref GetData(state.DefaultTile).Click;

        bool reached = currClick.Count == goalClick.Count;
                       //&& goalClicks.Seconds > currClicks.Seconds;

        return reached;
    }
}

public sealed class DestroyOnClickHandler : ClickQuestHandler
{
    private byte _matchXCounter;

    protected override void HandleEvent()
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        GameState state = Game.State;

        if (!IsActive)
            return;

        if (ClickGoalReached())
        {
            state.Enemy.Disable(true);
            state.Enemy.BlockSurroundingTiles(state.Grid, false);
            Console.WriteLine("YEA goal was reached, deleted the evil match!");

            ref var currClicks = ref state.GetData().Click;
            currClicks = (0, 0);
            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            Console.WriteLine(nameof(state.EnemiesStillPresent) + ": " + state.EnemiesStillPresent);
        }
        else
        {
            //Console.WriteLine($"SADLY YOU STILL NEED {_goal.GetData(state.CurrentType).Click.Count}");
        }
    }

    public static DestroyOnClickHandler Instance => GetInstance<DestroyOnClickHandler>();
}

public sealed class TileReplacerOnClickHandler : ClickQuestHandler
{        
    protected override void HandleEvent()
    {
        GameState state = Game.State;

        if (!IsActive)
            return;

        //The Game notifies the QuestHandler, when smth happened to a tile on the map!
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        ref var currClick = ref state.GetData().Click;
        ref var goalClick = ref GetData(state.DefaultTile).Click;
        
        if (ClickGoalReached() && !IsSoundPlaying(AssetManager.Splash))
        {
            //state.DefaultTile.Body.ToConstColor(rndColors[Utils.Randomizer.Next(0,rndColors.Length-1)]);
            var tile = Bakery.CreateTile(state.DefaultTile.GridCell, Utils.Randomizer.NextSingle());
            state.Grid[tile.GridCell] = tile;
            PlaySound(AssetManager.Splash);
            state.DefaultTile = tile;
            DefineGoals();
            currClick = default;
            System.Console.WriteLine("Nice, you got a new tile!");
        }
        else
        {
            //System.Console.WriteLine($"GOAL_CLICKS:  {goalClick.Count} at Cell: {state.DefaultTile.GridCell}" );
            Console.WriteLine($"SADLY YOU STILL NEED {goalClick.Count - currClick.Count} more clicks!");
        }
    }
    
    public static TileReplacerOnClickHandler Instance => 
                        GetInstance<TileReplacerOnClickHandler>();
}