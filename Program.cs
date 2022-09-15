using Raylib_cs;
using Match_3;

//INITIALIZATION:................................

class Program
{
    private static GameTime timer;
    private static Grid<Tile> _tileMap;
    private static readonly ISet<Tile> MatchesOf3 = new HashSet<Tile>(3);
    private static Tile? secondClickedTile;
    private static bool isUndoPressed;
    private static readonly HashSet<Tile> UndoBuffer = new(5);
    private static bool isGameOver = false;

    public static int WindowWidth;
    public static int WindowHeight;

    private delegate void GameOverHandler(bool isDone);
    private static event GameOverHandler CheckForGameOverEvent;

    private static void Main(string[] args)
    {
        //Now I wanna give the player the task to collect
        //X-Reds, Y-Blues, Z-Greens
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        timer = GameTime.GetTimer(15);
        GameTasks.SetQuest();
        GameTasks.LogQuest();
        _tileMap = new(14, 8, timer);
        WindowWidth = _tileMap.TileWidth * Grid<Tile>.TileSize;
        WindowHeight = _tileMap.TileHeight * Grid<Tile>.TileSize;
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(WindowWidth, WindowHeight, "Match3 By Alex und Shpend");
        AssetManager.Init();
        CheckForGameOverEvent += CheckForGameOver;
    }

    private static void CheckForGameOver(bool isDone)
    {
        ShowResultAfterGame(isDone);
    }

    private static void ShowResultAfterGame(bool gameWon)
    {
        var output = gameWon ? "YOU WON!" : "YOU LOST";
        //Console.WriteLine(output);
        Raylib.ClearBackground(Color.WHITE);
        Raylib.DrawText(output, (WindowWidth / 2)-(WindowWidth/4), (WindowHeight/2) +50, 80, Color.RED);
    }

    private static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BEIGE);
            timer.UpdateTimerOnScreen();

            if (isGameOver)
            {
                ShowResultAfterGame(true);
                return;
            }          
            _tileMap.Draw(timer.ElapsedSeconds);

            ProcessSelectedTiles();
            UndoLastOperation();

            if (timer.Done())
            {
                ShowResultAfterGame(GameTasks.IsQuestDone());
                isGameOver = true;
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
            //Console.WriteLine("FOUND A MATCH-3");
            int tileCounter = 0;

            foreach (var match in MatchesOf3)
            {
                if (GameTasks.TryGetSubQuest(match.TileShape, out int toCollect))
                {
                    if (++tileCounter == toCollect)
                    {
                        Console.WriteLine($"Good job, you got your {tileCounter} match3! by {match.TileShape.Kind}");
                        GameTasks.RemoveSubQuest(match.TileShape);
                        tileCounter = 0;
                    }
                    //Console.WriteLine($"You sill have to collect: {toCollect- tileCounter}");
                    if (GameTasks.IsQuestDone())
                    {
                        isGameOver = true;
                        return;
                        //ShowResultAfterGame(isGameOver);
                    }
                }

                UndoBuffer.Add(_tileMap[match.Current]);
                _tileMap.Delete(match.Current);               
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
                    var tmp = (_tileMap[storedItem.Current] = storedItem);
                    tmp!.Selected = false;
                    tmp.ChangeTo(Color.WHITE);
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

