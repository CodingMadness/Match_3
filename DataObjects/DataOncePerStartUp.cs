using System.Numerics;

namespace Match_3.DataObjects;

/// <summary>
/// This Record defines data which has to be known BEFORE the actual Game runs!
/// </summary>
/// <param name="Id">The current levelID, like 0, 1, 2: the higher the number the harder the level gets</param>
/// <param name="GameBeginAt">The startup time of the Game</param>
/// <param name="GameOverScreenCountdown">The countdown which runs to 0, and so long you see a "GameOverScreen"</param>
/// <param name="GridWidth">an integer number defining the count of tiles PER WIDTH</param>
/// <param name="GridHeight">an integer number defining the count of tiles PER HEIGHT</param>
public readonly record struct Config(int Id, 
    int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight)
{
    public const int MaxTilesPerMatch = 3;
    public const int TileSize = 64 / 1;
    public const int TileColorCount = 11;
    public readonly int WindowHeight = GridHeight * TileSize;
    public readonly int WindowWidth = GridWidth * TileSize;
    public readonly Vector2 WindowInWorldCoordinates = new Vector2(GridWidth, GridHeight) * TileSize; 
    public readonly Vector2 WindowInGridCoordinates = new(GridWidth, GridHeight);

    /// <summary>
    /// This is the Number in which the below 'QuestLog' variable HAVE TO be iterated
    /// in order to get all the proper segements been drawn properly,
    /// </summary>
    public const int SegmentsOfQuestLog = 8;
    // QuestLog remains a computed string (now with instance access)
    public const string QuestLog = $"(Black) You have to collect ({Quest.TileColorName}\0\0\0\0\0\0) {Quest.MatchCountName} Matches " +
                                   $"(Black) for which you have in between those only really like ({Quest.TileColorName}\0\0\0\0\0\0) {Quest.MatchIntervalName} seconds left " +
                                   $"(Black) and also just ({Quest.TileColorName}\0\0\0\0\0\0) {Quest.SwapCountName} swaps available per match " +
                                   $"(Black) furthermore, you only are allowed to replace any given tile ({Quest.TileColorName}\0\0\0\0\0\0) {Quest.ReplacementCountName} times maximum " +
                                   $"(Black) for your own help as well as there is only tolerance for ({Quest.TileColorName}\0\0\0\0\0\0) {Quest.WrongMatchName} wrong matches";
}