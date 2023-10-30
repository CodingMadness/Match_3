using System.Diagnostics;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Match_3.Service;
using Match_3.StateHolder;
using Match_3.Workflow;
using Raylib_cs;
using static Match_3.Setup.AssetManager;
using static Match_3.Service.Utils;

namespace Match_3.Setup;

internal static class Game
{
    public static Level Level;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background? _bgGameOver;
    private static Background _bgWelcome = null!;
    private static Background? _bgInGame1 = null!;
    private static GameTime _gameTimer;
    private static readonly StringBuilder TimeBuilder = new(3);

    private static bool _inGame;
 
    private static GameTime _gameOverTimer;
    
    public static event Action? OnTileClicked;

    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }
    
    private static void InitGame()
    {
        Level = new(0, 300, 6, 12, 12);
        GameState.CurrentLvl = Level;
        _gameTimer = GameTime.GetTimer(Level.GameBeginAt);
        _gameOverTimer = GameTime.GetTimer(Level.GameOverScreenCountdown + 10);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets(new(Level.GridWidth, Level.GridHeight));
        ShaderData = InitWobble2(GetScreenCoord());
        _bgWelcome = new(WelcomeTexture);
        _bgGameOver = new(GameOverTexture);

        //this has to be initialized RIGHT HERE in order to work!
        QuestHandler.ActivateHandlers();
        QuestBuilder.Init();
        Grid.Init();
        GameState.CurrData = new();
        // GameState.Quests = QuestBuilder.GetQuests();
    }

    private static void NotifyClickHandler()
    {
        if (TileClicked(out var firstClickedTile))
        {
            var currState = GameState.CurrData!;
            currState.TileX = firstClickedTile;
            currState.TileY = _secondClicked; //this may be NULL! check to see when and how..
            OnTileClicked?.Invoke();
        }
    }
    
    private static void DragMouseToEnemies()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches is not null && _enemyMatches.WorldPos != InvalidCell)
        {
            bool outsideRect = !CheckCollisionPointRec(GetMousePosition(), _enemyMatches.Border.AsIntRayRect());

            if (outsideRect && GameState.EnemiesStillPresent)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.WorldPos, 1);
            }
            else if (!GameState.EnemiesStillPresent && _enemyMatches.IsMatchActive)
            {
                //we set this to null, because we cant make any swaps after this, cause 
                //_secondClicked has a value! so we then can repeat the entire cycle!
                _secondClicked = null;
                //enemies were created from the matchesOf3, so we have to
                //delete all of them, because else we will reference always the base-matches internally which is bad!
                _enemyMatches.Clear();
            }
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
        //TODO: change this (maybe?) to EventDriven code! via "OnGetTile?.Invoke();"
        tile = Grid.GetTile(gridPos);
        return tile is not null;
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            //Grid.Instance = new Grid(Level);
            _matchesOf3?.Clear();
            _enemyMatches?.Clear();
            GameState.EnemiesStillPresent = false;
            _secondClicked = null;
            GameState.CurrData!.WasSwapped = false;
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
            if (GameState.IsGameOver)
            {
                // OnGameOver();
                _gameOverTimer.Run();
                TimeBuilder.Append($"{_gameOverTimer.ElapsedSeconds}");

                UiRenderer.DrawText(TimeBuilder.AsSpan());
                UiRenderer.DrawBackground(_bgGameOver);
                UiRenderer.DrawTimer(_gameOverTimer.ElapsedSeconds);
                ImGui.SetWindowFontScale(2f);
                
                return 
                    !UiRenderer.DrawGameOverScreen(_gameOverTimer.Done(),
                                                  GameState.WasGameWonB4Timeout,
                                                 GameState.Logger!.Dequeue());
            }
            else if (GameState.WasGameWonB4Timeout)
            {
                if (UiRenderer.DrawGameOverScreen(_gameTimer.Done(), true, GameState.Logger!.Dequeue()))
                {
                    //Begin new Level and reset values!
                    InitGame();
                    GameState.Logger.Clear();
                    GameState.WasGameWonB4Timeout = false;
                    GameState.IsGameOver = false;
                    GameState.EnemiesStillPresent = false;
                    GameState.CurrData = null;
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
        Debug.WriteLine(currTime);
        
        switch (_inGame)
        {
            case false:
                UiRenderer.DrawQuestLog(); 
                break;
            case true:
            {
                _gameTimer.Run();

                UpdateShader(ShaderData.gridSizeLoc, GetScreenCoord());
                UpdateShader(ShaderData.secondsLoc, currTime);
                UpdateShader(ShaderData.shouldWobbleLoc, true);
                GameState.IsGameOver = _gameTimer.Done();

                if (IsGameStillRunning())
                {
                    UiRenderer.DrawBackground(_bgInGame1);
                    UiRenderer.DrawTimer(currTime);
                    DragMouseToEnemies();
                    NotifyClickHandler();
                    GameObjectRenderer.DrawGrid(currTime);
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