using System.Globalization;
using DotNext.Runtime;
using Match_3.GameTypes;
using Raylib_cs;
using static Match_3.AssetManager;
using static Match_3.Utils;

namespace Match_3;

internal static class Game
{
    public static Level Level { get; private set; }
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background? _bgGameOver;
    private static Background _bgWelcome;
    private static Background? _bgIngame1;
    private static GameTime _gameTimer;
    private static GameTime[]? _questTimers;
    private static EnemyMatchRuleHandler _enemyMatchRuleHandler;

    private static bool _enterGame;
    private static bool _shallCreateEnemies;
    private static bool _runQuestTimers;
    
    public static event Action OnMatchFound;
    public static event Action OnTileClicked;
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
        Level = new(0, 900, 6, 12, 10);
        _runQuestTimers = false;
        _gameTimer = GameTime.GetTimer(Level.GameBeginAt);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets();
   
        _bgWelcome = new(WelcomeTexture);
        _bgGameOver = new(GameOverTexture);
        
        QuestHandler.ActivateHandlers();
        Grid.Instance.Init(Level);
        //this has to be initialized RIGHT HERE in order to work!
        _questTimers = QuestHandler.QuestTimers;
        ShaderData = InitShader();
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
        gridPos /= Tile.Size;
        tile = Grid.Instance[gridPos];
        return tile is not null;
    }

    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var firstClickedTile))
            return;

        if (firstClickedTile!.IsDeleted)
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (_enemyMatches?.IsMatchActive == true && Intrinsics.IsExactTypeOf<EnemyTile>(firstClickedTile))
        {
            /*DestroyOnClickHandler.Instance.Subscribe();*/
            /*TileReplacementOnClickHandler.Instance.UnSubscribe();*/
            
            //we store our current values inside "GameState" which due to its static nature is then checked upon 
            //internally inside the QuestHandler's
            GameState.Tile = firstClickedTile;
            GameState.Matches = _enemyMatches;
            OnTileClicked();
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

                GameState.Tile = firstClickedTile;

                if (GameState.WasFeatureBtnPressed == true)
                    OnTileClicked();
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
                    GameState.WasSwapped = true;
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
        if (!GameState.WasSwapped)
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
            int tileTypeIdx = (int)GameState.Tile.Body.TileType;
            GameState.Tile = _secondClicked!;
            GameState.Matches = _matchesOf3;
            GameState.CurrentTime = _questTimers[tileTypeIdx].ElapsedSeconds;
            OnMatchFound();
            
            //if its the 1.time we set _runQuestTimers=true, but we also check for each new Matched
            _runQuestTimers = true;
        }

        shallCreateEnemies &= EnemyMatchRuleHandler.Instance.Check();

        switch (shallCreateEnemies)
        {
            case true:
                CreateEnemiesIfNeeded();
                break;
            case false:
                Grid.Instance.Delete(_matchesOf3!);
                break;
        }

        GameState.WasSwapped = false;
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
            GameState.WasSwapped = false;
            Console.Clear();
        }
    }

    private static void MainGameLoop()
    {
        //float seconds = 0.0f;
        GameTime gameOverTimer = GameTime.GetTimer(Level.GameOverScreenCountdown + 10);
        int shallWobble = GetShaderLocation(WobbleEffect, "shallWobble");

        RlImGui.Setup(false);

        while (!WindowShouldClose())
        {
            //seconds += GetFrameTime();
            float currTime = _gameTimer.ElapsedSeconds;
            GameState.CurrentTime = currTime;

            BeginDrawing();
            {
                ClearBackground(WHITE);
                //ImGui Context Begin
                const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration |
                                               ImGuiWindowFlags.NoScrollbar |
                                               ImGuiWindowFlags.NoBackground |
                                               ImGuiWindowFlags.NoMove;

                RlImGui.Begin();
                {
                    if (ImGui.Begin("Screen Overlay", flags))
                    {
                        // ImGui.ShowDemoWindow();

                        ImGui.SetWindowPos(default);
                        ImGui.SetWindowSize(GetScreenCoord());

                        if (_enterGame)
                        {
                            Vector2 size = GetScreenCoord();
                            SetShaderValue(WobbleEffect, ShaderData.size, size,
                                ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                            SetShaderValue(WobbleEffect, ShaderData.time, currTime,
                                ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                            // SetShaderValue(WobbleEffect, shallWobble, 0.1, ShaderUniformDataType.SHADER_UNIFORM_INT);
                        }
                        else if (!_enterGame)
                        {
                            // UiRenderer.DrawBackground(_bgWelcome);
                            UiRenderer.DrawQuestLog();
                        }

                        if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
                        {
                            _gameTimer.Run();

                            int tileTypeIdx = (int)GameState.Tile.Body.TileType;
                            ref var questTimer = ref _questTimers[tileTypeIdx];

                            //if it got activated in "ComputeMatches()" then we can count down, else it sleeps!
                            if (_runQuestTimers)
                                questTimer.Run();

                            
                            if (_runQuestTimers)
                                if (questTimer.Done())
                                { 
                                }
                                // Console.WriteLine(questTimer.Done()
                                //     ? $"YOU MISSED 1 MATCH OF TYPE {GameState.Tile.Body.TileType} AND NOW YOU LOSE BONUS OR GET PUNISHED!"
                                //     : $"YOU STILL HAVE {questTimer.ElapsedSeconds} TIME LEFT!");

                            GameState.IsGameOver = _gameTimer.Done();

                            if (GameState.IsGameOver)
                            {
                                OnGameOver();
                                gameOverTimer.Run();
                                var value = gameOverTimer.ElapsedSeconds.ToString(CultureInfo.InvariantCulture);
                                UiRenderer.DrawText(value);
                                UiRenderer.DrawBackground(_bgGameOver);
                                UiRenderer.DrawTimer(gameOverTimer.ElapsedSeconds);
                                ImGui.SetWindowFontScale(2f);

                                if (UiRenderer.DrawGameOverScreen(gameOverTimer.Done(),
                                        GameState.WasGameWonB4Timeout,
                                        GameState.GameOverMessage))
                                    return;
                            }
                            else if (GameState.WasGameWonB4Timeout)
                            {
                                if (UiRenderer.DrawGameOverScreen(_gameTimer.Done(), true, GameState.GameOverMessage))
                                {
                                    //Begin new Level!
                                    InitGame();
                                    GameState.WasGameWonB4Timeout = false;
                                    continue;
                                }
                            }
                            else
                            {
                                // var pressed = UIRenderer.DrawFeatureBtn(out _);
                                // GameState.WasFeatureBtnPressed ??= pressed;
                                // GameState.WasFeatureBtnPressed = pressed ?? GameState.WasFeatureBtnPressed;
                                UiRenderer.DrawBackground(_bgIngame1);
                                UiRenderer.DrawTimer(currTime);
                                DragMouseToEnemies();
                                ProcessSelectedTiles();
                                ComputeMatches();
                                GameObjectRenderer.DrawOuterBox(_enemyMatches, currTime);
                                GameObjectRenderer.DrawInnerBox(_matchesOf3, currTime);
                                // BeginShaderMode(WobbleEffect);
                                GameObjectRenderer.DrawGrid(currTime);
                                GameObjectRenderer.DrawMatches(_enemyMatches, currTime, _shallCreateEnemies);
                                // EndShaderMode();
                                HardReset();
                            }

                            _enterGame = true;
                        }
                    }

                    ImGui.End();
                }
                RlImGui.End();
            }
            EndDrawing();
        }
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleEffect);
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}