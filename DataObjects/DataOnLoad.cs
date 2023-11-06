namespace Match_3.DataObjects;

/// <summary>
/// This Record defines data which has to be known BEFORE the actual Game runs!
/// </summary>
/// <param name="Id">The current levelID, like 0, 1, 2: the higher the number the harder the level gets</param>
/// <param name="GameBeginAt">The startup time of the Game</param>
/// <param name="GameOverScreenCountdown">The countdown which runs to 0, and so long you see a "GameOverScreen"</param>
/// <param name="GridWidth">an integer number defining the count of tiles PER WIDTH</param>
/// <param name="GridHeight">an integer number defining the count of tiles PER HEIGHT</param>
public record DataOnLoad(int Id, 
    int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight)
{
    public const int MaxTilesPerMatch = 3;
    public const int TileSize = 64 / 1;
    public const int TileColorCount = 12;
    public int WindowHeight => GridHeight * TileSize;
    public int WindowWidth => GridWidth * TileSize;

    public Quest[] Quests;

    public int QuestCount;
    public byte CountForAllColors;
}
