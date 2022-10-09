namespace Match_3.GameTypes;

public record Level(int ID, 
    int GameBeginAt, int GameOverScreenCountdown,
    int GridWidth, int GridHeight)
{
    public const int MAX_TILES_PER_MATCH = 3;
    public const int TILE_SIZE = 64 / 2;
    public int WindowHeight => GridWidth * TILE_SIZE;
    public int WindowWidth => GridHeight * TILE_SIZE;

    private GameTime _gameTime = GameTime.GetTimer(GameBeginAt);
    public ref GameTime GameTimer => ref _gameTime;
}