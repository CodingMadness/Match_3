using Raylib_cs;
using Match_3;

//INITIALIZATION:................................

class Program
{
    private static GameTime timer;
    private static Grid<Tile> _tileMap;
    private static ISet<Tile> _matches = new HashSet<Tile>(3);
    private static Tile? secondClickedTile;
    private static bool isUndoPressed;
    private static HashSet<Tile> undoBuffer = new (5);
   
    public static int WindowWidth;
    public static int WindowHeight;
   
    private static void Main(string[] args)
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        timer = GameTime.GetTimer(3);
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
            timer.UpdateTimer();
            _tileMap.Draw();
            
           // _tileMap.Iterate();
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
        
        //Different tile selected => swap
        firstClickedTile.Selected = true;
        _tileMap.Swap(firstClickedTile, secondClickedTile);
        undoBuffer.Add(firstClickedTile as Tile);
        undoBuffer.Add(secondClickedTile);
        secondClickedTile.Selected = false;
        
        if (_tileMap.MatchInAnyDirection(secondClickedTile!.Cell, _matches))
        {
            undoBuffer.Clear();
            //Console.WriteLine("FOUND A MATCH-3");
            
            foreach (var match in _matches)
            {
                undoBuffer.Add(_tileMap[match.Cell] as Tile); 
                _tileMap[match.Cell] = null;
                //Console.WriteLine(match);
            }
        }
        
        _matches.Clear();
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
                    _tileMap.Swap(_tileMap[Grid<Tile>.MatchXTrigger.CoordsB4Swap], 
                        _tileMap[Grid<Tile>.MatchXTrigger.Cell]);
            }
            undoBuffer.Clear();
        }
    }
}

    