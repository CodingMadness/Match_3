using JetBrains.Annotations;

namespace Match_3.GameTypes;

public readonly record struct Level(int ID, int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight, int TileSize,
    byte[] MapLayout)
{
    public int WindowHeight => GridWidth * TileSize;
    public int WindowWidth => GridHeight * TileSize;
}

public sealed class Goal
{
    public const int MAX_MATCHES_TO_COLLECT = 3;
    public const int MAX_TILES_PER_MATCH = 5;
    public float GoalTime;
    public int ClicksPerTileNeeded;
    public int TypeCountToCollect;
    public int MatchCountPerTilesToCollect;
    public int MissedSwapsAllowed;
}

public sealed class GameState
{
    public float ElapsedTime;
    public (Type ballType, int Count) TilesClicked;
    public (Type ballType, int collected) CollectPair;
    public (Type ballType, int swapped) Swapped;
    public bool AreEnemiesStillPresent;
    public int[] TotalCountPerType;
    public bool WasGameWonB4Timeout;
    public EnemyTile Enemy;
    public Grid Map;
}

public abstract class QuestHandler
{
    private readonly int[] _totalCountsPerTile;
    protected readonly IDictionary<Type, int> TilesPerCount;

    protected Goal _goal { get; private set; }
    private GameState _currentState;

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
                _goal.MissedSwapsAllowed = 6;
                break;
            case 1:
                _goal.GoalTime = 25f;
                _goal.TypeCountToCollect = 4;
                _goal.ClicksPerTileNeeded = 4;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(3, 5);
                _goal.MissedSwapsAllowed = 4;
                break;
            case 2:
                _goal.GoalTime = 20f;
                _goal.TypeCountToCollect = 5;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(4, 6);
                _goal.MissedSwapsAllowed = 2;
                break;
            case 3:
                _goal.GoalTime = 17f;
                _goal.TypeCountToCollect = 6;
                _goal.ClicksPerTileNeeded = 5;
                _goal.MatchCountPerTilesToCollect = Utils.Randomizer.Next(5, 9);
                _goal.MissedSwapsAllowed = 0;
                break;
        }
    }

    protected QuestHandler(int levelIdId)
    {
        TilesPerCount = new Dictionary<Type, int>((int)Type.Length);
        GenerateQuestBasedOnLevel(levelIdId);
    }
}

public class CollectQuestHandler : QuestHandler
{
    private void DefineCollectionGoal(GameState inventory)
    {
        for (int currentBall = 0; currentBall < _goal.TypeCountToCollect; currentBall++)
        {
            var matchesNeeded = _goal.MatchCountPerTilesToCollect;

            int matchSum = matchesNeeded * Goal.MAX_TILES_PER_MATCH;
                
            if (matchSum < inventory.TotalCountPerType[currentBall])
                TilesPerCount.TryAdd((Type)currentBall, matchesNeeded);
            else
                TilesPerCount.TryAdd((Type)currentBall,  matchSum - inventory.TotalCountPerType[currentBall]);
        }
    }

    public CollectQuestHandler(int levelIdId) : base(levelIdId)
    {
        Game.OnMatchFound += CompareStateWithGoal;
        Grid.NotifyOnGridCreationDone += DefineCollectionGoal;
    }
    
    private void CompareStateWithGoal(GameState inventory)
    {
        //The Game notifies the QuestHandler, when a matchX happened or a tile was swapped
        //or about other events
        //Game -------> QuestHandler--->takes "GameState" does == with Goal and based on the comparison, it decides what to do!
        TilesPerCount.TryGetValue(inventory.CollectPair.ballType, out int collectCount);

        if (inventory.CollectPair.collected == collectCount && 
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