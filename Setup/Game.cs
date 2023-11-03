using System.Numerics;
using System.Text;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Workflow;
using Raylib_cs;

using static Match_3.Setup.AssetManager;
using static Match_3.Service.Utils;

namespace Match_3.Setup;

internal static class Game
{
    private static GameTime _gameTimer;
    private static readonly StringBuilder TimeBuilder = new(3);
    private static bool _inGame;
    private static GameTime _gameOverTimer;
    
    public static event Action OnTileClicked;

    private static void Main()
    {
        Initialize();
        MainGameLoop();
        CleanUp();
    }
    
    private static void Initialize()
    {
        GameState.Lvl = new(0, 700, 6, 12, 12);
        var level = GameState.Lvl;
        level.QuestCount = 1;
        _gameTimer = GameTime.GetTimer(level.GameBeginAt);
        _gameOverTimer = GameTime.GetTimer(level.GameOverScreenCountdown + 10);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(level.WindowWidth, level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets(new(level.GridWidth, level.GridHeight));
        ShaderData = InitWobble2(GetScreenCoord());
        
        //this has to be initialized RIGHT HERE in order to work!
        QuestHandler.ActivateHandlers();
        Grid.Init();
        QuestBuilder.DefineQuests();
    }

    private static void NotifyClickHandler()
    {
        if (TileClicked(out var firstClickedTile))
        {
            var currState = GameState.CurrData!;
            currState.TileX = firstClickedTile;
            OnTileClicked();
        }
    }

    private static bool TileClicked(out Tile? tile)
    {
        tile = default!;

        if (!IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            return false;

        var mouseVec2 = GetMousePosition();
        Vector2 gridPos = new Vector2((int)mouseVec2.X, (int)mouseVec2.Y);
        gridPos /= Size;
        tile = Grid.GetTile(gridPos);
        return tile is not null;
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            GameState.CurrData!.EnemiesStillPresent = false;
            GameState.CurrData.WasSwapped = false;
            Console.Clear();
        }
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
            var eventData = GameState.CurrData!;
            
            if (eventData.IsGameOver)
            {
                // OnGameOver();
                _gameOverTimer.CountDown();
                TimeBuilder.Append($"{_gameOverTimer.ElapsedSeconds}");

                UiRenderer.DrawText(TimeBuilder.AsSpan());
                UiRenderer.DrawTimer(_gameOverTimer.ElapsedSeconds);
                ImGui.SetWindowFontScale(2f);
                
                return 
                    !UiRenderer.DrawGameOverScreen(_gameOverTimer.Done(),
                        eventData.WasGameWonB4Timeout,
                                                 GameState.Logger!.Dequeue(true));
            }
            else if (eventData.WasGameWonB4Timeout)
            {
                if (UiRenderer.DrawGameOverScreen(_gameTimer.Done(), true, GameState.Logger!.Dequeue()))
                {
                    //Begin new Level and reset values!
                    Initialize();
                    GameState.Logger.Clear();
                    eventData.WasGameWonB4Timeout = false;
                    eventData.IsGameOver = false;
                    eventData.EnemiesStillPresent = false;
                    eventData = null;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        
        float currTime = _gameTimer.ElapsedSeconds;
        _inGame |= IsKeyDown(KeyboardKey.KEY_ENTER);
        
        switch (_inGame)
        {
            case false:
                UiRenderer.DrawQuestLog(); 
                break;
            case true:
            {
                _gameTimer.CountDown();

                UpdateShader(ShaderData.gridSizeLoc, GetScreenCoord());
                UpdateShader(ShaderData.secondsLoc, currTime);
                UpdateShader(ShaderData.shouldWobbleLoc, true);
                GameState.CurrData!.IsGameOver = _gameTimer.Done();

                if (IsGameStillRunning())
                {
                    UiRenderer.DrawTimer(currTime);
                    NotifyClickHandler();
                    TileRenderer.DrawGrid(currTime);
                    HardReset();
                }
                break;
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