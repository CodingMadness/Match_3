using Raylib_cs;
using System.Diagnostics;
using Match_3;

//INITIALIZATION:................................

class Program
{
    public static Texture2D TileSheet { get; private set; }
    private static Stopwatch _stopwatch = new();
    private static TileMap _tileMap = new(8, 8);

    private const int _tileSize = 64;
    private const int _tileCountX = 8;
    private const int _tileCountY = 8;

    public static readonly Int2 WindowSize = new Int2(_tileCountX, _tileCountY) * _tileSize;
    public static readonly Int2 TileSize = new Int2(_tileSize);

    public static void Main(string[] args)
    {
        Initialize();
        GameLoop();
        CleanUp();
    }

    public static void Initialize()
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
    }

    public static void GameLoop()
    {
        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.BEIGE);

            ProcessSelectedTiles();
            
            _stopwatch.Stop();
            _tileMap.Draw((float) _stopwatch.Elapsed.TotalSeconds);
            _stopwatch.Restart();


            Raylib.EndDrawing();
        }
    }

    private static Tile? _selectedTile;

    private static void ProcessSelectedTiles()
    {
        if (!_tileMap.TryGetClickedTile(out var foundTile))
            return;

        //No tile selected yet
        if (_selectedTile is null)
        {
            _selectedTile = foundTile;

            _selectedTile.Selected = true;
            return;
        }

        //Same tile selected => deselect
        if (foundTile.Equals(_selectedTile))
        {
            _selectedTile.Selected = false;
            _selectedTile = null;
            return;
        }

        //Different tile selected => swap
        foundTile.Selected = true;
        _tileMap.Swap(_selectedTile, foundTile);
        _selectedTile.Selected = false;
        _selectedTile = null;
        foundTile.Selected = false;
    }

    public static void CleanUp()
    {
        Raylib.UnloadTexture(TileSheet);

        Raylib.CloseWindow();
    }

    static bool AddWhenEqual(Tile first, Tile next, HashSet<Tile> rowOf3)
    {
        if (first.Equals(next) && first.Coords.Y == next.Coords.Y)
        {
            rowOf3.Add(first);
            rowOf3.Add(next);
            return true;
        }

        return false;
    }

    static bool MatchInDirection(Tile?[,] map, Int2 clickedCoord, Int2 direction, out int count)
    {
        count = 0;
        var originTile = map[clickedCoord.X, clickedCoord.Y];

        if (originTile is null)
            return false;

        while (true)
        {
            var compareTile = map[clickedCoord.X + direction.X * count,
                clickedCoord.Y + direction.Y * count];
            if (!originTile.Equals(compareTile))
            {
                break;
            }

            count++;
        }

        return count >= 3;
    }

    static bool Match3InRightDirection(Tile?[,] map, (int x, int y) clickedCoord, HashSet<Tile> rowOf3)
    {
        if (rowOf3.Count == 3)
            return true;

        if (clickedCoord.x == _tileCountX - 1)
            return Match3InLeftDirection(map, clickedCoord, rowOf3);

        Tile first = map[clickedCoord.x, clickedCoord.y]!;
        Tile next = map[clickedCoord.x + 1, clickedCoord.y]!;

        return AddWhenEqual(first, next, rowOf3) &&
               Match3InRightDirection(map, (clickedCoord.x + 1, clickedCoord.y), rowOf3);
    }

    static bool Match3InLeftDirection(Tile?[,] map, (int x, int y) clickedCoord, HashSet<Tile> rowOf3)
    {
        if (rowOf3.Count == 3)
            return true;

        if (clickedCoord.x == 0)
            return Match3InRightDirection(map, clickedCoord, rowOf3);

        Tile first = map[clickedCoord.x, clickedCoord.y]!;
        Tile next = map[clickedCoord.x - 1, clickedCoord.y]!;

        return AddWhenEqual(first, next, rowOf3) &&
               Match3InLeftDirection(map, (clickedCoord.x - 1, clickedCoord.y), rowOf3);
    }

    static bool Match3InBetween(Tile?[,] map, (int x, int y) clickedCoord, HashSet<Tile> rowOf3)
    {
        //Console.WriteLine("Match3InBetween() was CALLED");

        if (rowOf3.Count == 3)
            return true;

        if (clickedCoord.x == 0)
            return Match3InRightDirection(map, clickedCoord, rowOf3);

        if (clickedCoord.x == _tileCountX)
            return Match3InLeftDirection(map, clickedCoord, rowOf3);

        //before we went 1 LEFT to check the LEFT neighbor
        //now we go 2 RIGHT to check the RIGHT neighbor
        int nextX = rowOf3.Count == 0 ? clickedCoord.x - 1 : clickedCoord.x + 1;

        Tile first = map[clickedCoord.x, clickedCoord.y]!;
        Tile next = map[nextX, clickedCoord.y]!;
        return AddWhenEqual(first, next, rowOf3) && Match3InBetween(map, (nextX, clickedCoord.y), rowOf3);
    }
}