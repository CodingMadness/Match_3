namespace Match_3.GameTypes;

public record Level(int ID, int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight, int TileSize,
    byte[] MapLayout)
{
    public int WindowHeight => GridWidth * TileSize;
    public int WindowWidth => GridHeight * TileSize;

    private GameTime _gameTime = GameTime.GetTimer(GameBeginAt);
    public ref GameTime GameTimer => ref _gameTime;
}