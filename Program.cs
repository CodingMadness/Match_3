using Match_3;
using Match_3.GameTypes;
using Raylib_CsLo;
using System.Numerics;

//INITIALIZATION:................................

class Program
{
    private static GameState state;
    private static GameTime globalTimer, gameOverScreenTimer;

    private static Grid _tileMap;
    private static readonly ISet<ITile> MatchesOf3 = new HashSet<ITile>(3);
    private static Tile? secondClickedTile;
    private static readonly ISet<ITile> UndoBuffer = new HashSet<ITile>(5);
    private static bool? wasGameWonB4Timeout = null;
    private static bool toggleGame = false;
    private static int tileCounter = 0;
    private static int missedSwapTolerance = 0;

    private static void Main()
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        GameStateManager.LoadLevel();
        state = GameStateManager.State;
        globalTimer = GameTime.GetTimer(state!.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(state.GameOverScreenTime);
        _tileMap = new(state);
        Raylib.SetTargetFPS(60);
        Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        Raylib.InitWindow(state.WINDOW_WIDTH, state.WINDOW_HEIGHT, "Match3 By Alex und Shpend");
        AssetManager.Init();
        //Console.Clear();        
    }
    
    public static void UpdateTimerOnScreen(ref GameTime timer)
    {
        if (timer.ElapsedSeconds <= timer.MAX_TIMER_VALUE / 2f) ;
           // timer.ElapsedSeconds -= Raylib.GetFrameTime() * 1.3f;

        timer.UpdateTimer();
        string txt = ((int)timer.ElapsedSeconds).ToString();

        Raylib.DrawTextEx(AssetManager.DebugFont,
            txt,
            state.TopCenter,
            75f/*ScaleText(txt, 1)*/, //problem is here!...
            1f,
            timer.ElapsedSeconds > 0f ? Raylib.RED : Raylib.BEIGE);
    }
    
    public static void DrawScaledFont(in GameText font)
    {       
        Raylib.DrawTextEx(font.Src, font.Text, font.Begin, font.Size, 1f, font.Color);
    }

    private static void ShowWelcomeScreenOnLoop(bool shallClearTxt)
    {
        string txt = "WELCOME YOUNG MATCH_3 ADDICT!";
        FadeableColor tmp = Raylib.RED;
        tmp.CurrentAlpha = shallClearTxt ? 0f : 1f;
        tmp.TargetAlpha = 1f;
        Vector2 aThirdOfScreen = new(state.WINDOW_WIDTH/4.5f, 15);         
        GameText welcomeFont = new(AssetManager.DebugFont, txt, aThirdOfScreen, 5, tmp);
        DrawScaledFont(welcomeFont.AlignText());        
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
        Raylib.ClearBackground(Raylib.WHITE);
        Vector2 windowSize = new(state.WINDOW_WIDTH/2, state.WINDOW_HEIGHT);
        GameText gameOverFont = new(AssetManager.DebugFont, output, windowSize, 3f, Raylib.RED);
        DrawScaledFont(gameOverFont);

        return gameOverScreenTimer.Done();
    }

    private static void RememberDeletedMatches(ISet<ITile> tiles)
    {
        foreach (var match in tiles)
        {
            if (match is null)
                continue;

            UndoBuffer.Add((Tile)_tileMap[match.GridCoords]!);
            _tileMap.Delete(match.GridCoords);
        }
    }

    private static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.WHITE);

            //render text on 60fps
            if (!toggleGame)
            {
                ShowWelcomeScreenOnLoop(false);
                GameStateManager.LogQuest(false);
            }
            if (Raylib.IsKeyDown(KeyboardKey.KEY_ENTER) || toggleGame)
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
                        /*
                        ///TODO: prepare nextlevel
                        //1. New Map!
                        GameStateManager.LoadLevel(null);
                        //2. New Quest
                        GameStateManager.SetCollectQuest();
                        break;
                        */
                    }
                }
                else
                {
                    //Keep the game Drawing!
                    UpdateTimerOnScreen(ref globalTimer);
                    ShowWelcomeScreenOnLoop(false);
                    _tileMap.Draw(globalTimer.ElapsedSeconds);
                    ProcessSelectedTiles();
                    UndoLastOperation();
                }
                toggleGame = true;
            }

            Raylib.EndDrawing();
        }
    }

    private static void ProcessSelectedTiles()
    {
        if (!_tileMap.TryGetClickedTile(out var firstClickedTile))
            return;

       //Console.WriteLine($"{nameof(firstClickedTile)} {firstClickedTile}");
        
        //No tile selected yet
        if (secondClickedTile is null)
        {
            secondClickedTile = (Tile)firstClickedTile;
            secondClickedTile.Selected = true;
            return;
        }

        //Same tile selected => deselect
        //WRONG LOGIC IN EQUALS!
        if (firstClickedTile.Equals(secondClickedTile))
        {
            secondClickedTile.Selected = false;
            secondClickedTile = null;
            return;
        }

        /*Different tile selected ==> swap*/
        firstClickedTile.Selected = true;
        _tileMap.Swap(firstClickedTile, secondClickedTile);
        UndoBuffer.Add(firstClickedTile);
        UndoBuffer.Add(secondClickedTile);
        secondClickedTile.Selected = false;

        var candy = secondClickedTile is not null ? 
                       secondClickedTile.TileShape as CandyShape : 
                         null;

        if (candy is null)
            return;
        TryMatchWithFirstTile:
        if (_tileMap.MatchInAnyDirection(secondClickedTile!, MatchesOf3))           
        {
            UndoBuffer.Clear();

            if (GameStateManager.TryGetSubQuest(candy, out int toCollect))
            {
                tileCounter += 1;

                if (tileCounter == toCollect)
                {
                    Console.WriteLine($"Good job, you got your {tileCounter} match3! by {candy.Ball}");
                    GameStateManager.RemoveSubQuest(candy);
                    tileCounter = 0;
                    missedSwapTolerance = 0;
                }
                wasGameWonB4Timeout = GameStateManager.IsQuestDone();
            }

            RememberDeletedMatches(MatchesOf3);
        }
        else
        {
            //if (++missedSwapTolerance == state.MaxAllowedSwpas)
            //{
            //    Console.WriteLine("UPSI! you needed to many swaps to get a match now enjoy the punishment of having to collect MORE THAN BEFORE");
            //    GameStateManager.ChangeSubQuest(candy, tileCounter + 3);
            //    missedSwapTolerance = 0;
            //}
            //if (MatchesOf3.Count == 0)
            //{
            //    secondClickedTile = (Tile)firstClickedTile;
            //    Console.WriteLine("calling GOTO in order to try it with the 1. tile");
            //    MatchesOf3.Add(null!); //this line we just do, in order to interrupt the GOTO call again!
            //    goto TryMatchWithFirstTile;
            //}
        }
        
        //Console.WriteLine(firstClickedTile);
        MatchesOf3.Clear();
        secondClickedTile = null;
        firstClickedTile.Selected = false;
    }

    private static void UndoLastOperation()
    {
        bool keyDown = (Raylib.IsKeyDown(KeyboardKey.KEY_A));

        //UNDO...!
        if (keyDown)
        {
            bool wasSwappedBack = false;

            foreach (Tile storedItem in UndoBuffer)
            {
                //check if they have been ONLY swapped without leading to a match3
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
        Raylib.UnloadTexture(AssetManager.SpriteSheet);
        Raylib.CloseWindow();
    }
}

