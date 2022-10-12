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
    public Type CurrentType => (DefaultTile.Body as TileShape)!.TileType;
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

public abstract class QuestHandler<T> where T:notnull
{
    private IDictionary<T, Numbers> _goal { get; }
    
    protected ref Numbers GetData(T key)
    {
        return ref new Ref<Numbers>(ref CollectionsMarshal.GetValueRefOrAddDefault((Dictionary<T, Numbers>)_goal, key, out _))._ref;
    }

    protected int Count => _goal.Count;

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    protected QuestHandler()
    {
        _goal = new Dictionary<T, Numbers>((int)Type.Length * 5);

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

    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    protected override void HandleEvent(GameState state)
    {
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

public class MatchQuestHandler : QuestHandler<Type>
{
    public MatchQuestHandler()
    {
        Game.OnMatchFound += HandleEvent;
    }

    protected override void DefineGoals(GameState state)
    {
        ref Numbers _numbers = ref GetData(Type.Empty);
        
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
    
    private bool IsMatchGoalReached(GameState state)
    {
        ref var goal = ref GetData(state.CurrentType).Match;
        ref var current = ref state.GetData().Match;
        return goal.Count == current.Count;
    }
    
    protected override void HandleEvent(GameState state)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!
        if (IsMatchGoalReached(state))
        {
            state.WasGameWonB4Timeout = Count == 0;
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
            ref Numbers _numbers = ref GetData(i);

            _numbers.Click = Game.Level.ID switch
            {
                //you have to click Count-times with only "maxTime" seconds in-between for the next click
                0 => (4, 3f),
                1 => (6, 2.5f),
                2 => (7, 2f),
                3 => (9, 4),
                _ => _numbers.Click
            };
        }
    }
    
    private bool ClickGoalReached(GameState inventory)
    {
        ref var currClick = ref inventory.GetData().Click;
        ref var goalClick = ref GetData(inventory.CurrentType).Click;

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

            ref var clicks = ref state.GetData().Click;
            clicks = (0, 0);

            state.EnemiesStillPresent = ++_matchXCounter < state.Matches!.Count;
            _matchXCounter = (byte)(state.EnemiesStillPresent ? _matchXCounter : 0);
            Console.WriteLine(nameof(state.EnemiesStillPresent) + ": " + state.EnemiesStillPresent);
        }
        else
        {
            //Console.WriteLine($"SADLY YOU STILL NEED {_goal.GetData(state.CurrentType).Click.Count}");
        }
    }
}