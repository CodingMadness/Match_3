using System.Numerics;

namespace Match_3.GameTypes;

public class Level
{
    public Level(int gameStartAt, int gameOverScreenTime,
        int tilemapWidth, int tilemapHeight,
        (int level, Dictionary<Balls, int> Quest) questPerLevel,
        int tileSize/*, int clickCountPerEnemy*/)
    {
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
    public Vector2 TopCenter => new Vector2(WindowWidth / 2, 0) - Vector2.UnitX * 25;
    public Vector2 Center => new((WindowWidth / 2), WindowHeight / 2);
    public int GameOverScreenTime { get; set; }
    public int GameStartAt { get; set; }
    public int TilemapWidth { get; set; }
    public int TilemapHeight { get; set; }
    public int TileSize { get; }
}