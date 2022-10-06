namespace Match_3.GameTypes;

public readonly record struct Level(int ID, int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight, int TileSize,
    byte[] MapLayout)
{
    public int WindowHeight => GridWidth * TileSize;
    public int WindowWidth => GridHeight * TileSize;

    public GameTime GameTimer { get; } = GameTime.GetTimer(GameBeginAt);
}