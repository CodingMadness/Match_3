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
    private static GameText welcomeText;
    
    private static void Main()
    {
        Initialize();
        GameLoop();
        CleanUp();
    }
    
    private static void Initialize()
    {
        GameRuleManager.DefineNewLevel();
        state = GameRuleManager.State;
        globalTimer = GameTime.GetTimer(state.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(state.GameOverScreenTime);
        _tileMap = new(state);
        SetTargetFPS(60);
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow(state.WINDOW_WIDTH, state.WINDOW_HEIGHT, "Match3 By Shpendicus");
        Vector2 pos = new(state.Center.X * 0.25f, state.Center.Y);
        AssetManager.Init(pos);
        welcomeText = new(AssetManager.WelcomeFont, "Welcome young man!!", 10f);
        //Console.Clear();        
    }
    
    public static void UpdateTimerOnScreen(ref GameTime timer)
    {
        if (timer.ElapsedSeconds <= timer.MAX_TIMER_VALUE / 2f)
        {
            // timer.ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;
        }
        timer.UpdateTimer();
        string txt = ((int)timer.ElapsedSeconds).ToString();
        
        DrawTextEx(AssetManager.WelcomeFont,
            txt,
            state.TopCenter,
            75f,
            1f,
            timer.ElapsedSeconds > 0f ? Raylib.RED : Raylib.BEIGE);
    }
    
    private static void ShowWelcomeScreenOnLoop(bool shallClearTxt)
    {
        FadeableColor tmp = Raylib.RED;
        tmp.CurrentAlpha = shallClearTxt ? 0f : 1f;
        tmp.TargetAlpha = 1f;
        
        welcomeText.ScaleText();
        welcomeText.AlignText();
        welcomeText.Draw(tmp);
    }

    private static bool OnGameOver(bool? gameWon)
    {
        if (gameWon is null)
        {
            return false;
        }

        var output = gameWon.Value ? "YOU WON!" : "YOU LOST";
        
        //Console.WriteLine(output);
        UpdateTimerOnScreen(ref gameOverScreenTimer);
        ClearBackground(Raylib.WHITE);
        Vector2 windowSize = new Vector2(state.WINDOW_WIDTH, state.WINDOW_HEIGHT) * 0.5f;
        //GameText gameOverText = new(AssetManager.WelcomeFont, output, windowSize, 3f, Raylib.RED);
        //gameOverText.AlignText();
        //gameOverText.Draw();
        return gameOverScreenTimer.Done();
    }

    private static void SaveDeletedMatches(IEnumerable<ITile?> tiles)
    {
        foreach (ITile? match in tiles)
        {
            if (match is not null)
            {
                UndoBuffer.Add(_tileMap[match.GridCoords]);
                _tileMap.Delete(match.GridCoords);
            }
        }
    }

    private static void GameLoop()
    {
        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(Raylib.WHITE);

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
                        Initialize();
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

    private static void ProcessSelectedTiles()
    {
        if (!_tileMap.TryGetClickedTile(out ITile? firstClickedTile) || firstClickedTile is null)
            return;

        //Console.WriteLine($"{nameof(firstClickedTile)} {firstClickedTile}");
        
        //No tile selected yet
        if (secondClickedTile is null)
        {
            secondClickedTile = firstClickedTile;
            secondClickedTile!.Selected = true;
            return;
        }

        //Same tile selected => deselect
        //WRONG LOGIC IN EQUALS!
        //if (firstClickedTile is not null)
        {
            if (firstClickedTile.Equals(secondClickedTile))
            {
                secondClickedTile.Selected = false;
                secondClickedTile = null;
                return;
            }
            else
            {
                /*Different tile selected ==> swap*/
                firstClickedTile.Selected = true;
                _tileMap.Swap(firstClickedTile, secondClickedTile);
                UndoBuffer.Add(firstClickedTile);
                UndoBuffer.Add(secondClickedTile);
                secondClickedTile.Selected = false;
            }
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
                Console.WriteLine($"You already got {tileCounter} match of Balltype: {candy.Ball}");
              
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

            foreach (var storedItem in UndoBuffer)
            {
                //check if they have been ONLY swapped without leading to a match3
                if (storedItem is not null)
                {
                    _tileMap[storedItem.GridCoords] = null!;
                    if (!wasSwappedBack && _tileMap[storedItem.GridCoords] is not null)
                    {
                        var secondTile = _tileMap[storedItem.GridCoords];
                        var firstTie = _tileMap[storedItem.CoordsB4Swap];
                        _tileMap.Swap(secondTile, firstTie);
                        wasSwappedBack = true;
                    }
                    else
                    {
                        //their has been a match3 after swap!
                        //for delete we dont have a .IsDeleted, cause we onl NULL
                        //a tile at a certain coordinate, so we test for that
                        //if (_tileMap[storedItem.GridCoords] is { } backupItem)
                        var tmp = (_tileMap[storedItem.GridCoords] = storedItem) as Tile;
                        tmp!.Selected = false;
                        tmp.ChangeTo(Raylib.WHITE);
                    }
                }

                if (!wasSwappedBack)
                {
                    var trigger = Grid.MatchXTrigger;

                    if (trigger is not null)
                        _tileMap.Swap(_tileMap[trigger.CoordsB4Swap],
                            _tileMap[trigger.GridCoords]);

                    wasSwappedBack = true;
                }
            }
            UndoBuffer.Clear();
        }
    }

    private static void CleanUp()
    {
        UnloadTexture(AssetManager.SpriteSheet);
        CloseWindow();
    }
}