namespace Match_3.GameTypes;

public class Level
{
    public readonly int MatchConstraint;

    public Level(int gameStartAt, int gameOverScreenTime,
        int tilemapWidth, int tilemapHeight,
        (int level, Dictionary<Balls, int> Quest) questPerLevel,
        int tileSize, int matchConstraint)
    {
        MatchConstraint = matchConstraint;
        GameStartAt = gameStartAt;
        GameOverScreenTime = gameOverScreenTime;
        TilemapWidth = tilemapWidth;
        TilemapHeight = tilemapHeight;
        TileSize = tileSize;
        QuestPerLevel = questPerLevel;
        //ClickCountPerEnemy = clickCountPerEnemy;
    }

    public bool ShallMakeRandomQuest { get; set; }
    public int WindowHeight => TilemapHeight * TileSize;
    public (int level, Dictionary<Balls, int> Quest) QuestPerLevel { get; private set; }
        
    public int ClickCountPerEnemy { get; }
    public void SetNextLevel()
    {
        var tmp = QuestPerLevel;
        tmp.level++;
        tmp.Quest.Clear();
        QuestPerLevel = tmp;
    }
    //public (int level, Dictionary<Balls, int> Quest) QuestPerLevel { get; private set; };

    public int WindowWidth => TilemapWidth * TileSize;
    public int GameOverScreenTime { get; set; }
    public int GameStartAt { get; set; }
    public int TilemapWidth { get; set; }
    public int TilemapHeight { get; set; }
    public int TileSize { get; }
}