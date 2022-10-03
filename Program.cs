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
    private static Level Level;
    private static GameTime globalTimer;
    private static Grid _grid;
    private static MatchX? matchesOf3;
    private static EnemyMatches? enemyMatches;
    private static Tile? secondClicked;
    
    private static bool? wasGameWonB4Timeout;
    private static bool enterGame;
    private static int matchCounter;
    private static int missedSwapTolerance;
    private static int clickCount;
    private static bool backToNormal;
    private static bool enemiesStillThere = true;
    private static bool wasSwapped;
    private static float match3RectAlpha = 1f;
    private static bool shallCreateEnemies;
    
    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        matchesOf3 = new(3);
        GameRuleManager.InitNewLevel();
        Level = GameRuleManager.State;
        globalTimer = GameTime.GetTimer(Level.GameStartAt);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        LoadAssets();
        _grid = new(Level);
    }
    
    private static void CenterMouse()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (matchesOf3?.Begin != Vector2.Zero && enemyMatches is not null)
        {
            bool outsideOfRect = CheckCollisionPointRec(GetMousePosition(), enemyMatches.MapRect);

            if (outsideOfRect && enemiesStillThere)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMousePos(matchesOf3!.Begin);
            }
            else
            {
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
        Console.WriteLine($"{tmpFirst!.Cell} was clicked!");

        /*No tile selected yet*/
        if (secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            secondClicked = tmpFirst;
            secondClicked.State |= State.Selected;
            return;
        }

        /*Same tile selected => deselect*/
        if (StateAndBodyComparer.Singleton.Equals(tmpFirst, secondClicked))
        {
            //firstClickedTile.Selected = false;
            Console.Clear();
            Console.WriteLine($"{tmpFirst.Cell} was clicked AGAIN!");
            secondClicked.State |= State.Selected;
            secondClicked = null;
        }
        /*Different tile selected ==> swap*/
        else
        {
            tmpFirst.State |= State.Selected;

            if (_grid.Swap(tmpFirst, secondClicked))
            {
                //Console.WriteLine("first and second were swapped successfully!");
                //both "first" are in this case the second, due to the swap!
                //ComputeMatches((secondClicked as Tile)!);
                wasSwapped = true;
            }
        }
    }
    
    static void ComputeMatches()
    {
        if (!wasSwapped)
            return;
        
        bool CheckIfMatchQuestWasMet(TileShape body)
        {
            if (GameRuleManager.TryGetMatch3Quest(body, out int matchesNeeded))
            {
                if (++matchCounter == matchesNeeded)
                {
                    matchCounter = 0;
                    missedSwapTolerance = 0;
                    GameRuleManager.RemoveSubQuest(body);
                    return true;
                }
            }
            return false;
        }
        
        bool ShallTransformMatchesToEnemyMatches() => Randomizer.NextSingle() <= 0.5f;

        if (_grid.WasAMatchInAnyDirection(secondClicked!, matchesOf3!) /*&& !shallCreateEnemies*/)
        {
            if (secondClicked!.Body is not TileShape body)
                return;
            
            //_grid.Delete(matchesOf3!);
            
            if (CheckIfMatchQuestWasMet(body))
            {
                Console.WriteLine($"Good job, you got the {matchCounter} match3! for {body.Ball} Balls");
                wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }

            shallCreateEnemies = true; //ShallTransformMatchesToEnemyMatches();
            
            if (!shallCreateEnemies)
                matchesOf3!.Empty();
        }

        //if (++missedSwapTolerance == Level.MaxAllowedSpawns)
        //{
        //    Console.WriteLine("UPSI! you needed to many swaps to get a match now enjoy the punishment of having to collect MORE THAN BEFORE");
        //    GameRuleManager.ChangeSubQuest(candy, matchCounter + 3);
        //    missedSwapTolerance = 0;
        //}
        
        //reset values to default to repeat the code for the next input!
        wasSwapped = false;
        secondClicked = null;
    }
    
    private static void HandleEnemies()
    {
        if (!TileClicked(out var enemyTile))
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (enemyTile is EnemyTile e)
        {
            //INVOKE MatchX.NotifyMeWhenMatchTileWasClicked//
            
            //IF it is an enemy, YOU HAVE TO delete them before u can continue
            if (GameRuleManager.TryGetEnemyQuest((TileShape)e.Body, out int clicksNeeded))
            {
                if (clicksNeeded == ++clickCount)
                {
                    e.State = State.Deleted;
                    clickCount = 0;

                    if (!backToNormal)
                    {
                        e.BlockSurroundingTiles(_grid, false);
                        backToNormal = true;
                    }
                    Console.WriteLine(matchCounter++);
                }
                enemiesStillThere = matchCounter <= Level.MatchConstraint;
            }
        }
    }

    private static bool CreateEnemiesIfNeeded()
    {
        if (shallCreateEnemies)
        {
            //we now create here the enemies
            enemyMatches ??= matchesOf3?.MakeEnemies(_grid);
            
        }
        return shallCreateEnemies;
    }

    private static void HardResetIf_A_Pressed()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(Level);
            shallCreateEnemies = true;
            matchesOf3?.Empty();
            enemyMatches?.Empty();
            enemiesStillThere = false;
            match3RectAlpha = 0f;
            secondClicked = null;
            wasSwapped = false;
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

            if (!enterGame)
            {
                Renderer.ShowWelcomeScreen(false);
                Renderer.LogQuest(false, Level);
            }

            if (IsKeyDown(KeyboardKey.KEY_ENTER) || enterGame)
            {
                bool isGameOver = globalTimer.Done();

                if (isGameOver)
                {
                    if (Renderer.OnGameOver(ref globalTimer,false))
                    {
                        //leave Gameloop and hence end the game
                        return;
                    }
                }
                else if (wasGameWonB4Timeout == true)
                {
                    if (Renderer.OnGameOver(ref globalTimer, true))
                    {
                        InitGame();
                        wasGameWonB4Timeout = false;
                        continue;
                    }
                }
                else
                {
                    Renderer.ShowWelcomeScreen(true);
                    Renderer.UpdateTimer(ref globalTimer);
                    CenterMouse();
                    ProcessSelectedTiles();
                    ComputeMatches();
                    
                    if (CreateEnemiesIfNeeded()) 
                        HandleEnemies();
                    
                    Renderer.DrawBorder(enemyMatches);
                    Renderer.DrawGrid(_grid, globalTimer.ElapsedSeconds);
                    HardResetIf_A_Pressed();
                }
                enterGame = true;
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