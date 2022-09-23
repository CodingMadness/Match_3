using System.Numerics;

namespace Match_3.GameTypes;

public class Level
{
    public Level(int GameStartAt, int GameOverScreenTime,
        int TilemapWidth, int TilemapHeight,
        (int level, Dictionary<Balls, int> Quest) QuestPerLevel,
        int TileSize)
    {
        this.GameStartAt = GameStartAt;
        this.GameOverScreenTime = GameOverScreenTime;
        this.TilemapWidth = TilemapWidth;
        this.TilemapHeight = TilemapHeight;
        this.TileSize = TileSize;
        this.QuestPerLevel = QuestPerLevel;
    }

    public bool ShallMakeRandomQuest { get; set; }
    public int WINDOW_HEIGHT => TilemapHeight * TileSize;
    public (int level, Dictionary<Balls, int> Quest) QuestPerLevel { get; private set; }

    public void SetNextLevel()
    {
        var tmp = QuestPerLevel;
        tmp.level++;
        tmp.Quest.Clear();
        QuestPerLevel = tmp;
    }
    //public (int level, Dictionary<Balls, int> Quest) QuestPerLevel { get; private set; };

    public int WINDOW_WIDTH => TilemapWidth * TileSize;
    public Vector2 TopCenter => new Vector2(WINDOW_WIDTH / 2, 0) - Vector2.UnitX * 25;
    public Vector2 Center => new(WINDOW_WIDTH / 2, WINDOW_HEIGHT / 2);
    public int GameOverScreenTime { get; }
    public int GameStartAt { get; set; }
    public int TilemapWidth { get; set; }
    public int TilemapHeight { get; set; }
    public int TileSize { get; }

    /*
    public void Deconstruct(out int GameStartAt, out int GameOverScreenTime, out int TilemapWidth, out int TilemapHeight, out (int level, Dictionary<Balls, int> Quest) QuestPerLevel, out int TileSize, out int MaxAllowedSpawns)
    {
        GameStartAt = this.GameStartAt;
        GameOverScreenTime = this.GameOverScreenTime;
        TilemapWidth = this.TilemapWidth;
        TilemapHeight = this.TilemapHeight;
        QuestPerLevel = this.QuestPerLevel;
        TileSize = this.TileSize;
        MaxAllowedSpawns = this.MaxAllowedSpawns;
    }
}
*/
}