using Raylib_cs;
using System.Diagnostics;
using System.Numerics;
using Match_3;

//INITIALIZATION:................................

class Program
{
    public static Texture2D TileSheet { get; private set; }
    private static Stopwatch _stopwatch = new();
    private static TileMap _tileMap = new(8, 8);
    private static readonly HashSet<Tile> _matches = new(3);
    
    private const int _tileSize = 64;
    private const int _tileCountX = 8;
    private const int _tileCountY = 8;

    public static readonly IntVector2 WindowSize = new IntVector2(_tileCountX, _tileCountY) * _tileSize;
    public static readonly IntVector2 TileSize = new IntVector2(_tileSize);

    private static Tile? secondClickedTile;
    private static Direction _startDirection = Direction.PositiveX;

    private static bool isUndoPressed;
    private static HashSet<Tile> undoBuffer = new (5);
    private static void Main(string[] args)
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        Raylib.InitWindow(WindowSize.X, WindowSize.Y, "Match3 By Alex und Shpend");
        string net6Path = Environment.CurrentDirectory;
        const string projectName = "Match3";
        int lastProjectNameOccurence = net6Path.LastIndexOf(projectName) + projectName.Length;
        var fontPath = $"{net6Path.AsSpan(0, lastProjectNameOccurence)}/Assets/font3.ttf";
        var tilePath = $"{net6Path.AsSpan(0, lastProjectNameOccurence)}/Assets/shapes.png";
        Console.WriteLine(tilePath);
        TileSheet = Raylib.LoadTexture(tilePath);
        Tile.FontPath = fontPath;
        _stopwatch = Stopwatch.StartNew();
        Raylib.SetTargetFPS(60);
    }

    private static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BEIGE);
            _stopwatch.Stop();
            _tileMap.Draw((float)_stopwatch.Elapsed.TotalSeconds);
            Raylib.DrawFPS(0,0);
            _stopwatch.Restart();
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
        
        //Different tile selected => swap
        firstClickedTile.Selected = true;
        _tileMap.Swap(firstClickedTile, secondClickedTile);
        undoBuffer.Add(firstClickedTile);
        undoBuffer.Add(secondClickedTile);
        secondClickedTile.Selected = false;
        
        if (Match3InAnyDirection(_tileMap, secondClickedTile!.CurrentCoords, _matches))
        {
            undoBuffer.Clear();
            Console.WriteLine("FOUND A MATCH-3");
            
            foreach (var match in _matches)
            {
                undoBuffer.Add(_tileMap[match.CurrentCoords]); 
                _tileMap[match.CurrentCoords] = null;
                Console.WriteLine(match);
            }
            //PrintWhereTilesAreNull();

            Console.WriteLine(_matches.Count);
            Console.WriteLine();
        }
        _matches.Clear();
        secondClickedTile = null;        
        firstClickedTile.Selected = false;
    }

    private static void CleanUp()
    {
        Raylib.UnloadTexture(TileSheet);
        Raylib.CloseWindow();
    }
 
    static bool Match3InAnyDirection(TileMap map, IntVector2 clickedCoord, HashSet<Tile> rowOf3)
    {
        static bool AddWhenEqual(Tile? first, Tile? next, Direction direction, HashSet<Tile> rowOf3)
        {
            if (first is not null &&
                next is not null &&
                first.Equals(next))
            {
                rowOf3.Add(first);
                rowOf3.Add(next);
                return true;
            }

            return false;
        }
    
        static IntVector2 GetStepsFromDirection(IntVector2 input, Direction direction)
        {
            var tmp = direction switch
            {
                Direction.NegativeX => new IntVector2(input.X - 1, input.Y),
                Direction.PositiveX => new IntVector2(input.X + 1, input.Y),
                Direction.NegativeY => new IntVector2(input.X, input.Y - 1),
                Direction.PositiveY => new IntVector2(input.X, input.Y + 1),
                _ => IntVector2.Zero
            };

            return tmp;
        }

        const Direction lastDir = (Direction)4;

        Tile? first = map[clickedCoord];
        
        for (Direction i = 0; i < lastDir ; i++)
        {
            IntVector2 nextCoords = GetStepsFromDirection(clickedCoord, i);
            Tile? next = map[nextCoords];
            
            while (AddWhenEqual(first, next, i, rowOf3) && rowOf3.Count < 3)
            {
                nextCoords = GetStepsFromDirection(nextCoords, i);
                next = map[nextCoords];
            }
            
            if (rowOf3.Count < 3)
                rowOf3.Clear();
        }
        return rowOf3.Count >= 3;
    }
    
    private static void UndoAllOperations()
    {
        bool keyDown = (Raylib.IsKeyDown(KeyboardKey.KEY_A));

        //UNDO...!
        if (keyDown)
        {
            bool wasSwappedBack = false;
            int i = 0;

            foreach (var storedItem in undoBuffer)
            {
                /*if (i++ == 0)
                    continue;*/
                
                //check if they have been ONLY swapped without leading to a 
                //match3
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
                    var tmp = _tileMap[storedItem.CurrentCoords] = storedItem;
                    tmp!.Selected = false;
                    tmp.Colour = Color.WHITE;
                }
            }
            undoBuffer.Clear();
        }
    }
}

    