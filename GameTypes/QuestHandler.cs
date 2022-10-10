using System.Runtime.InteropServices;

namespace Match_3.GameTypes;

public struct Numbers
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public (int Count, float Seconds) Click;
    public (int AllowedSwaps, float Seconds) Swaps;
    public (int Count, float? Seconds) Match;
}

public sealed class GameState
{
    public bool WasSwapped;
    public Tile DefaultTile { get; set; }
    public Type CurrentType => (DefaultTile.Body as TileShape)!.TileType;
    private IDictionary<Type, Numbers> _eventData { get; }
    public MatchX? Matches { get; set; }
    
    public ref Numbers FromRef()
    {
        if (EqualityComparer<Type>.Default.Equals(default))
            return ref _current;
            
        ref var current = ref CollectionsMarshal.GetValueRefOrAddDefault((Dictionary<Type, Numbers>)_eventData, CurrentType, out bool success);
        _current = current;
        return ref _current;
    }
    
    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Grid;
    private Numbers _current;

    public GameState(int initSize)
    {
        _eventData = new Dictionary<Type, Numbers>(initSize);
    }
}

public abstract class QuestHandler<T> where T:notnull
{
    protected sealed record GoalPer
    {
        public readonly IDictionary<T, Numbers> EventData =
            new Dictionary<T, Numbers>((int)Type.Length);

        private Numbers _num;
        
        public ref Numbers FromRef(T key)
        {
            if (EqualityComparer<T>.Default.Equals(default))
                return ref _num;
            
            _num = CollectionsMarshal.GetValueRefOrAddDefault((Dictionary<T, Numbers>)EventData, key, out bool success);
            
            return ref _num;
        }
        
        public void Add(T current) => EventData[current] = _num;
    }

    protected GoalPer _goal { get; }

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    protected QuestHandler()
    {
        _goal = new();
        Grid.NotifyOnGridCreationDone += DefineGoals;
    }

    /// <summary>
    /// This will be called automatically when Grid is done with its bitmap creation!
    /// </summary>
    /// <param name="state">The current State of the game, with all needed Data</param>
    protected abstract void DefineGoals(GameState state);
    protected abstract void HandleEvent(GameState state);

    public static void InitAllQuestHandlers()
    {
        // INIT all Sub_QuestHandlers here!...
        _ = new SwapQuestHandler();
        _ = new MatchQuestHandler();
        _ = new ClickQuestHandler();
    }
}

public class SwapQuestHandler : QuestHandler<Type>
{
    protected override void DefineGoals(GameState? state)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            ref Numbers numbers = ref _goal.FromRef(i);

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

            _goal.Add(i);
        }
    }

    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    protected override void HandleEvent(GameState state)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            ref Numbers numbers = ref _goal.FromRef(i);
            //The Game notifies the QuestHandler, when something happens to the tile!
            //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
            ref var currSwaps = ref state.FromRef().Swaps;
            
            if (currSwaps == numbers.Swaps)
            {
                //EventData.Remove(state.CollectPair.ballType);
                Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
            }
        }
    }
}

public class MatchQuestHandler : QuestHandler<Type>
{
    public MatchQuestHandler()
    {
        Game.OnMatchFound += HandleEvent;
    }

    protected override void DefineGoals(GameState state)
    {
        ref Numbers _numbers = ref _goal.FromRef(Type.Empty);
        
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

            _goal.Add(i);
        }
    }
    
    private bool IsMatchGoalReached(GameState state)
    {
        ref var goal = ref _goal.FromRef(state.CurrentType);
        ref var existent = ref state.FromRef();

        var goalMatch = goal.Match;
        var currMatch = existent.Match;
        return goalMatch.Count == currMatch.Count;
    }
    
    protected override void HandleEvent(GameState state)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        if (IsMatchGoalReached(state))
        {
            state.WasGameWonB4Timeout = _goal.EventData.Count == 0;
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}

public class ClickQuestHandler : QuestHandler<Type>
{
    public ClickQuestHandler()
    {
        Game.OnTileClicked += HandleEvent;
    }

    private byte _matchXCounter;
    
    protected override void DefineGoals(GameState state)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            ref Numbers _numbers = ref _goal.FromRef(i);

            _numbers.Click = Game.Level.ID switch
            {
                //you have to click Count-times with only "maxTime" seconds in-between for the next click
                0 => (4, 3f),
                1 => (6, 2.5f),
                2 => (7, 2f),
                3 => (9, 4),
                _ => _numbers.Click
            };
            _goal.Add(i);
        }
    }
    
    private bool ClickGoalReached(GameState inventory)
    {
        ref var currClick = ref inventory.FromRef().Click;
        ref var goalClick = ref _goal.FromRef(inventory.CurrentType).Click;

        bool reached = currClick.Count == goalClick.Count;
                       //&& goalClicks.Seconds > currClicks.Seconds;

        return reached;
    }
    
    protected override void HandleEvent(GameState state)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!

        if (ClickGoalReached(state))
        {
            state.Enemy.Disable(true);
            state.Enemy.BlockSurroundingTiles(state.Grid, false);
            Console.WriteLine("YEA goal was reached, deleted the evil match!");

            ref var clicks = ref state.FromRef().Click;
            clicks = (0, 0);

            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            Console.WriteLine(nameof(state.EnemiesStillPresent) + ": " + state.EnemiesStillPresent);
        }
        else
        {
            //Console.WriteLine($"SADLY YOU STILL NEED {_goal.FromRef(state.CurrentType).Click.Count}");
        }
    }
}