namespace Match_3.GameTypes;

public record Level(int ID, 
    int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight)
{
    public const int MAX_TILES_PER_MATCH = 3;
    public const int TILE_SIZE = 64 / 1;
    public int WindowHeight => GridHeight * TILE_SIZE;
    public int WindowWidth => GridWidth * TILE_SIZE;
}
