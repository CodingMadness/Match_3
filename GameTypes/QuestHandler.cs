namespace Match_3.GameTypes;

public abstract class QuestHandler
{
    protected readonly IDictionary<Type, int> TilesPerCount;
    protected Goal _goal { get; private set; }

    //For instance:
    //Collect N-Type1, N-Type2, N-Type3 up to maxTilesActive
    //----->within a TimeSpan of X-sec
    //----->without any miss-swap!
    //the goal is to make per new Level the Quests harder!!
    private void GenerateQuestBasedOnLevel(int levelID)
    {
        _goal = new();

        switch (levelID)
        {
            case 0:
                _goal.GoalTime = 30f;
                _goal.TypeCountToCollect = 3;
                _goal.ClicksPerTileNeeded = 3;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(1, 3);
                _goal.MissedSwapsTolerance = 6;
                break;
            case 1:
                _goal.GoalTime = 25f;
                _goal.TypeCountToCollect = 4;
                _goal.ClicksPerTileNeeded = 4;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(3, 5);
                _goal.MissedSwapsTolerance = 4;
                break;
            case 2:
                _goal.GoalTime = 20f;
                _goal.TypeCountToCollect = 5;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(4, 6);
                _goal.MissedSwapsTolerance = 2;
                break;
            case 3:
                _goal.GoalTime = 17f;
                _goal.TypeCountToCollect = 6;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(5, 9);
                _goal.MissedSwapsTolerance = 0;
                break;
        }
    }

    protected QuestHandler(int levelId)
    {
        TilesPerCount = new Dictionary<Type, int>((int)Type.Length);
        Grid.NotifyOnGridCreationDone += DefineTileTypeToCountRelation;
        GenerateQuestBasedOnLevel(levelId);
    }

    protected abstract void DefineTileTypeToCountRelation(QuestState inventory);
    protected abstract void DoSmthWhenGoalReached(QuestState inventory);

    public static void InitGameEventSubscriber(int levelID)
    {
        // INIT all Sub_QuestHandlers here!...
        _ = new SwapQuestHandler(levelID);
        _ = new CollectQuestHandler(levelID);
    }
}

public class SwapQuestHandler : QuestHandler
{
    public SwapQuestHandler(int levelId) : base(levelId)
    {
        TilesPerCount.Clear();
        Game.OnTileSwapped += DoSmthWhenGoalReached;
    }
    
    protected override void DefineTileTypeToCountRelation(QuestState inventory)
    {
        for (Type i = 0; i < Type.Length; i++)
        {
            int missSwapsAllowed = Utils.Randomizer.Next(1, _goal.MissedSwapsTolerance);
            TilesPerCount.TryAdd(i, missSwapsAllowed);
        }
    }

    protected override void DoSmthWhenGoalReached(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with Goal and based on the comparison, it decides what to do!
        TilesPerCount.TryGetValue(inventory.Swapped.ballType, out int swapCount);

        if (inventory.Swapped.count == swapCount)
        {
            //TilesPerCount.Remove(inventory.CollectPair.ballType);
            Console.WriteLine("NOW YOU CAN DO SMTH WITH THE INFO THAT HE SWAPPED TILE X AND Y");
        }
    }
}

public class CollectQuestHandler : QuestHandler
{
    protected override void DefineTileTypeToCountRelation(QuestState inventory)
    {
        for (int currentBall = 0; currentBall < _goal.TypeCountToCollect; currentBall++)
        {
            var matchesNeeded = _goal.MatchCountPerTilesToCollect;

            int matchSum = matchesNeeded * Goal.MAX_TILES_PER_MATCH;

            if (matchSum < inventory.TotalCountPerType[currentBall])
                TilesPerCount.TryAdd((Type)currentBall, matchesNeeded);
            else
                TilesPerCount.TryAdd((Type)currentBall, matchSum - inventory.TotalCountPerType[currentBall]);
        }
    }

    public CollectQuestHandler(int levelId) : base(levelId)
    {
        Game.OnMatchFound += DoSmthWhenGoalReached;
    }

    protected override void DoSmthWhenGoalReached(QuestState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "QuestState" does == with Goal and based on the comparison, it decides what to do!
        TilesPerCount.TryGetValue(inventory.Swapped.ballType, out int swapCount);

        if (inventory.CollectPair.collected == swapCount &&
            (int)inventory.ElapsedTime >=
            (int)_goal.GoalTime)
        {
            inventory.WasGameWonB4Timeout = TilesPerCount.Count == 0;
            TilesPerCount.Remove(inventory.CollectPair.ballType);
            // inventory.CollectPair.collected = 0;
            Console.WriteLine("YEA YOU GOT A MATCH AND ARE REWARDED FOR IT !: ");
        }
    }
}

