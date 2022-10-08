namespace Match_3.GameTypes;

public sealed class QuestState
{
    public bool WasSwapped;
    public float ElapsedTime;
    public Type[] SwappedTiles;
    public IDictionary<Type, Numbers> EventData { get; init; }
    public bool EnemiesStillPresent;
    public int[] TotalAmountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Type MatchTrigger;
    public Grid Grid;
}

public abstract class QuestHandler
{
    protected sealed record GoalPerType
    {
        public IDictionary<Type, Numbers> EventData;
    }

    protected GoalPerType _goalPerType { get; private set; }
    
    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    
    protected QuestHandler()
    {
        _goalPerType = new();
        _goalPerType.EventData = new Dictionary<Type, Numbers>((int)Type.Length);
    }

    protected abstract void DefineGoals(QuestState? inventory);
    protected abstract void HandleMatch(QuestState inventory);

    public static void InitAllQuestHandlers(int levelID)
    {
        // INIT all Sub_QuestHandlers here!...
        _ = new SwapQuestHandler();
        _ = new MatchQuestHandler();
    }
}

file class SwapQuestHandler : QuestHandler
{    
    protected override void DefineGoals(QuestState? inventory)
    {
        Numbers eventData = default;
        
        for (Type i = 0; i < Type.Length; i++)
        {
            switch (Game.Level.ID)
            {
                case 0:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(6, 8);
                    eventData.Swaps.CountDown = (int)(Game.Level.GameBeginAt / 6f); 
                    break;
                case 1:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(4, 6);
                    eventData.Swaps.CountDown = (int)(Game.Level.GameBeginAt / 4f); 
                    break;
                case 2:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(3, 5);
                    eventData.Swaps.CountDown = (int)(Game.Level.GameBeginAt / 3f); 
                    break;
                case 3:
                    eventData.Swaps.AllowedSwaps = Utils.Randomizer.Next(1, 2);
                    eventData.Swaps.CountDown = (int)(Game.Level.GameBeginAt / 10f); 
                    break;
            }

            _goalPerType.EventData.TryAdd(i, eventData);
        }
    }

    public SwapQuestHandler()
    {
        Game.OnTileSwapped += HandleMatch;
    }
    
    protected override void HandleMatch(QuestState inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            _goalPerType.EventData.TryGetValue(i, out var goalData);
            //The Game notifies the QuestHandler, when something happens to the tile!
            //Game -------> QuestHandler--->takes "QuestState" does == with _goalPerType and based on the comparison, it decides what to do!

            bool success = inventory.EventData.TryGetValue(i, out var inventoryData);

            if (success && inventoryData.Swaps == goalData.Swaps)
            {
                //EventData.Remove(inventory.CollectPair.ballType);
                Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
            }
        }
    }
}

file class MatchQuestHandler : QuestHandler
{
    private Numbers _numbers;
    
    public MatchQuestHandler()  
    {
        _numbers.Match = Game.Level.ID switch
        {
            0 => (4, 4),
            1 => (6, 3),
            2 => (7, 2),
            3 => (9, 4),
            _ => _numbers.Match
        };
        Grid.NotifyOnGridCreationDone += DefineGoals;
        Game.OnMatchFound += HandleMatch;
    }

    protected override void DefineGoals(QuestState? inventory)
    {
        if (inventory is null)
            throw new ArgumentException("Inventory has to have values or we cannot build the QUestHandler");
        
        for (Type i = 0; i < Type.Length; i++)
        {
            int matchesNeeded = _numbers.Match.Count;
            
            int matchSum = matchesNeeded * Level.MAX_TILES_PER_MATCH;
            int maxAllowed = inventory.TotalAmountPerType[(int)i];
            
            if (matchSum < maxAllowed)
                _numbers.Match.Count = matchesNeeded;
            else
                _numbers.Match.Count = maxAllowed / Level.MAX_TILES_PER_MATCH ;
            
            _goalPerType.EventData.TryAdd(i, _numbers);
        }
    }

    protected override void HandleMatch(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with _goalPerType and based on the comparison, it decides what to do!
        _goalPerType.EventData.TryGetValue(inventory.MatchTrigger, out var existent);
        
        bool success = inventory.EventData.TryGetValue(inventory.MatchTrigger, out var goal);
 
        if (success && goal.Match == existent.Match)
        {
            inventory.WasGameWonB4Timeout = _goalPerType.EventData.Count == 0;
            _goalPerType.EventData.Remove(inventory.MatchTrigger);
            Console.WriteLine("YEA YOU GOT A MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}
/*
file class ClickQuestHandler : QuestHandler
{
    public ClickQuestHandler(int levelId) : base(levelId)
    {
        Game.OnTileClicked += HandleMatch;
    }

    protected override void DefineGoals(QuestState inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            _goalPerType.EventData.TryAdd(i, _goalPerType.ClicksPerTileNeeded);
        }
    }

    private void CheckEnemy(Tile enemyTile)
    {
        if (enemyTile is EnemyTile e)
        {
        }
    }

    protected override void HandleMatch(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with _goalPerType and based on the comparison, it decides what to do!
        _goalPerType.EventData.TryGetValue(inventory.CollectPair.ballType, out int clickCount);

        if (inventory.TilesClicked.count == clickCount &&
            (int)inventory.ElapsedTime >=
            (int)_goalPerType.GoalTime)
        {
            if (inventory.EnemiesStillPresent)
            {
                inventory.Enemy.Disable(true);
                inventory.Enemy.BlockSurroundingTiles(inventory.Grid, false);
                inventory.TilesClicked.count = 0;
            }

            inventory.EnemiesStillPresent = _matchCounter < _level.MatchConstraint;
            //inventory.WasGameWonB4Timeout = _goalPerType.EventData.Count == 0;
            _goalPerType.EventData.Remove(inventory.TilesClicked.ballType);
            Console.WriteLine("YEA YOU DELETED THE EVIL-MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}*/