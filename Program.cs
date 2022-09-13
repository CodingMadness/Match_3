using Raylib_cs;
using Match_3;

//INITIALIZATION:................................

class Program
{
    private static GameTime timer;
    private static Grid<Tile> _tileMap;
    private static ISet<Tile> _matchesOf3 = new HashSet<Tile>(3);
    private static Tile? secondClickedTile;
    private static bool isUndoPressed;
    private static HashSet<Tile> undoBuffer = new (5);
   
    public static int WindowWidth;
    public static int WindowHeight;
   
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
        timer = GameTime.GetTimer(30);
        GameTasks.SetQuest();
        _tileMap = new(14, 8, timer);
        WindowWidth = _tileMap.TileWidth * Grid<Tile>.TileSize;
        WindowHeight = _tileMap.TileHeight * Grid<Tile>.TileSize;
        Raylib.SetTargetFPS(60);
        Raylib.InitWindow(WindowWidth, WindowHeight, "Match3 By Alex und Shpend");
        AssetManager.Init();
    }

    private static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BEIGE);
            timer.UpdateTimerOnScreen();
            _tileMap.Draw();
            
            ////WORKS GOOD!:
            
            /*
             * if (timer.TimerDone())
             
            {
                Console.WriteLine("TIMER IS AT 3 SEC NOW!" + Raylib.GetFrameTime());
                timer.Reset();
            }*/

            ProcessSelectedTiles();
            UndoAllOperations();
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
            secondClickedTile = firstClickedTile as Tile; 
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
        
        /*Different tile selected => swap*/
        firstClickedTile.Selected = true;
        _tileMap.Swap(firstClickedTile, secondClickedTile);
        undoBuffer.Add(firstClickedTile as Tile);
        undoBuffer.Add(secondClickedTile);
        secondClickedTile.Selected = false;
        
        if (_tileMap.MatchInAnyDirection(secondClickedTile!.Cell, _matchesOf3))
        {
            undoBuffer.Clear();
            //Console.WriteLine("FOUND A MATCH-3");
            int tileCounter = 0;
            
            foreach (var match in _matchesOf3)
            {
                if (GameTasks.TryGetShapeKind(match.TileShape, out int toCollect))
                {
                    Console.WriteLine($"SHAPE:  {match.TileShape}   To collect: {toCollect}");
                        
                    if (++tileCounter == toCollect)
                    {
                        Console.WriteLine($"Nice, U COLLECTED.  {tileCounter}! Good job!");
                        GameTasks.RemoveQuest(match.TileShape);
                        tileCounter = 0;
                    }
                }
                //Console.WriteLine(match);
                _tileMap.Delete(match.Cell);
            }
        }
        _matchesOf3.Clear();
        secondClickedTile = null;        
        firstClickedTile.Selected = false;
    }

    private static void CleanUp()
    {
        Raylib.UnloadTexture(AssetManager.SpriteSheet);
        Raylib.CloseWindow();
    }
    
    private static void UndoAllOperations()
    {
        bool keyDown = (Raylib.IsKeyDown(KeyboardKey.KEY_A));

        //UNDO...!
        if (keyDown)
        {
            bool wasSwappedBack = false;

            foreach (Tile storedItem in undoBuffer)
            {
                //check if they have been ONLY swapped without leading to a match3
                if (!wasSwappedBack && _tileMap[storedItem.Cell] is not null)
                {
                    var secondTile = _tileMap[storedItem.Cell];
                    var firstTie = _tileMap[storedItem.CoordsB4Swap];
                    _tileMap.Swap(secondTile, firstTie);
                    wasSwappedBack = true;
                }
                else
                {
                    //their has been a match3 after swap!
                    //for delete we dont have a .IsDeleted, cause we onl NULL
                    //a tile at a certain coordinate, so we test for that
                    //if (_tileMap[storedItem.Cell] is { } backupItem)
                    var tmp = (_tileMap[storedItem.Cell] = storedItem) as Tile;
                    tmp!.Selected = false;
                    tmp.ChangeTo(Color.WHITE);
                }
                if (!wasSwappedBack)
                    if (Grid<Tile>.MatchXTrigger is { })
                        _tileMap.Swap(_tileMap[Grid<Tile>.MatchXTrigger.CoordsB4Swap],
                            _tileMap[Grid<Tile>.MatchXTrigger.Cell]);
            }
            undoBuffer.Clear();
        }
    }
}

    