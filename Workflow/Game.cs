using System.Text;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
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
    private static StringBuilder _timeBuilder = null!;
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
        // GameState.Lvl = new(0, 700, 6, 12, 12);
        _timeBuilder = new(3);
        var level = GameState.Lvl;
        level.QuestCount = 1;
        _gameTimer = GameTime.GetTimer(level.GameBeginAt);
        _gameOverTimer = GameTime.GetTimer(level.GameOverScreenCountdown + 10);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(level.WindowWidth, level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets(new(level.GridWidth, level.GridHeight));
        ShaderData = InitWobble2(Utils.GetScreen());
        
        //this has to be initialized RIGHT HERE in order to work!
        QuestHandler.ActivateHandlers();
        Grid.Init();
        QuestBuilder.DefineQuests();
    }

    private static void NotifyClickHandler()
    {
        if (TileClicked(out var firstClickedTile))
        {
            var currState = GameState.CurrData;
            currState.TileX = firstClickedTile;
            OnTileClicked();
            Console.WriteLine(firstClickedTile);
        }
    }

    private static bool TileClicked(out Tile? tile)
    {
        tile = default!;

        if (!IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            return false;

        SingleCell tileCell = GetMousePosition();
        tile = Grid.GetTile(tileCell.Start);
        // Console.WriteLine(tile);
        return tile is not null;
    }
    
    /// <summary>
    /// this checks for a lot of scenarios in which the game could end, either by failure OR
    /// by actually winning in time!
    /// </summary>
    /// <returns></returns>
    //------------------------------------------------------------
    private static void HandleGameInput()
    {
        static bool IsGameStillRunning()
        {
            var eventData = GameState.CurrData;
            
            if (eventData.IsGameOver)
            {
                // OnGameOver();
                _gameOverTimer.CountDown();
                _timeBuilder.Append($"{_gameOverTimer.CurrentSeconds}");

                UiRenderer.DrawText(_timeBuilder.AsSpan());
                UiRenderer.DrawTimer(_gameOverTimer.CurrentSeconds);
                ImGui.SetWindowFontScale(2f);
                
                return 
                    !UiRenderer.DrawGameOverScreen(_gameOverTimer.Done(),
                        eventData.WasGameWonB4Timeout,
                                                 GameState.Logger.Dequeue(true));
            }
            else if (eventData.WasGameWonB4Timeout)
            {
                if (UiRenderer.DrawGameOverScreen(_gameTimer.Done(), true, GameState.Logger.Dequeue()))
                {
                    //Start new Level and reset values!
                    Initialize();
                    GameState.Logger.Clear();
                    eventData.WasGameWonB4Timeout = false;
                    eventData.IsGameOver = false;
                    eventData = null;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        
        float currTime = _gameTimer.CurrentSeconds;
        _inGame |= IsKeyDown(KeyboardKey.KEY_ENTER);
        // Console.WriteLine(currTime);
        
        if (!_inGame)
        {
            // UiRenderer.DrawQuestLog(GameState.GetQuests());
        }
        else if (_inGame)
        {
            _gameTimer.CountDown();
             GameState.CurrData.IsGameOver = _gameTimer.Done();

            if (IsGameStillRunning())
            {
                UiRenderer.DrawTimer(currTime);
                NotifyClickHandler();
                TileRenderer.DrawGrid(currTime, GameState.Lvl.GridWidth, GameState.Lvl.GridHeight);
            }
        }
    }

    private static void MainGameLoop()
    {
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