using System.Numerics;
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
        InitWindow(state.WINDOW_WIDTH, state.WINDOW_HEIGHT, "Match3 By Shpendicus");
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
    
    private static void ShowWelcomeScreenOnLoop(bool shallClearTxt)
    {
        FadeableColor tmp = RED;
        tmp.CurrentAlpha = shallClearTxt ? 0f : 1f;
        tmp.TargetAlpha = 1f;
        
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

    private static void SaveDeletedMatches(IEnumerable<ITile?> tiles)
    {
        //ITile.GetAtlas() = AssetManager.MatchBlockAtlas;
        
        foreach (ITile? match in tiles)
        {
            if (match is not null && _tileMap[match.CurrentCoords] is not null)
            {
                Tile? current = _tileMap[match.CurrentCoords] as Tile;
                UndoBuffer.Add(current);
                MatchBlockTile madBall = Bakery.Transform(current!, _tileMap);
                _tileMap.Delete(match.CurrentCoords);
                _tileMap[match.CurrentCoords] = madBall;
            }
        }
    }
    
    private static void ProcessSelectedTiles()
    {
        if (!_tileMap.TryGetClickedTile(out ITile? firstClickedTile) || firstClickedTile is null)
            return;

        //_tileMap[firstClickedTile.CurrentCoords].Selected = true;
        firstClickedTile.Selected = true;
        
        /*No tile selected yet*/
        if (secondClickedTile is null)
        {
            secondClickedTile = firstClickedTile;
            secondClickedTile.Selected = true;
            return;
        }

        /*Same tile selected => deselect*/
        if (firstClickedTile.Equals(secondClickedTile))
        {
            secondClickedTile.Selected = false;
            secondClickedTile = null;
            return;
        }
        /*Different tile selected ==> swap*/
        else
        {
            firstClickedTile.Selected = true;
            _tileMap.Swap(firstClickedTile, secondClickedTile);
            UndoBuffer.Add(firstClickedTile);
            UndoBuffer.Add(secondClickedTile);
            secondClickedTile.Selected = false;
        }

        var candy = secondClickedTile is Tile d ? d.Shape as CandyShape : null;

        if (candy is null)
            return;
        
        if (_tileMap.MatchInAnyDirection(secondClickedTile, MatchesOf3))           
        {
            UndoBuffer.Clear();

            if (GameRuleManager.TryGetSubQuest(candy, out int toCollect))
            {
                tileCounter += 1;
                //Console.WriteLine($"You already got {tileCounter} match of Balltype: {candy.Ball}");
                
                if (tileCounter == toCollect)
                {
                    // Console.WriteLine($"Good job, you got your {tileCounter} match3! by {candy.Ball}");
                    GameRuleManager.RemoveSubQuest(candy);
                    tileCounter = 0;
                    missedSwapTolerance = 0;
                }
                wasGameWonB4Timeout = GameRuleManager.IsQuestDone();
            }
            SaveDeletedMatches(MatchesOf3);
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
        bool keyDown = (IsKeyDown(KeyboardKey.KEY_A));

        //UNDO...!
        if (keyDown)
        {
            bool wasSwappedBack = false;
            //ITile.SetAtlas(ref AssetManager.Default);
            
            foreach (var storedItem in UndoBuffer)
            {
                //check if they have been ONLY swapped without leading to a match3
                if (storedItem is not null)
                {
                    _tileMap[storedItem.CurrentCoords] = null!;
                  
                    if (!wasSwappedBack && _tileMap[storedItem.CurrentCoords] is not null)
                    {
                        var secondTile = _tileMap[storedItem.CurrentCoords];
                        var firstTie = _tileMap[storedItem.CoordsB4Swap];
                        _tileMap.Swap(secondTile, firstTie);
                        wasSwappedBack = true;
                    }
                    else
                    {
                        //their has been a match3 after swap!
                        //for delete we dont have a .IsDeleted, cause we onl NULL
                        //a tile at a certain coordinate, so we test for that
                        //if (_tileMap[storedItem.CurrentCoords] is { } backupItem)
                        var tmp = (_tileMap[storedItem.CurrentCoords] = storedItem) as Tile;
                        tmp!.Selected = false;
                        tmp.ChangeTo(WHITE);
                    }
                }
                if (!wasSwappedBack)
                {
                    var trigger = Grid.LastMatchTrigger;

                    if (trigger is not null)
                        _tileMap.Swap(_tileMap[trigger.CoordsB4Swap],
                            _tileMap[trigger.CurrentCoords]);

                    wasSwappedBack = true;
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
                        //TODO: prepare nextlevel
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
                    UndoLastOperation();
                }
                
                enterGame = true;
            }
            
            EndDrawing();
        }
    }

}