using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Match_3.Utils;
using static Raylib_CsLo.Raylib;

//INITIALIZATION:................................

namespace Match_3;

internal static class Program
{
    private static Level Level;
    private static GameTime globalTimer, gameOverScreenTimer;

    private static Grid _grid;
    private static readonly MatchX MatchesOf3 = new(3);
    private static EnemyMatches enemyMatches = new(MatchesOf3.Count, _grid);
   
    //private static readonly ISet<ITile?> UndoBuffer = new HashSet<ITile?>(5);
    private static bool? wasGameWonB4Timeout;
    private static bool enterGame;
    private static int matchCounter = 0;
    private static int missedSwapTolerance;
    private static GameText welcomeText, timerText, gameOverText;
    private static int clickCount;
    private static bool backToNormal;
    private static ITile? secondClicked;
    private static Rectangle match3Rect;

    private static Vector2 enemyCellPos;
    private static int matchXCounter = 1;
    private static bool enemiesStillThere = true;
    private static bool wasSwapped;
    private static float match3RectAlpha = 1f;

    private static MatchX currentMatch;
    private static bool ShallCreateEnemies;

    private static void ShowWelcomeScreen(bool hideWelcome)
    {
        FadeableColor tmp = RED;
        tmp.AlphaSpeed = hideWelcome ? 1f : 0f;
        welcomeText.Color = tmp;
        welcomeText.ScaleText();
        welcomeText.Begin = (GetScreenCoord() * 0.5f) with { X = 0f };
        welcomeText.Draw(null);
    }

    private static bool OnGameOver(bool? gameWon)
    {
        if (gameWon is null)
        {
            return false;
        }

        //UpdateTimer(); 
        ClearBackground(WHITE);
        gameOverText.Src.baseSize = 2;
        gameOverText.Begin = (GetScreenCoord() * 0.5f) with { X = 0f };
        gameOverText.Text = gameWon.Value ? "YOU WON!" : "YOU LOST";
        gameOverText.ScaleText();
        gameOverText.Draw(null);
        return gameOverScreenTimer.Done();
    }

    private static void Main()
    {
        InitGame();
        MainGameLoop();
        CleanUp();
    }

    private static void InitGame()
    {
        GameRuleManager.InitNewLevel();
        Level = GameRuleManager.State;
        globalTimer = GameTime.GetTimer(Level.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(Level.GameOverScreenTime);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(Level.WindowWidth, Level.WindowHeight, "Match3 By Shpendicus");
        AssetManager.Init();
        //SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_NS);
        _grid = new(Level);
        //AssetManager.WelcomeFont.baseSize = 32;
        //USE A TEXTURE FOR LOSE AND WIN ASWELL AS FOR WELCOME TO AVOID HIGH VIDEO/G-RAM USAGE!
        welcomeText = new(AssetManager.WelcomeFont, "Welcome young man!!", 7f);
        gameOverText = new(AssetManager.WelcomeFont, "", 7f);
        Font copy = GetFontDefault() with { baseSize = 512 * 2 };
        timerText = new(copy, "", 20f);
        //Console.Empty();        
    }

    public static void UpdateTimer()
    {
        if (globalTimer.ElapsedSeconds <= globalTimer.MAX_TIMER_VALUE / 2f)
        {
            // timer.ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;
        }

        globalTimer.Run();

        //(int start, int end) = _grid.MakePlaceForTimer();
        //GetMatch3Rect(start, 0, end-start, ITile.SIZE, RED);
        timerText.Text = ((int)globalTimer.ElapsedSeconds).ToString();
        FadeableColor color = globalTimer.ElapsedSeconds > 0f ? BLUE : WHITE;
        timerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f };
        timerText.Begin = (GetScreenCoord() * 0.5f) with { Y = 0f };
        timerText.ScaleText();
        //timerText.Draw(0.5f);
    }

    private static void HideWelcomeScreen()
    {
        ShowWelcomeScreen(true);
    }

