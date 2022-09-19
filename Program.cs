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
    private static int tileCounter;
    private static int missedSwapTolerance = 0;


    private static void Main()
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        GameStateManager.SetNewLevl();
        state = GameStateManager.State;

        GameStateManager.SetCollectQuest();
        globalTimer = GameTime.GetTimer(state!.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(state.GameOverScreenTime);
        _tileMap = new(state);
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(state.WINDOW_WIDTH, state.WINDOW_HEIGHT, "Match3 By Alex und Shpend");
        AssetManager.Init();
        Console.Clear();
    }

    
    public static void UpdateTimerOnScreen(ref GameTime timer)
    {
        timer.UpdateTimer();
        string txt = ((int)timer.ElapsedSeconds).ToString();

        Raylib.DrawTextEx(AssetManager.DebugFont,
            txt,
            state.TopCenter,
            75f/*ScaleText(txt, 1)*/, //problem is here!...
            1f,
            timer.ElapsedSeconds > 0f ? Raylib.RED : Raylib.BEIGE);
    }
    
    private static void DrawScaledFont(in AdaptableFont font)
    {
        var scaled = font.CenterText();
        Raylib.DrawTextEx(scaled.Src, scaled.Text, scaled.Begin, scaled.Size, 1f, font.Color);
    }

    private static bool ShowWelcomeScreenOnLoop(bool shallClearTxt)
    {
        string txt = "WELCOME";
        FadeableColor tmp = Raylib.RED;
        tmp.CurrentAlpha = shallClearTxt ? 0f : 1f;
        tmp.TargetAlpha = 1f;
        AdaptableFont welcomeFont = new(AssetManager.DebugFont, txt, new(state.Center.X, 60), 10, tmp);
        DrawScaledFont(welcomeFont.ScaleText());        
        return shallClearTxt;
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
        Vector2 windowSize = new(state.WINDOW_WIDTH, state.WINDOW_HEIGHT);
        AdaptableFont gameOverFont = new(AssetManager.DebugFont, output, windowSize, 3f, Raylib.RED);
        DrawScaledFont(gameOverFont);

        return gameOverScreenTimer.Done();
    }

    private static void DeleteMatchesForUndoBuffer(ISet<Tile> tiles)
    {
        foreach (var match in tiles)
        {
            UndoBuffer.Add((Tile)_tileMap[match.Current]!);
            _tileMap.Delete(match.Current);
        }
    }

    private static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.BEIGE);

            //render text on 60fps
            if (!toggleGame)
                ShowWelcomeScreenOnLoop(false);

            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) || toggleGame)
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
                        GameStateManager.SetNewLevl(null);
                        //2. New Quest
                        GameStateManager.SetCollectQuest();
                        break;
                        */
                    }
                }
                else
                {
                    //Keep the game running!
                    UpdateTimerOnScreen(ref globalTimer);
                    ShowWelcomeScreenOnLoop(true);
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

        //No tile selected yet
        if (secondClickedTile is null)
        {
            secondClickedTile = (Tile)firstClickedTile;
            secondClickedTile.Selected = true;
            return;
        }

        //Same tile selected => deselect
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

        var candy = secondClickedTile is not null ? secondClickedTile.TileShape as Candy : null;

        if (candy is null)
            return;

        if (_tileMap.MatchInAnyDirection(secondClickedTile!.Current, MatchesOf3))
        {
            UndoBuffer.Clear();
            //GameStateManager.DoSomeChecks(secondClickedTile.TileShape);          

            if (GameStateManager.TryGetSubQuest(candy, out int toCollect))
            {
                tileCounter += 3;

                if (tileCounter >= toCollect)
                {
                    Console.WriteLine($"Good job, you got your {tileCounter} match3! by {candy.Sweet}");
                    GameStateManager.RemoveSubQuest(candy);
                    tileCounter = 0;
                    missedSwapTolerance = 0;
                }
                wasGameWonB4Timeout = GameStateManager.IsQuestDone();
            }

            DeleteMatchesForUndoBuffer((ISet<Tile>)MatchesOf3);
        }

        else
        {
            if (++missedSwapTolerance == state.MaxAllowedSwpas) 
            {
                Console.WriteLine("UPSI! you needed to many swaps to get a match now enjoy the punishment of having to collect MORE THAN BEFORE");
                GameStateManager.ChangeSubQuest(candy, tileCounter+3);
                missedSwapTolerance = 0;
            }
        }
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
                if (!wasSwappedBack && _tileMap[storedItem.Current] is not null)
                {
                    var secondTile = _tileMap[storedItem.Current];
                    var firstTie = _tileMap[storedItem.CoordsB4Swap];
                    _tileMap.Swap(secondTile, firstTie);
                    wasSwappedBack = true;
                }
                else
                {
                    //their has been a match3 after swap!
                    //for delete we dont have a .IsDeleted, cause we onl NULL
                    //a tile at a certain coordinate, so we test for that
                    //if (_tileMap[storedItem.Current] is { } backupItem)
                    var tmp = (_tileMap[storedItem.Current] = storedItem) as Tile;
                    tmp!.Selected = false;
                    tmp.ChangeTo(Raylib.WHITE);
                }

                if (!wasSwappedBack)
                {
                    var trigger = Grid.MatchXTrigger;

                    if (trigger is not null)
                        _tileMap.Swap(_tileMap[trigger.CoordsB4Swap],
                                       _tileMap[trigger.Current]);

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

