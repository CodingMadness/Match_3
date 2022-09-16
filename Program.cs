using Match_3;
using Raylib_CsLo;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

//INITIALIZATION:................................

class Program
{
    private static GameState state;
    private static GameTime globalTimer, gameOverScreenTimer;
  
    private static Grid<Tile> _tileMap;
    private static readonly ISet<Tile> MatchesOf3 = new HashSet<Tile>(3);
    private static Tile? secondClickedTile;
    private static readonly ISet<Tile> UndoBuffer = new HashSet<Tile>(5);
    private static bool? wasGameWonB4Timeout = null;
    private static bool toggleGame = false;
    private static int tileCounter;

    private static void Main()
    {        
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        GameStateManager.SetNewLevl(5);
        state = GameStateManager.State;
       
        GameStateManager.SetCollectQuest();
        globalTimer = GameTime.GetTimer(state!.GameStartAt);
        gameOverScreenTimer = GameTime.GetTimer(state.GameOverScreenTime);
        _tileMap = new(state);
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(state.WINDOW_WIDTH, state.WINDOW_HEIGHT, "Match3 By Alex und Shpend");
        AssetManager.Init();
    }

    private static float ScaleText(string txt, float initialFontSize)
    {
        var fontSize = Raylib.MeasureTextEx(AssetManager.DebugFont, txt, initialFontSize, 1f);
        float scale = state.WINDOW_WIDTH / fontSize.X;
        return scale * (initialFontSize);
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

    private static bool ShowWelcomeScreenOnLoop(bool shallClearTxt)
    {
        string txt = "WELCOME PLAYER - ARE YOU READY TO GET ATLEAST HERE A MATCH IF NOT ON TINDER?";
        Raylib.DrawTextEx(AssetManager.WelcomeFont, txt,
                                    Int2.Zero, ScaleText(txt, 25),
                                    1.3f, Raylib.ColorAlpha(Raylib.BLUE, shallClearTxt ? 0f : 1f));
        return shallClearTxt;
    }

    private static bool OnGameOver(bool? gameWon)
    {
        if (gameWon is null)
        {
            return false;
        }

        var output = gameWon.Value ? "YOU WON!" : "YOU LOST";

        Int2 AlignMid(float fSize)
        {      
            var fontSize = Raylib.MeasureText(output, (int)fSize);
            Int2 diffToMoveLeft = state.Center - fontSize / 2;
            return diffToMoveLeft;
        }
        
        //Console.WriteLine(output);
        UpdateTimerOnScreen(ref gameOverScreenTimer);
        Raylib.ClearBackground(Raylib.WHITE);
        float fontSize = ScaleText(output, 10);
        Int2 aligned = AlignMid(fontSize);
        Raylib.DrawTextEx(AssetManager.WelcomeFont, output, aligned, fontSize, 1f, Raylib.RED);
        return gameOverScreenTimer.Done();
    }

    private static void DeleteMatchesForUndoBuffer(ISet<Tile> tiles) 
    {
        foreach (var match in tiles)
        {
            UndoBuffer.Add(_tileMap[match.Current]!);
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
                        return;
                }
                else if (wasGameWonB4Timeout == true)
                {
                    if (OnGameOver(true))
                    {
                        ///TODO: prepare nextlevel
                        //1. New Map!
                        GameStateManager.SetNewLevl(null);
                        //2. New Quest
                        GameStateManager.SetCollectQuest();
                        break;
                    }
                }
                else
                {
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
            secondClickedTile = firstClickedTile;
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

        if (_tileMap.MatchInAnyDirection(secondClickedTile!.Current, MatchesOf3))
        {
            UndoBuffer.Clear();

            if (GameStateManager.TryGetSubQuest(secondClickedTile.TileShape, out int toCollect))
            {
                tileCounter += 3;

                if (tileCounter >= toCollect)
                {
                    Console.WriteLine($"Good job, you got your {tileCounter} match3! by {secondClickedTile.TileShape.Kind}");
                    GameStateManager.RemoveSubQuest(secondClickedTile.TileShape);
                    tileCounter = 0;
                }
                wasGameWonB4Timeout = GameStateManager.IsQuestDone();
            }

            DeleteMatchesForUndoBuffer(MatchesOf3);
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
                    var tmp = (_tileMap[storedItem.Current] = storedItem);
                    tmp!.Selected = false;
                    tmp.ChangeTo(Raylib.WHITE);
                }

                if (!wasSwappedBack)
                {
                    var trigger = Grid<Tile>.MatchXTrigger;

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