    private static void HandleMouseEvents()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        if (enemyCellPos != Vector2.Zero)
        {
            bool outsideOfRect = !CheckCollisionPointRec(GetMousePosition(), match3Rect);

            if (outsideOfRect && enemiesStillThere)
            {
                /*the player has to get these enemies out of the way b4 he can pass!*/
                SetMousePos(enemyCellPos);
            }
            else
            {
            }
        }
    }

    private static void ProcessSelectedTiles()
    {
        if (_grid.NothingClicked(out var tmpFirst))
            return;

        //tmpFirst!.Selected = (tmpFirst.State & State.Movable) == State.Movable;
        Console.WriteLine($"{tmpFirst.Cell} was clicked!");

        /*No tile selected yet*/
        if (secondClicked is null)
        {
            //prepare for next round, so we store first in second!
            secondClicked = tmpFirst;
            secondClicked.State = State.Selected;
            return;
        }

        /*Same tile selected => deselect*/
        if (tmpFirst == secondClicked)
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

        void DeleteMatchFromGrid()
        {
            
        }
        
        bool ShallTransformMatchesToEnemyMatches() => Randomizer.NextSingle() <= 0.5f;
            
        if (_grid.WasAMatchInAnyDirection(secondClicked!, MatchesOf3) && !ShallCreateEnemies)
        {
            if ((secondClicked as Tile)!.Body is not TileShape body)
                return;
            
            _grid.Delete();
            
            if (CheckIfMatchQuestWasMet(body))
            {
                Console.WriteLine($"Good job, you got the {matchCounter} match3! for {body.Ball} Balls");
                wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }

            ShallCreateEnemies = ShallTransformMatchesToEnemyMatches();
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
        MatchesOf3.Empty();
    }
    
    private static void HandleEnemies()
    {
        if (_grid.NothingClicked(out var enemyTile))
            return;

        //Enemy tile was clicked on , ofc after a matchX happened!
        if (enemyTile is EnemyTile e)
        {
            //IF it is an enemy, YOU HAVE TO delete them before u can continue
            if (GameRuleManager.TryGetEnemyQuest((TileShape)e.Body, out int clicksNeeded))
            {
                if (clicksNeeded == ++clickCount /*&& e.State == State.Clean*/)
                {
                    e.State = State.Deleted;
                    clickCount = 0;

                    if (!backToNormal)
                    {
                        e.BlockSurroundingTiles(_grid, false);
                        backToNormal = true;
                    }
                    Console.WriteLine(matchXCounter++);
                }
                enemiesStillThere = matchXCounter <= Level.MatchConstraint;
            }
        }
    }

    private static bool CreateEnemiesIfNeeded()
    {
        if (ShallCreateEnemies)
        {
            //we now create here the enemies
            enemyMatches = enemyMatches.Count == 0 ? MatchesOf3.Transform(_grid) : enemyMatches;
        }
        else
        {
            return true;
        }

        return false;
    }

    private static void DrawRectAroundEnemyTiles()
    {
        //foreach new match3, the internal "find" method always gets all new match3s 
        //which we dont want, we only need the current match3!
        var tmp = enemyMatches.MapRect;

        if (tmp.x == 0f && tmp.y == 0f)
            DrawRectangleRec(match3Rect, ColorAlpha(RED, match3RectAlpha)); //invisible
        else
        {
            match3Rect = tmp;
            DrawRectangleRec(tmp, ColorAlpha(RED, match3RectAlpha)); //visible
        }
    }

    private static void DrawGrid()
    {
        _grid.Draw(globalTimer.ElapsedSeconds);
    }

    private static void HardResetIf_A_Pressed()
    {
        if (IsKeyDown(KeyboardKey.KEY_A))
        {
            _grid = new Grid(Level);
            enemiesStillThere = false;
            match3RectAlpha = 0f;
            secondClicked = null;
            Console.Clear();
        }
    }

    private static void MainGameLoop()
    {
        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(WHITE);
            DrawTexture(AssetManager.BGAtlas, 0, 0, WHITE);

            if (!enterGame)
            {
                ShowWelcomeScreen(false);
                GameRuleManager.LogQuest(false);
            }

            if (IsKeyDown(KeyboardKey.KEY_ENTER) || enterGame)
            {
                bool isGameOver = globalTimer.Done();

                if (isGameOver)
                {
                    if (OnGameOver(false))
                    {
                        //leave Gameloop and hence end the game
                        return;
                    }
                }
                else if (wasGameWonB4Timeout == true)
                {
                    if (OnGameOver(true))
                    {
                        InitGame();
                        wasGameWonB4Timeout = false;
                        continue;
                    }
                }
                else
                {
                    //UPDATE-CALLS! 
                    UpdateTimer();
                    HideWelcomeScreen();
                    HandleMouseEvents();
                    ProcessSelectedTiles();
                    ComputeMatches();
                    
                    if (CreateEnemiesIfNeeded()) 
                        HandleEnemies();
                        
                    //Draw has to be always at any time ! 
                    DrawRectAroundEnemyTiles();
                    DrawGrid();
                    HardResetIf_A_Pressed();
                }

                enterGame = true;
            }

            EndDrawing();
        }
    }

    private static void CleanUp()
    {
        UnloadTexture(AssetManager.DefaultTileAtlas);
        CloseWindow();
    }
}