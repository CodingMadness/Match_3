using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
//INITIALIZATION:................................

namespace Match_3;

internal static class Program
{
    private static Level state;
    private static GameTime globalTimer, gameOverScreenTimer;

    private static Grid _grid;
    private static readonly ISet<ITile?> MatchesOf3 = new HashSet<ITile?>(3);
    private static ITile? secondClickedTile;
    private static readonly ISet<ITile?> UndoBuffer = new HashSet<ITile?>(5);
    private static bool? wasGameWonB4Timeout;
    private static bool enterGame;
    private static int tileCounter;
    private static int missedSwapTolerance;
    private static GameText welcomeText, timerText, gameOverText;
    private static int clickCount;
    private static bool backToNormal;

    private static (bool shallReset, Vector2? beginOfMatch3Rect) pair =(false, null);
    
    private static void Main() 
    {
        InitGame();
        GameLoop();
        CleanUp();
    }
    
    private static void InitGame()
    {
        GameRuleManager.InitNewLevel();
        state = GameRuleManager.State;
        globalTimer = GameTime.GetTimer(state.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(state.GameOverScreenTime);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(state.WindowWidth, state.WindowHeight, "Match3 By Shpendicus");
        AssetManager.Init();
        //SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_NS);
        _grid = new(state);
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
        timerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        timerText.ScaleText();
        //timerText.Draw(0.5f);
    }
    
    private static void ShowWelcomeScreen(bool hideWelcome)
    {
        FadeableColor tmp = RED;
        tmp.AlphaSpeed = hideWelcome ? 1f : 0f;
        welcomeText.Color = tmp;
        welcomeText.ScaleText();
        welcomeText.Begin = (Utils.GetScreenCoord() * 0.5f) with { X = 0f };
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
        gameOverText.Begin = (Utils.GetScreenCoord() * 0.5f) with{X = 0f};
        gameOverText.Text = gameWon.Value ? "YOU WON!" : "YOU LOST";
        gameOverText.ScaleText();
        gameOverText.Draw(null);
        return gameOverScreenTimer.Done();
    }

    private static void UpdateEnemyTiles()
    {
        //foreach new match3, the internal "find" method always gets all new match3s 
        //which we dont want, we only need the current match3!
        var match3Rect = EnemyTile.DrawRectAroundMatch3(_grid);
        DrawRectangleRec(match3Rect, RED);
    }
    
    private static void ProcessSelectedTiles()
    {
        //we only fix the mouse point,
        //WHEN the cursor exceeds at a certain bounding box
        /*if (enemyCellPos != -Vector2.One)
            SetMousePosition((int)enemyCellPos.X, (int)enemyCellPos.Y);*/
            
        ITile? firstClickedTile;
        backToNormal = false;
        
        //Here we check mouseinput AND if the clickedTile is actually an enemy, then the clicks correlates to the 
        if (!_grid.TryGetClickedTile(out firstClickedTile) || firstClickedTile is null)
            return;
        
        firstClickedTile.Selected = (firstClickedTile.State & TileState.Movable)
                                    == TileState.Movable;
        
        /*
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
                }
            }
        }
        */
        
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
            
            GameTime toggleTimer = GameTime.GetTimer(500900);
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
                    //Console.WriteLine(beginOfMatch3Rect);
                }
            }
            pair.shallReset = true;
        }
        //if (++missedSwapTolerance == state.MaxAllowedSpawns)
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
                    UpdateEnemyTiles();
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