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

    private static Tile? swappyTile;
    private static Direction _startDirection = Direction.PositiveX;

    private static void Main(string[] args)
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    private static void Initialize()
    {
        Raylib.InitWindow(WindowSize.X, WindowSize.Y, "Hello World");
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
            Raylib.EndDrawing();
        }
    }

    private static void ProcessSelectedTiles()
    {
        if (!_tileMap.TryGetClickedTile(out var clickedTile))
            return;

        //No tile selected yet
        if (swappyTile is null)
        {
            swappyTile = clickedTile;
            swappyTile.Selected = true;
            return;
        }

        //Same tile selected => deselect
        if (clickedTile.Equals(swappyTile))
        {
            swappyTile.Selected = false;
            swappyTile = null;
            return;
        }

        //Different tile selected => swap
        clickedTile.Selected = true;
        _tileMap.Swap(swappyTile, clickedTile);
        swappyTile.Selected = false;

        if (Match3InAnyDirection(_tileMap, swappyTile!.CurrentCoords, _matches, _startDirection))
        {
            Console.WriteLine("FOUND A MATCH-3");
            foreach (var match in _matches)
            {
                match.Selected = true;
                Console.WriteLine(match);
            }
            Console.WriteLine();
        }
        _matches.Clear();
        swappyTile = null;        
        clickedTile.Selected = false;
    }

    private static void CleanUp()
    {
        Raylib.UnloadTexture(TileSheet);

        Raylib.CloseWindow();
    }
 
    static bool Match3InAnyDirection(TileMap map, IntVector2 clickedCoord, HashSet<Tile> rowOf3, Direction startDirection)
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
            IntVector2 tmp = IntVector2.Zero;

            if (direction == Direction.NegativeX)
                tmp = new IntVector2(input.X - 1, input.Y);

            if (direction == Direction.PositiveX)
                tmp = new IntVector2(input.X + 1, input.Y);
        
            if (direction == Direction.NegativeY)
                tmp = new IntVector2(input.X, input.Y-1);
        
            if (direction == Direction.PositiveY)
                tmp = new IntVector2(input.X, input.Y + 1);

            return tmp;
        }

        Tile? first = map[clickedCoord];
        IntVector2 nextCoords = GetStepsFromDirection(clickedCoord, startDirection);
        Tile? next = map[nextCoords];
        bool isNext = true;
        
        while (isNext)
        {
            if (AddWhenEqual(first, next, startDirection, rowOf3))
            {
                nextCoords = GetStepsFromDirection(nextCoords, startDirection);
                next = map[nextCoords];
                isNext = rowOf3.Count < 3;
            }
            else
            {
                isNext = false;
            }
        }

        if (rowOf3.Count >= 3)
            return true;
        
        rowOf3.Clear();
            
        if ((int)startDirection == 3)
            return false;
            
        return Match3InAnyDirection(map, clickedCoord, rowOf3, ++startDirection);
    }
}

    