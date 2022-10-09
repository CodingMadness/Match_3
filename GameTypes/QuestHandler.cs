using System.Collections.Concurrent;

namespace Match_3.GameTypes;

public struct Numbers
{
    /// <summary>
    /// Count-Clicks, with maxTime inbetween them
    /// </summary>
    public (int Count, float Intervall) Click;
    public (int AllowedSwaps, float Intervall) Swaps;
    public (int Count, float Intervall) Match;
}

public sealed class GameState
{
    public bool WasSwapped;
    public float ElapsedTime;
    public TileShape Current;
    public TileShape? B;
    public IDictionary<Type, Numbers> EventData { get; init; }

    private Numbers _numbers;

    public ref readonly Numbers Load()
    {
        EventData.TryGetValue(Current.TileType, out _numbers);
        return ref _numbers;
    }

    public void Update() => EventData.TryAdd((Current as TileShape)!.TileType, _numbers);
    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Grid;
}

public abstract class QuestHandler<T>
{
    protected sealed record GoalPer<T>
    {
        public readonly IDictionary<T, Numbers> EventData =
            new Dictionary<T, Numbers>((int)Type.Length);

        private Numbers _numbers;

        public ref readonly Numbers LoadBy(T Key)
        {
            EventData.TryGetValue(Key, out _numbers);
            return ref _numbers;
        }

        public void Update(T current) => EventData.TryAdd(current, _numbers);
    }

    protected GoalPer<T> _goal { get; }

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    protected QuestHandler()
    {
        _goal = new();
    }

    protected abstract void DefineGoals(GameState? inventory);
    protected abstract void HandleEvent(GameState inventory);

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
    protected override void DefineGoals(GameState? inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            Numbers eventData = _goal.LoadBy(i);

            switch (Game.Level.ID)
            {
                case 0:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(6, 8);
                    eventData.Swaps.Intervall = (int)(Game.Level.GameBeginAt / 6f);
                    break;
                case 1:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(4, 6);
                    eventData.Swaps.Intervall = (int)(Game.Level.GameBeginAt / 4f);
                    break;
                case 2:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(3, 5);
                    eventData.Swaps.Intervall = (int)(Game.Level.GameBeginAt / 3f);
                    break;
                case 3:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(1, 2);
                    eventData.Swaps.Intervall = (int)(Game.Level.GameBeginAt / 10f);
                    break;
            }

            _goal.Update(i, eventData);
        }
    }

    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleEvent;
    }

    protected override void HandleEvent(GameState inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            _goal.EventData.TryGetValue(i, out var goalData);
            //The Game notifies the QuestHandler, when something happens to the tile!
            //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!

            bool success = inventory.EventData.TryGetValue(i, out var inventoryData);

            if (success && inventoryData.Swaps == goalData.Swaps)
            {
                //EventData.Remove(inventory.CollectPair.ballType);
                Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
            }
        }
    }
}

public class MatchQuestHandler : QuestHandler<Type>
{
    public MatchQuestHandler()
    {
        Grid.NotifyOnGridCreationDone += DefineGoals;
        Game.OnMatchFound += HandleEvent;
    }

    protected override void DefineGoals(GameState? inventory)
    {
        if (inventory is null)
            throw new ArgumentException("Inventory has to have values or we cannot build the QUestHandler");

        Numbers _numbers = _goal.LoadBy(inventory.Current.TileType);
        
        _numbers.Match = Game.Level.ID switch
        {
            0 => (4, 4),
            1 => (6, 3),
            2 => (7, 2),
            3 => (9, 4),
            _ => _numbers.Match
        };
        
        for (Type i = 0; i < Type.Length; i++)
        {
            int matchesNeeded = _numbers.Match.Count;

            int matchSum = matchesNeeded * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = inventory.TotalAmountPerType[(int)i];

            if (matchSum < maxAllowed)
                _numbers.Match.Count = matchesNeeded;
            else
                _numbers.Match.Count = maxAllowed / Level.MAX_TILES_PER_MATCH;

            _goal.Update(i);
        }
    }
    
    private bool MatchGoalPerTypeReached(GameState inventory)
    {
        var goal = _goal.LoadBy(inventory.Current.TileType);
        var existent = inventory.Load();
        return existent.Match == goal.Match;
    }
    
    protected override void HandleEvent(GameState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!

        if (MatchGoalPerTypeReached(inventory))
        {
            inventory.WasGameWonB4Timeout = _goal.EventData.Count == 0;
            //_goal.EventData.Remove((inventory.B as TileShape)!.TileType);
            Console.WriteLine("YEA YOU GOT current MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}

public class ClickQuestHandler : QuestHandler<Tile>
{
    private Numbers _numbers;

    public ClickQuestHandler()
    {
        Game.OnTileClicked += HandleEvent;
    }

    private bool ClickGoalReached(GameState inventory)
    {
        bool success = inventory.Load()
        bool success2 = _goal.EventData.TryGetValue(inventory.Current, out var goal);
        return success && success2 && existent.Click == goal.Click;
    }
    
    protected override void DefineGoals(GameState? inventory)
    {
        _numbers.Click = Game.Level.ID switch
        {
            //you have to click Count-times with only "maxTime" seconds in-between for the next click
            0 => (4, 3f),
            1 => (6, 2.5f),
            2 => (7, 2f),
            3 => (9, 4),
            _ => _numbers.Click
        };
        for (Type i = 0; i < Type.Length; i++)
        {
            _goal.EventData.TryAdd(i, _numbers);
        }
    }

    protected override void HandleEvent(GameState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with _goal and based on the comparison, it decides what to do!

        if (ClickGoalReached(inventory))
        {
            if (inventory.EnemiesStillPresent)
            {
                inventory.Enemy.Disable(true);
                inventory.Enemy.BlockSurroundingTiles(inventory.Grid, false);
               
                if (inventory.EventData.TryGetValue(inventory.Current, out var existent))
                {
                       
                }
            }

            //inventory.EnemiesStillPresent = _matchCounter < _level.MatchConstraint;
            //inventory.WasGameWonB4Timeout = _goal.EventData.Count == 0;
           // _goal.EventData.Remove(inventory.TilesClicked.ballType);
            Console.WriteLine("YEA YOU DELETED THE EVIL-MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}