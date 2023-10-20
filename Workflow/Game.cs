using System.Numerics;
using System.Text;
using DotNext.Runtime;
using ImGuiNET;
using Match_3.Datatypes;
using Match_3.Service;
using Raylib_cs;
using static Match_3.Setup.AssetManager;
using static Match_3.Service.Utils;

namespace Match_3.Workflow;

using System;

internal static class Game
{
    public static Level Level { get; private set; } = null!;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background? _bgGameOver;
    private static Background _bgWelcome = null!;
    private static Background? _bgInGame1 = null!;
    private static GameTime _gameTimer;
    private static GameTime[]? _questTimers;
    private static TimeOnly _whenTileClicked, _whenEventRaised;
    private static readonly StringBuilder TimeBuilder = new(3);

    private static bool _inGame;
    private static bool _shallCreateEnemies;
    private static bool _runQuestTimers;
    private static GameTime _gameOverTimer;

    public static event Action OnMatchFound;
    // public static event Action OnTileClicked;
    public static event Action OnTileSwapped;
    public static event Action OnGameOver;

    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        Level = new(0, 600, 6, 12, 12);
        _runQuestTimers = false;
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
        _questTimers = QuestBuilder.QuestTimers;
        QuestHandler.ActivateHandlers();
        Grid.Instance.Init(Level);
        GameState.CurrentData = new();
        // GameState.Quests = QuestBuilder.GetQuests();
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
        tile = Grid.Instance[gridPos];
        return tile is not null;
    }

    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var firstClickedTile))
            return;

        _whenTileClicked = TimeOnly.FromDateTime(DateTime.UtcNow);

        if (firstClickedTile!.IsDeleted)
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (_enemyMatches?.IsMatchActive == true && Intrinsics.IsExactTypeOf<EnemyTile>(firstClickedTile))
        {
            /*DestroyOnClickHandler.Instance.Subscribe();*/
            /*TileReplacementOnClickHandler.Instance.UnSubscribe();*/

            //we store our current values inside "GameState" which due to its static nature is then checked upon 
            //internally inside the QuestHandler's
            _whenEventRaised = TimeOnly.FromDateTime(DateTime.UtcNow);
            float elapsedTime = (_whenTileClicked - _whenEventRaised).Seconds;
            GameState.CurrentData!.TileX = firstClickedTile;
            GameState.CurrentData.Matches = _enemyMatches;

            //OnTileClicked();
        }
        else
        {
            if (Intrinsics.IsExactTypeOf<Tile>(firstClickedTile))
            {
                //Only when a default tile is clicked X-times, we wanna allow it to change
                //and since both event classes are active,
                //we will need still to unsub from the one who destroys on clicks
                //because he still active and listens to click-events, which we dont want

                /*DestroyOnClickHandler.Instance.UnSubscribe();*/
                /*TileReplacementOnClickHandler.Instance.Subscribe();*/

                GameState.CurrentData!.TileX = firstClickedTile;

                // if (GameState.WasFeatureBtnPressed == true)
                //     OnTileClicked();
            }

            firstClickedTile.TileState |= TileState.Selected;

            /*No tile selected yet*/
            if (_secondClicked is null)
            {
                //prepare for next round, so we store first in second!
                _secondClicked = firstClickedTile;
                return;
            }

            /*Same tile selected => deselect*/
            if (StateAndBodyComparer.Singleton.Equals(firstClickedTile, _secondClicked))
            {
                Console.Clear();
                //Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
                _secondClicked.TileState &= TileState.Selected;
                _secondClicked = null;
            }
            /*Different tile selected ==> swap*/
            else
            {
                firstClickedTile.TileState &= TileState.Selected;

                if (Grid.Instance.Swap(firstClickedTile, _secondClicked))
                {
                    //the moment we have the 1. swap, we notify the MatchQuestHandler for this
                    //and he begins to count-down
                    _whenEventRaised = TimeOnly.FromDateTime(DateTime.UtcNow);
                    var eventData = GameState.CurrentData!;
                    eventData.WasSwapped = true;
                    eventData.Count++;
                    eventData.Interval = (_whenTileClicked - _whenEventRaised).Seconds;
                    eventData.TileX = firstClickedTile;
                    eventData.TileY = _secondClicked;
                    OnTileSwapped();

                    _secondClicked.TileState &= TileState.Selected;
                }
                else
                {
                    _secondClicked.TileState &= TileState.Selected;
                }
            }
        }
    }

    private static void ComputeMatches()
    {
        if (!GameState.CurrentData!.WasSwapped)
            return;

        void CreateEnemiesIfNeeded()
        {
            if (_shallCreateEnemies && (_enemyMatches is null || _enemyMatches.Count == 0))
            {
                _enemyMatches = Bakery.AsEnemies(Grid.Instance, _matchesOf3!);
                GameState.EnemiesStillPresent = true;
            }
        }

        bool shallCreateEnemies;

        if (shallCreateEnemies = Grid.Instance.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!))
        {
            var eventData = GameState.CurrentData;
            eventData.WasSwapped = true;
            eventData.Count++;
            eventData.TileX = _secondClicked!;
            eventData.Matches = _matchesOf3;
            int tileTypeIdx = (int)eventData.TileX!.Body.TileColor;

            //TODO: Look into this timer problem, cause we dont wanna have TileType.Length timers only those who are in request!
            //OnMatchFound();

            //if its the 1.time we set _runQuestTimers=true, but we also check for each new Matched
            _runQuestTimers = true;
        }

        // shallCreateEnemies &= EnemyMatchRuleHandler.Instance.Check();

        switch (shallCreateEnemies)
        {
            case true:
                CreateEnemiesIfNeeded();
                break;
            case false:
                Grid.Instance.Delete(_matchesOf3!);
                break;
        }

        GameState.CurrentData.WasSwapped = false;
        _secondClicked = null;
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            //Grid.Instance = new Grid(Level);
            _shallCreateEnemies = false;
            _matchesOf3?.Clear();
            _enemyMatches?.Clear();
            GameState.EnemiesStillPresent = false;
            _shallCreateEnemies = false;
            _secondClicked = null;
            GameState.CurrentData!.WasSwapped = false;
            Console.Clear();
        }
    }
    
    /// <summary>
    /// this checks for a lot of scenarios in which the game could end, either by failure OR win in time!
    /// </summary>
    /// <returns></returns>
    
    //------------------------------------------------------------

    private static void HandleGameInput()
    {
        static bool IsGameStillRunning()
        {
            if (GameState.IsGameOver)
            {
                OnGameOver();
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
                    GameState.CurrentData = null;
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
                    ProcessSelectedTiles();
                    ComputeMatches();
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
            UiRenderer.BeginRenderCycle(HandleGameInput);
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleEffect);
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}