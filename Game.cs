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
    private static Grid _grid;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    private static Background bg1, bgIngame1, bgIngame2;

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
        Level = new(0,45*5, 4, 10, 8);
        State = new((int)Type.Length);
        _matchesOf3 = new();
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        LoadAssets();
        bg1 = new(WelcomeTexture);
        bgIngame1 = new(IngameTexture1);
        bgIngame2 = new(IngameTexture2);
        SetTextureFilter(IngameTexture1, TextureFilter.TEXTURE_FILTER_BILINEAR);
        QuestHandler.InitGoal();
        _grid = new(Level);
        State.Grid = _grid;
        shaderLoc = InitShader();
        System.Console.WriteLine("HEY INIIIT!!!");
    }
    
    private static void DragMouseToEnemies()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches is { } && _enemyMatches.WorldPos != INVALID_CELL)
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
        tile = _grid[gridPos];
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
            var count = Game.OnTileClicked.GetInvocationList().Length;
            Console.WriteLine("Enemy tile was clicked !!");
            var elapsedSeconds = (TimeOnly.FromDateTime(DateTime.UtcNow) - _enemyMatches.CreatedAt).Seconds;
            State.Enemy = enemy ?? (firstClickedTile as EnemyTile)!;
            State.DefaultTile = (enemy as Tile)!;
            State.Matches = _enemyMatches;
            ref var current = ref State.GetData();
            current.Click.Count++;
            current.Click.Seconds = elapsedSeconds;
            OnTileClicked();
        }
        else
        {
            if (Tile.IsOnlyDefaultTile(firstClickedTile) && 
                !EnemyTile.IsOnlyEnemyTile(firstClickedTile, out _))
            {
                Console.WriteLine("Normal tile was clicked !!");
                //Only when a default tile is clicked, we wanna allow it to change
                //and since both event classes are active, we will unsub the one who destroys on clicks
                DestroyOnClickHandler.Instance.UnSubscribe();
                TileReplacerOnClickHandler.Instance.Subscribe();
                State.DefaultTile = firstClickedTile;
                ref var clicks = ref State.GetData().Click;
                clicks.Count++;
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

                if (_grid.Swap(firstClickedTile, _secondClicked))
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
                _enemyMatches = Bakery.AsEnemies(_grid, _matchesOf3);
                State.EnemiesStillPresent = true;
            }
        }
        
        if (_grid.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!) && !_shallCreateEnemies)
        {
            Console.WriteLine($"HAD A MATCH! with {_matchesOf3.Count} elements in it!");
            State.DefaultTile = _secondClicked!;
            ref Numbers matchData = ref State.GetData(); 
            matchData.Match.Seconds = (_matchesOf3!.CreatedAt - _matchesOf3.DeletedAt).Seconds;
            matchData.Match.Count++;
            State.Matches = _matchesOf3;
            OnMatchFound();
            _grid.Delete(_matchesOf3);
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

        //Console.WriteLine(_matchesOf3!.Count);
        _shallCreateEnemies = RollADice();
        //Console.WriteLine(_shallCreateEnemies);
        State.WasSwapped = false;
        _secondClicked = null;
    }
    
    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(Level);
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
  
        while (!WindowShouldClose())
        {
            //seconds += GetFrameTime();
            float elapsedTime = Level.GameTimer.ElapsedSeconds;

            BeginDrawing();

                if (_enemyMatches is not null)
                {
                    var size = GetScreenCoord();
                    SetShaderValue(WobbleShader, shaderLoc.size, size , ShaderUniformDataType.SHADER_UNIFORM_VEC2);
                    SetShaderValue(WobbleShader, shaderLoc.time, elapsedTime, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
                }

                ClearBackground(WHITE);
                //Renderer.DrawBackground(ref bg1);
                
                if (!_enterGame)
                {
                    //Renderer.ShowWelcomeScreen();
                    //Renderer.LogQuest(false, Level);
                }
                if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
                {
                    Level.GameTimer.Run();
                    
                    bool isGameOver = Level.GameTimer.Done();

                   // Console.WriteLine("TIME  : "+Level.GameTimer.ElapsedSeconds);
                    
                    if (isGameOver)
                    {
                        if (Renderer.OnGameOver(Level.GameTimer.Done(),false))
                        {
                            //leave Gameloop and hence end the game
                            return;
                        }
                    }
                    else if (State.WasGameWonB4Timeout)
                    {
                        if (Renderer.OnGameOver(Level.GameTimer.Done(), true))
                        {
                            InitGame();
                            State.WasGameWonB4Timeout = false;
                            continue;
                        }
                    }
                    else
                    {
                        Renderer.DrawBackground(ref bgIngame2);
                        Renderer.DrawTimer(elapsedTime);
                        DragMouseToEnemies();
                        ProcessSelectedTiles();
                        ComputeMatches();
                        //Console.WriteLine(_matchesOf3.Count);
                        //Renderer.DrawOuterBox(_enemyMatches, elapsedTime);  
                        Renderer.DrawInnerBox(_matchesOf3, elapsedTime) ;  
                        Renderer.DrawGrid(_grid, elapsedTime, shaderLoc);
                        BeginShaderMode(WobbleShader);
                        Renderer.DrawMatches(_enemyMatches, _grid, elapsedTime, _shallCreateEnemies);
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
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}