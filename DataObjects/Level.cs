namespace Match_3.DataObjects;

public record Level(int Id, 
    int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight)
{
    public const int MaxTilesPerMatch = 3;
    public const int TileSize = 64 / 1;
    public int WindowHeight => GridHeight * TileSize;
    public int WindowWidth => GridWidth * TileSize;
}
