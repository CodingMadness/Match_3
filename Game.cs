using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.AssetManager;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;

#pragma warning disable CS8618

namespace Match_3;

internal static class Game
{
    public static GameState State { get; private set; }
    public static Level Level { get; private set; }
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background bgGameOver, bgWelcome, bgIngame1, bgIngame2;
    private static GameTime _gameTimer ;
    
    private static bool _enterGame;
    private static bool _shallCreateEnemies;

    private static (int size, int time) shaderLoc;
    
    public static event Action OnMatchFound;
    public static event Action OnTileClicked;
    public static event Action OnTileSwapped;
    
    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        Level = new(0,60*3, 6, 11, 9);
        _gameTimer = GameTime.GetTimer(Level.GameBeginAt);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        SetTextureFilter(IngameTexture1, TextureFilter.TEXTURE_FILTER_BILINEAR);
        LoadAssets();
        InitGameOverTxt();
        InitWelcomeTxt();
        bgWelcome = new(WelcomeTexture);
        bgIngame1 = new(IngameTexture1);
        bgGameOver = new(GameOverTexture);
        QuestHandler.InitGoal();
        State = new( );
        Grid.Instance.Init(Level);
        shaderLoc = InitShader();
        //System.Console.WriteLine("ARE WE STILL IN THE SAME PROJECT????!!!");
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
            EnemyTile.IsOnlyEnemyTile(firstClickedTile, out var enemy))
        {
            TileReplacerOnClickHandler.Instance.UnSubscribe();
            DestroyOnClickHandler.Instance.Subscribe();
            State.Current = enemy!; 
            State.Matches = _enemyMatches;
            OnTileClicked();
        }
        else
        {
            if (Tile.IsOnlyDefaultTile(firstClickedTile) && 
                !EnemyTile.IsOnlyEnemyTile(firstClickedTile, out _))
            {
                //Console.WriteLine("Normal tile was clicked !!");
                //Only when a default tile is clicked, we wanna allow it to change
                //and since both event classes are active, we will unsub the one who destroys on clicks
                DestroyOnClickHandler.Instance.UnSubscribe();
                TileReplacerOnClickHandler.Instance.Subscribe();
                State.Current = firstClickedTile;
                OnTileClicked();
            }
            firstClickedTile.TileState |= TileState.Selected;

            /*No tile selected yet*/
            if (_secondClicked is null)
            {
                //prepare for next round, so we store first in second!
                _secondClicked = firstClickedTile;
                //_secondClicked.Select();
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
    
    static void ComputeMatches()
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
            Grid.Instance.Delete(_matchesOf3!);
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
        GameTime gameOverTimer = GameTime.GetTimer(Level.GameOverScreenCountdown);

        while (!WindowShouldClose())
        {
            //seconds += GetFrameTime();
            float elapsedTime = _gameTimer.ElapsedSeconds;

            BeginDrawing();

                if (_enemyMatches is not null)
                {
                    var size = GetScreenCoord();
                    SetShaderValue(WobbleShader, shaderLoc.size, size , ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                    SetShaderValue(WobbleShader, shaderLoc.time, elapsedTime, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                }

                ClearBackground(WHITE);
                
                if (!_enterGame)
                {
                    Renderer.DrawBackground(ref bgWelcome);
                    Renderer.ShowWelcomeScreen();
                    //Renderer.LogQuest(false, Level);
                }
                if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
                {
                    _gameTimer.Run();
                    
                    bool isGameOver = _gameTimer.Done();

                    if (isGameOver)
                    {
                        gameOverTimer.Run();
                        Renderer.DrawBackground(ref bgGameOver);
                        Renderer.DrawTimer(gameOverTimer.ElapsedSeconds);
                        //Renderer.ShowWelcomeScreen();
                        
                        if (Renderer.DrawGameOverResult(gameOverTimer.Done(), State.WasGameWonB4Timeout))
                            return;
                    }
                    else if (State.WasGameWonB4Timeout)
                    {
                        if (Renderer.DrawGameOverResult(_gameTimer.Done(), true))
                        {
                            InitGame();
                            State.WasGameWonB4Timeout = false;
                            continue;
                        }
                    }
                    else
                    {
                        Renderer.DrawBackground(ref bgIngame1);
                        Renderer.DrawTimer(elapsedTime);
                        DragMouseToEnemies();
                        ProcessSelectedTiles();
                        ComputeMatches();
                        //Console.WriteLine(_matchesOf3.Count);
                        Renderer.DrawOuterBox(_enemyMatches, elapsedTime);  
                        Renderer.DrawInnerBox(_matchesOf3, elapsedTime) ;  
                        Renderer.DrawGrid(elapsedTime, shaderLoc);
                        BeginShaderMode(WobbleShader);
                        Renderer.DrawMatches(_enemyMatches, elapsedTime, _shallCreateEnemies);
                        EndShaderMode();
                        HardReset();
                    }
                
                    _enterGame = true;
                }
            
            EndDrawing();
        }
    }

    private static void CleanUp()
    {
        UnloadShader(WobbleShader);
        UnloadTexture(DefaultTileSprite);
        CloseWindow();
    }
}