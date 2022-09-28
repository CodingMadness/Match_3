using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
//INITIALIZATION:................................

namespace Match_3;

class Program
{
    private static Level state;
    private static GameTime globalTimer, gameOverScreenTimer;

    private static Grid _tileMap;
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
        _tileMap = new(state);
        AssetManager.WelcomeFont.baseSize = 32;
        welcomeText = new(AssetManager.WelcomeFont, "Welcome young man!!", 7f);
        gameOverText = new(AssetManager.WelcomeFont, "", 7f);
        Font copy = AssetManager.WelcomeFont with {baseSize = 512*2};
        timerText = new(copy, "", 20f);
        //Console.Clear();        
    }
    
    public static void UpdateTimerOnScreen(ref GameTime timer)
    {
        if (timer.ElapsedSeconds <= timer.MAX_TIMER_VALUE / 2f)
        {
            // timer.ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;
        }
        timer.UpdateTimer();
        timerText.Text = ((int)timer.ElapsedSeconds).ToString();
        FadeableColor color = timer.ElapsedSeconds > 0f ? BLUE : WHITE;
        timerText.Color = color with { CurrentAlpha = 1f, TargetAlpha = 1f};
        timerText.Begin = (Utils.GetScreenCoord() * 0.5f) with { Y = 0f };
        timerText.ScaleText();
        timerText.Draw(0.5f);
    }
    
    private static void ShowWelcomeScreenOnLoop(bool hideWelcome)
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

        UpdateTimerOnScreen(ref gameOverScreenTimer); 
        ClearBackground(WHITE);
        gameOverText.Src.baseSize = 2;
        gameOverText.Begin = (Utils.GetScreenCoord() * 0.5f) with{X = 0f};
        gameOverText.Text = gameWon.Value ? "YOU WON!" : "YOU LOST";
        gameOverText.ScaleText();
        gameOverText.Draw(null);
        return gameOverScreenTimer.Done();
    }
    
    private static void ProcessSelectedTiles()
    {
        ITile? firstClickedTile;
        backToNormal = false;
        
        //Here we check mouseinput AND if the clickedTile is actually an enemy, then the clicks correlates to the 
        if (!_tileMap.TryGetClickedTile(out firstClickedTile) || firstClickedTile is null)
            return;

        if (firstClickedTile is EnemyTile e)
        {
            if (GameRuleManager.TryGetEnemyQuest(e.Shape as CandyShape, out int clicksNeeded))
            {
                if (clicksNeeded == ++clickCount && !e.IsDeleted)
                {
                    e.IsDeleted = true;
                    MatchesOf3.Remove(e);
                    clickCount = 0;
                    
                    if (MatchesOf3.Count == 0 && !backToNormal)
                    {
                        e.ToggleAbilitiesForNeighbors(_tileMap, backToNormal);
                        backToNormal = true;
                        //  UndoLastOperation();
                    }
                    //Console.WriteLine("Here we would have actually changed the Enemy tile to a friendly Tie");
                }
            }
        }

        //_tileMap[firstClickedTile.GridPos].Selected = true;
        firstClickedTile.Selected = true;
        
        /*No tile selected yet*/
        if (secondClickedTile is null)
        {
            secondClickedTile = firstClickedTile;
            secondClickedTile.Selected = true;
            return;
        }

        /*Same tile selected => deselect*/
        if (firstClickedTile == secondClickedTile)
        {
            secondClickedTile.Selected = false;
            secondClickedTile = null;
            return;
        }
        /*Different tile selected ==> swap*/
        else
        {
            if (firstClickedTile.IsDeleted || secondClickedTile.IsDeleted)
                return;
            
            firstClickedTile.Selected = true;
            _tileMap.Swap(firstClickedTile, secondClickedTile);
            UndoBuffer.Add(firstClickedTile);
            UndoBuffer.Add(secondClickedTile);
            secondClickedTile.Selected = false;
        }

        var candy = secondClickedTile is Tile d ? d.Shape as CandyShape : null;

        if (candy is null)
            return;
        
        if (_tileMap.WasAMatchInAnyDirection(secondClickedTile, MatchesOf3))           
        {
            UndoBuffer.Clear();

            if (GameRuleManager.TryGetMatch3Quest(candy, out int toCollect))
            {
                //tileCounter += 1;
                //Console.WriteLine($"You already got {tileCounter} match of Balltype: {candy.Ball}");
                
                if (++tileCounter == toCollect)
                {
                    Console.WriteLine($"Good job, you got your {tileCounter} match3! by {candy.Ball}");
                    //GameRuleManager.RemoveSubQuest(candy);
                    tileCounter = 0;
                    missedSwapTolerance = 0;
                }
                wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }
            
            //here begins the entire swapping from default tile to enemy tile
            //and its affects to other surrounding tiles
            foreach (ITile? match in MatchesOf3)
            {
                if (match is not null && _tileMap[match.GridPos] is not null)
                {
                    UndoBuffer.Add(_tileMap[match.GridPos]);
                    _tileMap.Delete(match.GridPos);
                    var enemy = Bakery.Transform(match as Tile);
                    _tileMap[match.GridPos] = enemy;
                    enemy.ToggleAbilitiesForNeighbors(_tileMap, true);
                }
            }
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
                    ITile? basic = _tileMap[deletedTile.GridPos];
                    
                    if (!standard.IsDeleted)
                    {
                        var firstTile = _tileMap[standard.CoordsB4Swap];
                        //Console.WriteLine(firstTile.GetAddrOfObject());
                        _tileMap.Swap(basic, firstTile);
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
                             e.ToggleAbilitiesForNeighbors(_tileMap, false);
                             _tileMap[e.GridPos] = standard;
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
        UnloadTexture(AssetManager.Default);
        CloseWindow();
    }
    
    private static void GameLoop()
    {
        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(WHITE);

            if (!enterGame)
            {
                ShowWelcomeScreenOnLoop(false);
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
                    //Keep the main loop going 
                    UpdateTimerOnScreen(ref globalTimer);
                    ShowWelcomeScreenOnLoop(true);
                    _tileMap.Draw(globalTimer.ElapsedSeconds);
                    ProcessSelectedTiles();
                    
                    if (IsKeyDown(KeyboardKey.KEY_A))
                        UndoLastOperation();
                }
                
                enterGame = true;
            }
            
            EndDrawing();
        }
    }
}