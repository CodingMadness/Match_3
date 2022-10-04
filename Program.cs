using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;

using static Match_3.AssetManager;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;

//INITIALIZATION:................................

namespace Match_3;

internal static class Program
{
    private static Level _level;
    private static GameTime _globalTimer;
    private static Grid _grid;
    private static MatchX? _matchesOf3;
    private static EnemyMatches? _enemyMatches;
    private static Tile? _secondClicked;
    
    private static bool? _wasGameWonB4Timeout;
    private static bool _enterGame;
    private static int _matchCounter;
    private static int _missedSwapTolerance;
    private static int _clickCount;
    private static bool _backToNormal;
    private static bool _enemiesStillThere = true;
    private static bool _wasSwapped;
    private static float _match3RectAlpha = 1f;
    private static bool _shallCreateEnemies;
    
    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        _matchesOf3 = new(3);
        GameRuleManager.InitNewLevel();
        _level = GameRuleManager.State;
        _globalTimer = GameTime.GetTimer(_level.GameStartAt);
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

            if (outsideRect)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMouseToWorldPos(_enemyMatches.BeginInWorld, 1);
            }
            else
            {
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
        Console.WriteLine($"{tmpFirst!.GridCell} was clicked!");

        /*No tile selected yet*/
        if (_secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            _secondClicked = tmpFirst;
            _secondClicked.State |= State.Selected;
            return;
        }

        /*Same tile selected => deselect*/
        if (StateAndBodyComparer.Singleton.Equals(tmpFirst, _secondClicked))
        {
            //firstClickedTile.Selected = false;
            Console.Clear();
            Console.WriteLine($"{tmpFirst.GridCell} was clicked AGAIN!");
            _secondClicked.State |= State.Selected;
            _secondClicked = null;
        }
        /*Different tile selected ==> swap*/
        else
        {
            tmpFirst.State |= State.Selected;

            if (_grid.Swap(tmpFirst, _secondClicked))
            {
                //Console.WriteLine("first and second were swapped successfully!");
                //both "first" are in this case the second, due to the swap!
                //ComputeMatches((_secondClicked as Tile)!);
                _wasSwapped = true;
            }
        }
    }
    
    static void ComputeMatches()
    {
        if (!_wasSwapped)
            return;
        
        bool CheckIfMatchQuestWasMet(TileShape body)
        {
            if (GameRuleManager.TryGetMatch3Quest(body, out int matchesNeeded))
            {
                if (++_matchCounter == matchesNeeded)
                {
                    _matchCounter = 0;
                    _missedSwapTolerance = 0;
                    GameRuleManager.RemoveSubQuest(body);
                    return true;
                }
            }
            return false;
        }
        
        bool ShallTransformMatchesToEnemyMatches() => Randomizer.NextSingle() <= 0.5f;

        if (_grid.WasAMatchInAnyDirection(_secondClicked!, _matchesOf3!) /*&& !_shallCreateEnemies*/)
        {
            if (_secondClicked!.Body is not TileShape body)
                return;

            //_grid.Delete(_matchesOf3!);
            
            if (CheckIfMatchQuestWasMet(body))
            {
                Console.WriteLine($"Good job, you got the {_matchCounter} match3! for {body.Ball} Balls");
                _wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }
            _shallCreateEnemies = false; //ShallTransformMatchesToEnemyMatches();
        }
        
        else
            _matchesOf3!.Empty();

        _shallCreateEnemies = true;
        _wasSwapped = false;
        _secondClicked = null;
    }
    
    private static void HandleEnemyMatches()
    {
        if (!TileClicked(out var enemyTile))
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (enemyTile is EnemyTile e)
        {
            //IF it is an enemy, YOU HAVE TO delete them before u can continue
            if (GameRuleManager.TryGetEnemyQuest((TileShape)e.Body, out int clicksNeeded))
            {
                if (clicksNeeded == ++_clickCount)
                {
                    e.State = State.Deleted;
                    _clickCount = 0;

                    if (!_backToNormal)
                    {
                        e.BlockSurroundingTiles(_grid, false);
                        _backToNormal = true;
                    }
                    Console.WriteLine(_matchCounter++);
                }
                _enemiesStillThere = _matchCounter <= _level.MatchConstraint;
            }
        }
    }

    private static bool CreateEnemiesIfNeeded()
    {
        if (_shallCreateEnemies && 
            (_enemyMatches is null || _enemyMatches.Count == 0) &&
            _matchesOf3?.Count > 0)
        {
            _enemyMatches = _matchesOf3?.AsEnemies(_grid);
        }
        return _shallCreateEnemies;
    }

    private static void HardReset()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(_level);
            _shallCreateEnemies = true;
            _matchesOf3?.Empty();
            _enemyMatches?.Empty();
            _enemiesStillThere = false;
            _match3RectAlpha = 0f;
            _secondClicked = null;
            _wasSwapped = false;
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
                Renderer.LogQuest(false, _level);
            }

            if (IsKeyDown(KeyboardKey.KEY_ENTER) || _enterGame)
            {
                bool isGameOver = _globalTimer.Done();

                if (isGameOver)
                {
                    if (Renderer.OnGameOver(ref _globalTimer,false))
                    {
                        //leave Gameloop and hence end the game
                        return;
                    }
                }
                else if (_wasGameWonB4Timeout == true)
                {
                    if (Renderer.OnGameOver(ref _globalTimer, true))
                    {
                        InitGame();
                        _wasGameWonB4Timeout = false;
                        continue;
                    }
                }
                else
                {
                    Renderer.ShowWelcomeScreen(true);
                    Renderer.UpdateTimer(ref _globalTimer);
                    CenterMouseToEnemyMatch();
                    ProcessSelectedTiles();
                    ComputeMatches();
      
                    if (CreateEnemiesIfNeeded()) 
                        HandleEnemyMatches();

                    Renderer.DrawOuterBox(_enemyMatches, _globalTimer.ElapsedSeconds);  //works!
                    Renderer.DrawInnerBox(_matchesOf3, _globalTimer.ElapsedSeconds) ;  //works!
                    Renderer.DrawGrid(_grid, _globalTimer.ElapsedSeconds);
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