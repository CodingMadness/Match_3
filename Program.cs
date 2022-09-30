using System.Numerics;
using System.Runtime.CompilerServices;
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
    private static readonly ISet<ITile?> MatchesOf3 = new HashSet<ITile?>(3);
    private static readonly ISet<ITile?> EnemiessPerMatch = new HashSet<ITile?>(3);
    private static readonly ISet<ITile?> UndoBuffer = new HashSet<ITile?>(5);
    private static bool? wasGameWonB4Timeout;
    private static bool enterGame;
    private static int tileCounter;
    private static int missedSwapTolerance;
    private static GameText welcomeText, timerText, gameOverText;
    private static int clickCount;
    private static bool backToNormal;
    private static ITile? secondClickedTile;
    private static Rectangle match3Rect;

    private static Vector2 enemyCellPos;
    private static int matchXCounter = 1;
    private static bool enemiesStillThere = true;

    private static void Main() 
    {
        InitGame();
        GameLoop();
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
        Font copy = GetFontDefault() with {baseSize = 512*2};
        timerText = new(copy, "", 20f); 
        //Console.Clear();        
    }
   
    public static void UpdateTimer(ref GameTime timer)
    {
        if (timer.ElapsedSeconds <= timer.MAX_TIMER_VALUE / 2f)
        {
            // timer.ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;
        }
        
        timer.Run();
        
        //(int start, int end) = _grid.MakePlaceForTimer();
        //GetMatch3Rect(start, 0, end-start, ITile.Size, RED);
        timerText.Text = ((int)timer.ElapsedSeconds).ToString();
        FadeableColor color = timer.ElapsedSeconds > 0f ? BLUE : WHITE;
        timerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f};
        timerText.Begin = (GetScreenCoord() * 0.5f) with { Y = 0f };
        timerText.ScaleText();
        //timerText.Draw(0.5f);
    }
    
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

        UpdateTimer(ref gameOverScreenTimer); 
        ClearBackground(WHITE);
        gameOverText.Src.baseSize = 2;
        gameOverText.Begin = (GetScreenCoord() * 0.5f) with{X = 0f};
        gameOverText.Text = gameWon.Value ? "YOU WON!" : "YOU LOST";
        gameOverText.ScaleText();
        gameOverText.Draw(null);
        return gameOverScreenTimer.Done();
    }

    private static void DrawRectAroundEnemyTiles()
    {
        //foreach new match3, the internal "find" method always gets all new match3s 
        //which we dont want, we only need the current match3!
        var tmp = EnemyTile.GetRectAroundMatch3(EnemiessPerMatch);
        
        if (tmp.x == 0f && tmp.y == 0f)
            DrawRectangleRec(match3Rect, ColorAlpha(RED, 1f)); //invisible
        else
        {
            match3Rect = tmp;
            DrawRectangleRec(tmp, ColorAlpha(RED, 1f)); //invisible
        }
        //Console.WriteLine(EnemiessPerMatch.Count);
        EnemiessPerMatch.Clear();
    }
    
    private static void ProcessSelectedTiles()
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
                //Console.WriteLine("I am inside rect!");
                //move freely!
            }
        }
      
        backToNormal = false; 
        
        //Here we check mouse input AND if the clickedTile is actually an enemy, then the clicks correlates to the 
        if (_grid.NothingClicked(out var firstClickedTile))
            return;

        firstClickedTile.Selected = (firstClickedTile.State & TileState.Movable)
                                    == TileState.Movable;
        
        if (firstClickedTile is EnemyTile e)
        {
            //IF it is an enemy, YOU HAVE TO delete them before u can continue
            if (GameRuleManager.TryGetEnemyQuest(e.Body as CandyShape, out int clicksNeeded))
            {
                if (clicksNeeded == ++clickCount && !e.IsDeleted)
                {
                    e.IsDeleted = true;
                    clickCount = 0;

                    if (!backToNormal)
                    {
                        e.BlockSurroundingTiles(_grid, backToNormal);
                        backToNormal = true;
                    }
                    Console.WriteLine(matchXCounter++);
                }

                enemiesStillThere = matchXCounter <= Level.MatchConstraint;
            }
        }
        
        /*No tile selected yet*/
        if (secondClickedTile is null)
        {
            secondClickedTile = firstClickedTile;
            return;
        }

        /*Same tile selected => deselect*/
        if (firstClickedTile == secondClickedTile)
        {
            firstClickedTile.Selected = false;
            secondClickedTile.Selected = false;
            secondClickedTile = null;
            return;
        }
       
        /*Different tile selected ==> swap*/
        else
        {
            if (firstClickedTile.IsDeleted || secondClickedTile.IsDeleted)
                return;
            
            _grid.Swap(firstClickedTile, secondClickedTile);
            UndoBuffer.Add(firstClickedTile);
            UndoBuffer.Add(secondClickedTile);
            secondClickedTile.Selected = false;
        }

        var candy = secondClickedTile is Tile d ? d.Body as CandyShape : null;

        if (candy is null)
            return;
        
        if (_grid.WasAMatchInAnyDirection(secondClickedTile, MatchesOf3))
        {
            UndoBuffer.Clear();

            if (GameRuleManager.TryGetMatch3Quest(candy, out int toCollect))
            {
                //tileCounter += 1;
                //Console.WriteLine($"You already got {tileCounter} match of Balltype: {candy.Ball}");
                
                if (++tileCounter == toCollect)
                {
                    Console.WriteLine($"Good job, you got your {tileCounter} match3! by {candy.Ball}");
                    tileCounter = 0;
                    missedSwapTolerance = 0;
                }
                wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }
            
            //GameTime toggleTimer = GameTime.GetTimer(500900);
            //here begins the entire swapping from default tile to enemy tile
            //and its affects to other surrounding tiles
            bool colorOnlyThe1One = true;
            
            foreach (ITile? match in MatchesOf3)
            {
                if (match is not null && _grid[match.Cell] is not null)
                {
                    UndoBuffer.Add(match);
                    _grid.Delete(match.Cell);
                    var enemy = Bakery.Transform(match as Tile);
                    _grid[match.Cell] = enemy;
                    enemy.BlockSurroundingTiles(_grid, true);
                    EnemiessPerMatch.Add(enemy);
                    enemyCellPos = enemy.Cell;
                }
            }
        }
        //if (++missedSwapTolerance == Level.MaxAllowedSpawns)
        //{
        //    Console.WriteLine("UPSI! you needed to many swaps to get a match now enjoy the punishment of having to collect MORE THAN BEFORE");
        //    GameRuleManager.ChangeSubQuest(candy, tileCounter + 3);
        //    missedSwapTolerance = 0;
        //}
        //if (MatchesOf3.Count == 0)
        //{
        //    secondClickedTile = (Tile)firstClickedTile;
        //    Console.WriteLine("calling GOTO in order to try it with the 1. tile");
        //    MatchesOf3.Add(null!); //this line we just do, in order to interrupt the GOTO call again!
        //    goto TryMatchWithFirstTile;
        //}
        //Console.WriteLine(firstClickedTile);
        MatchesOf3.Clear();
        secondClickedTile = null;
        firstClickedTile.Selected = false;
    }

    private static void UndoLastOperation()
    {
        //UNDO...!
        {
            bool triggeredMatch = false;
            
            foreach (var deletedTile in UndoBuffer)
            {
                if (deletedTile is Tile standard)
                {
                    //check if they have been ONLY swapped without leading to a match3
                    ITile? basic = _grid[deletedTile.Cell];
                    
                    if (!standard.IsDeleted)
                    {
                        var firstTile = _grid[standard.CoordsB4Swap];
                        //Console.WriteLine(firstTile.GetAddrOfObject());
                        _grid.Swap(basic, firstTile);
                    }
                    else
                    {
                        //their has been a match3 after swap!
                         //Case-1: they have been "Disabled", so "Enable" them again
                         if (standard.IsDeleted && basic is not EnemyTile)
                         {
                             standard.Enable();
                         }
                         //Case-2: they have been "transformed" to enemytiles, so we have to "Enable()" all surrounding
                         //tiles which were prev. disabled
                         if (basic is EnemyTile e)
                         {
                             //Console.WriteLine(e.GetAddrOfObject());
                             e.BlockSurroundingTiles(_grid, false);
                             _grid[e.Cell] = standard;
                             standard.Enable();
                             standard.IsDeleted = false;
                         }
                    }
                }
            }
            UndoBuffer.Clear();
        }
    }
    
    private static void CleanUp()
    {
        UnloadTexture(AssetManager.DefaultTileAtlas);
        CloseWindow();
    }

    private static void DrawGrid()
    {
        _grid.Draw(globalTimer.ElapsedSeconds);
    }
    
    private static void GameLoop()
    {
        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(WHITE);
            DrawTexture(AssetManager.BGAtlas,0,0, WHITE);

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
                        //return;
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
                    //DRAW-CALLS! 
                    UpdateTimer(ref globalTimer);
                    ShowWelcomeScreen(true);
                    ProcessSelectedTiles();
                    DrawRectAroundEnemyTiles();
                    DrawGrid();
                    /*
                    if (IsKeyDown(KeyboardKey.KEY_A))
                        UndoLastOperation();*/
                }
                enterGame = true;
            }
            EndDrawing();
        }
    }
}