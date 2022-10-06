namespace Match_3.GameTypes;

public abstract class QuestHandler
{
    protected sealed class Goal
    {
        public IDictionary<Type, int> TilesPerCount;
        public const int MAX_MATCHES_TO_COLLECT = 3;
        public const int MAX_TILES_PER_MATCH = 3;
        public float GoalTime;
        public int ClicksPerTileNeeded;
        public int CountPerTypeToCollect;
        public int MatchCountPerTilesToCollect;
        public int MissedSwapsTolerance;
    }
    protected Goal _goal { get; private set; }

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    private void InitGoalBasedOnLevelID(int levelID)
    {
        _goal = new()
        {
            TilesPerCount = new Dictionary<Type, int>((int)Type.Length)
        };
        switch (levelID)
        {
            case 0:
                _goal.GoalTime = 30f;
                _goal.CountPerTypeToCollect = 3;
                _goal.ClicksPerTileNeeded = 3;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(1, 3);
                _goal.MissedSwapsTolerance = 6;
                break;
            case 1:
                _goal.GoalTime = 25f;
                _goal.CountPerTypeToCollect = 4;
                _goal.ClicksPerTileNeeded = 4;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(3, 5);
                _goal.MissedSwapsTolerance = 4;
                break;
            case 2:
                _goal.GoalTime = 20f;
                _goal.CountPerTypeToCollect = 5;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(4, 6);
                _goal.MissedSwapsTolerance = 2;
                break;
            case 3:
                _goal.GoalTime = 17f;
                _goal.CountPerTypeToCollect = 6;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(5, 9);
                _goal.MissedSwapsTolerance = 0;
                break;
        }
    }

    protected QuestHandler(int levelId)
    {
        //TilesPerCount = new Dictionary<Type, int>((int)Type.Length);
        Grid.NotifyOnGridCreationDone += DefineTileTypeToCountRelation;
        InitGoalBasedOnLevelID(levelId);
    }

    protected abstract void DefineTileTypeToCountRelation(QuestState inventory);
    protected abstract void DoSmthWhenGoalReached(QuestState inventory);
    public static void InitAllQuestHandlers(int levelID)
    {
        // INIT all Sub_QuestHandlers here!...
        _ = new SwapQuestHandler(levelID);
        _ = new CollectQuestHandler(levelID);
    }
}

file class SwapQuestHandler : QuestHandler
{
    public SwapQuestHandler(int levelId) : base(levelId)
    {
        Game.OnTileSwapped += DoSmthWhenGoalReached;
    }

    protected override void DefineTileTypeToCountRelation(QuestState inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            int missSwapsAllowed = Utils.Randomizer.Next(1, _goal.MissedSwapsTolerance);
            _goal.TilesPerCount.TryAdd(i, missSwapsAllowed);
        }
    }

    protected override void DoSmthWhenGoalReached(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with Goal and based on the comparison, it decides what to do!
        _goal.TilesPerCount.TryGetValue(inventory.Swapped.ballType, out int swapCount);

        if (inventory.Swapped.count == swapCount)
        {
            //TilesPerCount.Remove(inventory.CollectPair.ballType);
            Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
        }
    }
}

file class CollectQuestHandler : QuestHandler
{
    public CollectQuestHandler(int levelId) : base(levelId)
    {
        Game.OnMatchFound += DoSmthWhenGoalReached;
    }
    
    protected override void DefineTileTypeToCountRelation(QuestState inventory)
    {
        for (int currentBall = 0; currentBall < _goal.CountPerTypeToCollect; currentBall++)
        {
            var matchesNeeded = _goal.MatchCountPerTilesToCollect;

            int matchSum = matchesNeeded * Goal.MAX_TILES_PER_MATCH;

            if (matchSum < inventory.TotalCountPerType[currentBall])
                _goal.TilesPerCount.TryAdd((Type)currentBall, matchesNeeded);
            else
                _goal.TilesPerCount.TryAdd((Type)currentBall, matchSum - inventory.TotalCountPerType[currentBall]);
        }
    }
    
    protected override void DoSmthWhenGoalReached(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with Goal and based on the comparison, it decides what to do!
        _goal.TilesPerCount.TryGetValue(inventory.CollectPair.ballType, out int collectCount);

        if (inventory.CollectPair.collected == collectCount &&
            (int)inventory.ElapsedTime >=
            (int)_goal.GoalTime)
        {
            inventory.WasGameWonB4Timeout = _goal.TilesPerCount.Count == 0;
            _goal.TilesPerCount.Remove(inventory.CollectPair.ballType);
            Console.WriteLine("YEA YOU GOT A MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}