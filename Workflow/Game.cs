using Match_3.DataObjects;
using Match_3.Setup;
using Raylib_cs;

using static Match_3.Setup.AssetManager;

namespace Match_3.Workflow;

//TODO: 1. Make all the "TileRelatedTypes" structs because they represent nothing but value holder with minimal state change
//TODO: 2. Fix the entire "QuestHandler" related Event logic, like what shall happen when certain tiles or matches are done, etc...
//TODO: 3. Write the algorithm for "TileGraph" which shall exchange 1 Graph with another so that there are not any distant tiles anymore
internal static class Game
{
    private static GameTime _gameTimer, _gameOverTimer;
    private static bool _inGame;
    public static event Action OnTileClicked = null!;

    private static void Main()
    {
        Initialize();
        MainGameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        Config level = new(0, 300, 6, 15, 15);
        //_timeBuilder = new(3);
        _gameTimer = GameTime.CreateTimer(level.GameBeginAt);
        _gameOverTimer = GameTime.CreateTimer(level.GameOverScreenCountdown + 10);
        SetTargetFPS(60);
        //SetConfigFlags(ConfigFlags.ResizableWindow);
        InitWindow(level.WindowWidth, level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.Bilinear);
        LoadAssets(new(level.GridWidth, level.GridHeight));

        //this has to be initialized RIGHT HERE in order to work!
        GameState.Lvl = level;
        QuestHandler.ActivateHandlers();
        Grid.Init();
        QuestBuilder.DefineQuests();
    }

    private static void MainGameLoop()
    {
        /// <summary>
        /// this checks for a lot of scenarios in which the game could end, either by failure OR
        /// by actually winning in time!
        /// </summary>
        /// <returns></returns>
        //------------------------------------------------------------
        static void HandleGameInput()
        {
            static void NotifyClickHandler()
            {
                static bool TileClicked(out Tile? tile)
                {
                    tile = default!;

                    if (!IsMouseButtonPressed(MouseButton.Left))
                        return false;

                    SingleCell tileCell = GetMousePosition();
                    tile = Grid.GetTile(tileCell.Start);
                    // Console.WriteLine(tile);
                    return tile is not null;
                }

                if (TileClicked(out var firstClickedTile))
                {
                    var currState = GameState.CurrData;
                    currState.TileX = firstClickedTile;
                    OnTileClicked();
                    Console.WriteLine(firstClickedTile);
                }
            }

            float currTime = _gameTimer.CurrentSeconds;
            _inGame |= IsKeyDown(KeyboardKey.Enter);
            // Console.WriteLine(currTime);

            if (!_inGame)
            {
                //UiRenderer.DrawQuestLog(GameState.GetQuests());
            }
            else if (_inGame)
            {
                var eventData = GameState.CurrData;
                eventData.WasGameLost = _gameTimer.CountDown();

                if (eventData.WasGameLost)
                {
                    //print to the main-window that the user has lost
                }
                else if (eventData.WasGameWon)
                {
                    //print to the main-window that the user has won
                }
                //game still running..!
                else
                {
                    const float debug_fontScale = 90;
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.TopLeft, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.MidLeft, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.BottomLeft, debug_fontScale);
                    UiRenderer.DrawText($"(Green) {(int)currTime}", CanvasStartingPoints.TopCenter, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.Center, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.Bottomcenter, debug_fontScale);
                    UiRenderer.DrawText($"(Red) {(int)currTime}", CanvasStartingPoints.TopRight, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.MidRight, debug_fontScale);
                    UiRenderer.DrawText($"(Blue) {(int)currTime}", CanvasStartingPoints.BottomRight, debug_fontScale);
                    NotifyClickHandler();
                    TileRenderer.DrawGrid(currTime, GameState.Lvl.GridWidth, GameState.Lvl.GridHeight);
                }
            }
        }

        while (!WindowShouldClose())
            UiRenderer.BeginRendering(HandleGameInput);
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleEffect);
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}