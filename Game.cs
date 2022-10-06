using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

using static Match_3.AssetManager;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;

namespace Match_3;

internal static class Game
{
    private static Level _level;
    private static Grid _grid;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    
    public static GameState StatePerLevel { get; private set; }
    
    private static bool _enterGame;
    private static bool _shallCreateEnemies;

    public static event Action<GameState> OnMatchFound;
    public static event Action<GameState> OnTileClicked;
    public static event Action<GameState> OnTileSwapped;
    
    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        _level = new(0,45, 4, 7, 7, 64, null);
        StatePerLevel = new();
        _matchesOf3 = new(Goal.MAX_TILES_PER_MATCH);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(_level.WindowWidth, _level.WindowHeight, "Match3 By Shpendicus");
        LoadAssets();
        _grid = new(_level);
    }
    
    private static void CenterMouseToEnemyMatch()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (_enemyMatches?.BeginInWorld != INVALID_CELL && _enemyMatches is not null)
        {
            bool outsideRect = !CheckCollisionPointRec(GetMousePosition(), _enemyMatches.Border);

            if (outsideRect && StatePerLevel.AreEnemiesStillPresent)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.BeginInWorld, 1);
            }
            else if (!StatePerLevel.AreEnemiesStillPresent && _enemyMatches.IsMatch)
            {
                //we set this to null, because we cant make any swaps after this, cause 
                //_secondClicked has a value! so we then can repeat the entire cycle!
                _secondClicked = null;
                //enemies were created from the matchesOf3, so we have to
                //delete all of them, because else we will reference always the base-matches internally which is bad!
                _enemyMatches.Clear(); 
                _matchesOf3?.Clear();
                //move freely
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
        return tile is not null;
    }
    
    private static void ProcessSelectedTiles()
    {
        if (!TileClicked(out var tmpFirst))
            return;

        //tmpFirst!.Selected = (tmpFirst.State & State.Movable) == State.Movable;
        //Console.WriteLine($"{tmpFirst!.GridCell} was clicked!");

        /*No tile selected yet*/
        if (_secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            _secondClicked = tmpFirst;
            _secondClicked!.State |= State.Selected;
            return;
        }

        /*Same tile selected => deselect*/
        if (StateAndBodyComparer.Singleton.Equals(tmpFirst, _secondClicked))
        {
            Console.Clear();
            //Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
            _secondClicked.State |= State.Selected;
            _secondClicked = null;
        }
        /*Different tile selected ==> swap*/
        else
        {
            tmpFirst!.State |= State.Selected;

            if (_grid.Swap(tmpFirst, _secondClicked))
            {
                StatePerLevel.WasSwapped = true;
                OnTileSwapped(StatePerLevel);
            }
        }
    }
    
    static void ComputeMatches()
    {
        if (!StatePerLevel.WasSwapped)
            return;

        bool ShallTransformMatchesToEnemyMatches() => Randomizer.NextSingle() <= 0.5f;

        void CreateEnemiesIfNeeded()
        {
            if (_shallCreateEnemies && 
                (_enemyMatches is null || _enemyMatches.Count == 0) &&
                _matchesOf3?.Count > 0)
            {
                _enemyMatches = _matchesOf3?.AsEnemies(_grid);
            }
        }
        
        if (_grid.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!) /*&& !_shallCreateEnemies*/)
        {
            if (_secondClicked?.Body is TileShape t)
            {
                StatePerLevel.CollectPair = (t.TileType, 1);
                StatePerLevel.Swapped = (t.TileType, 1);
                OnMatchFound(StatePerLevel);
            }

            _shallCreateEnemies = true;
            CreateEnemiesIfNeeded();
        }
        else
            _matchesOf3!.Clear();
        
        StatePerLevel.WasSwapped = false;
        _secondClicked = null;
    }
    
    private static void HandleEnemyMatches()
    {
        if (!TileClicked(out var enemyTile))
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (enemyTile is EnemyTile e)
        {
            StatePerLevel.Enemy = e;
            StatePerLevel.Map = _grid;
        }
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(_level);
            _shallCreateEnemies = true;
            _matchesOf3?.Clear();
            _enemyMatches?.Clear();
            StatePerLevel.AreEnemiesStillPresent = false;
            _secondClicked = null;
            StatePerLevel.WasSwapped = false;
            Console.Clear();
        }
    }

    private static void MainGameLoop()
    {
        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(WHITE);
            Renderer.DrawBackground();

            if (!_enterGame)
            {
                Renderer.ShowWelcomeScreen(false);
                //Renderer.LogQuest(false, _level);
            }
            if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
            {
                bool isGameOver = _level.GameTimer.Done();

                Console.WriteLine("TIME  : "+_level.GameTimer.ElapsedSeconds);
                
                if (isGameOver)
                {
                    if (Renderer.OnGameOver(_level.GameTimer,false))
                    {
                        //leave Gameloop and hence end the game
                        return;
                    }
                }
                else if (StatePerLevel.WasGameWonB4Timeout)
                {
                    if (Renderer.OnGameOver(_level.GameTimer, true))
                    {
                        InitGame();
                        StatePerLevel.WasGameWonB4Timeout = false;
                        continue;
                    }
                }
                else
                {
                    Renderer.DrawTimer(_level.GameTimer);
                    Renderer.ShowWelcomeScreen(true);
                    Renderer.DrawTimer(_level.GameTimer);
                    CenterMouseToEnemyMatch();
                    ProcessSelectedTiles();
                    ComputeMatches();
                    HandleEnemyMatches();
                    Renderer.DrawOuterBox(_enemyMatches, _level.GameTimer.ElapsedSeconds);  //works!
                    Renderer.DrawInnerBox(_matchesOf3, _level.GameTimer.ElapsedSeconds) ;  //works!
                    Renderer.DrawGrid(_grid, _level.GameTimer.ElapsedSeconds);
                    HardReset();
                }
                _enterGame = true;
            }
            
            EndDrawing();
        }
    }

    private static void CleanUp()
    {
        UnloadTexture(DefaultTileAtlas);
        CloseWindow();
    }
}