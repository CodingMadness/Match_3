using System.Globalization;
using DotNext.Runtime;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;
using static Match_3.Utils;
using static rlImGui_cs.RlImGui;

#pragma warning disable CS8618

namespace Match_3;

internal static class Game
{
    public static GameState State { get; private set; }
    public static Level Level { get; private set; }
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background _bgGameOver, _bgWelcome, _bgIngame1;
    private static GameTime _gameTimer ;
    
    private static bool _enterGame;
    private static bool _shallCreateEnemies;
    
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
        Level = new(0,90, 6, 11, 9);
        _gameTimer = GameTime.GetTimer(Level.GameBeginAt);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
      
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(BgIngameTexture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets();
        InitGameOverTxt();
        InitWelcomeTxt();
        _bgWelcome = new(WelcomeTexture);
        _bgIngame1 = new(BgIngameTexture);
        _bgGameOver = new(GameOverTexture);
        QuestHandler.InitGoals();
        State = new( );
        Grid.Instance.Init(Level);
        ShaderLoc = InitShader();
        var tmp = GetScreenCoord();
    }
    
    private static void DragMouseToEnemies()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches is { } && _enemyMatches.WorldPos != InvalidCell)
        {
            bool outsideRect = !CheckCollisionPointRec(GetMousePosition(), _enemyMatches.Border);

            if (outsideRect && State.EnemiesStillPresent)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.WorldPos, 1);
            }
            else if (!State.EnemiesStillPresent && _enemyMatches.IsMatchActive)
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
        return tile is { };
    }
    
    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var firstClickedTile))
            return;

        if (firstClickedTile!.IsDeleted)
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (_enemyMatches?.IsMatchActive == true &&
            Intrinsics.IsExactTypeOf<EnemyTile>(firstClickedTile))
        {
            TileReplacerOnClickHandler.Instance.UnSubscribe();
            DestroyOnClickHandler.Instance.Subscribe();
            State.Current = firstClickedTile!; 
            State.Matches = _enemyMatches;
            //OnTileClicked();
        }
        else
        {
            if (Intrinsics.IsExactTypeOf<Tile>(firstClickedTile))
            {
                //Only when a default tile is clicked X-times, we wanna allow it to change
                //and since both event classes are active, we will unsub from the one who destroys on clicks
                DestroyOnClickHandler.Instance.UnSubscribe();
                TileReplacerOnClickHandler.Instance.Subscribe();
                State.Current = firstClickedTile;
                if (State.WasFeatureBtnPressed == true)
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
                    State.WasSwapped = true;
                    //OnTileSwapped(State);
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
        if (!State.WasSwapped)
            return;
        
        void CreateEnemiesIfNeeded()
        {
            if (_shallCreateEnemies && 
                (_enemyMatches is null || _enemyMatches.Count == 0) &&
                _matchesOf3?.IsMatchActive == true)
            {
                _enemyMatches = Bakery.AsEnemies(Grid.Instance, _matchesOf3);
                State.EnemiesStillPresent = true;
            }
        }
        
        if (Grid.Instance.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!) && !_shallCreateEnemies)
        {
            //Console.WriteLine($"HAD A MATCH! with {_matchesOf3.Count} elements in it!");
            State.Current = _secondClicked!;
            State.Matches = _matchesOf3;
            OnMatchFound();
        }
        else switch (_shallCreateEnemies)
        {
            case true:
                CreateEnemiesIfNeeded();
                break;
            case false:
                 _matchesOf3!.Clear();
                break;
        }

        _shallCreateEnemies = false;//RollADice();
        State.WasSwapped = false;
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
            State.EnemiesStillPresent = false;
            _shallCreateEnemies = false;
            _secondClicked = null;
            State.WasSwapped = false;
            Console.Clear();
        }
    }

    private static void MainGameLoop()
    {
        //float seconds = 0.0f;
        GameTime gameOverTimer = GameTime.GetTimer(Level.GameOverScreenCountdown + 10);
        int shallWobble = GetShaderLocation(WobbleEffect, "shallWobble");
      
        Setup(false);
        
        while (!WindowShouldClose())
        {
            //seconds += GetFrameTime();
            float currTime = _gameTimer.ElapsedSeconds;
            State.CurrentTime = currTime;
            
            BeginDrawing();
            ClearBackground(WHITE);
                //ImGui Context Begin
                var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground;

                Begin();
                
                    //flags = 0;
            
                    if (ImGui.Begin("Screen Overlay", flags))
                    {
                        ImGui.SetWindowPos(default);           
                        ImGui.SetWindowSize(GetScreenCoord());
                        
                        if (_enterGame)
                        {
                            Vector2 size = GetScreenCoord();
                            SetShaderValue(WobbleEffect, ShaderLoc.size, size, ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                            SetShaderValue(WobbleEffect, ShaderLoc.time, currTime, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                            SetShaderValue(WobbleEffect, shallWobble, 0, ShaderUniformDataType.SHADER_UNIFORM_INT);
                        }
                        else if (!_enterGame)
                        {
                            UIRenderer.ShowBackground(ref _bgWelcome);
                            UIRenderer.ShowWelcomeScreen();
                            UIRenderer.ShowQuestLog(false);
                        }
                        if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
                        {
                            _gameTimer.Run();

                            State.IsGameOver = _gameTimer.Done();

                            if (State.IsGameOver)
                            {
                                OnGameOver();
                                gameOverTimer.Run();
                                UIRenderer.CenterText(gameOverTimer.ElapsedSeconds.ToString(CultureInfo.InvariantCulture), null);
                                UIRenderer.ShowBackground(ref _bgGameOver);
                                UIRenderer.ShowTimer(gameOverTimer.ElapsedSeconds);
                                ImGui.SetWindowFontScale(2f);

                                if (UIRenderer.ShowGameOverScreen(gameOverTimer.Done(),
                                                                State.WasGameWonB4Timeout,
                                                                State.GameOverMessage))
                                    return;
                            }
                            else if (State.WasGameWonB4Timeout)
                            {
                                if (UIRenderer.ShowGameOverScreen(_gameTimer.Done(), true, State.GameOverMessage))
                                {
                                    //Begin new Level!
                                    InitGame();
                                    State.WasGameWonB4Timeout = false;
                                    continue;
                                }
                            }
                            else
                            {
                                var pressed = UIRenderer.ShowFeatureBtn(out var id);
                                State.WasFeatureBtnPressed ??= pressed;
                                State.WasFeatureBtnPressed = pressed ?? State.WasFeatureBtnPressed;
                                UIRenderer.ShowBackground(ref _bgIngame1);
                                UIRenderer.ShowTimer(currTime);
                                DragMouseToEnemies();
                                ProcessSelectedTiles();
                                ComputeMatches();
                                GameObjectRenderer.DrawOuterBox(_enemyMatches, currTime);
                                GameObjectRenderer.DrawInnerBox(_matchesOf3, currTime);
                                //BeginShaderMode(WobbleEffect);
                                GameObjectRenderer.DrawGrid(currTime);
                                GameObjectRenderer.DrawMatches(_enemyMatches, currTime, _shallCreateEnemies);
                                //EndShaderMode();
                                HardReset();
                            }

                            _enterGame = true;
                        }
                    }
                    
                    ImGui.End();

                    End();
                
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