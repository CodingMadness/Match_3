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
        var clickedTiles = new Tile[2];

        int swapCounter = 0;

        HashSet<Tile> match3List = new(3);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.BEIGE);

            _stopwatch.Stop();
            _tileMap.Draw((float) _stopwatch.Elapsed.TotalSeconds);
            _stopwatch.Restart();

            if (_tileMap.TryGetClickedTile(out var foundTile))
            {
                //Console.WriteLine("I WAS CLICKED: !" + position);
                clickedTiles[swapCounter] = foundTile;
                //foundTile.Selected = true;
                swapCounter++;

                if (swapCounter == 2)
                {
                    swapCounter = 0;
                    
                    if (!clickedTiles[0].Equals(clickedTiles[1]))
                    {
                        foundTile.Selected = true;
                        _tileMap.Swap(clickedTiles[0], clickedTiles[1]);
                        clickedTiles[0].Selected = false;
                        clickedTiles[1].Selected = false;
                        clickedTiles.AsSpan().Clear();
                    }
                    else
                    {
                        clickedTiles[0].StopFading();
                        clickedTiles[1].StopFading();
                    }

                    /*
                     if (Match3InRightDirection(map, bTile.Coords, match3List))
                    {
                        Console.WriteLine("successfully deleted from LEFT-TO-RIGHT");

                        foreach (var item in match3List)
                        {
                            Console.WriteLine(item);
                            item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                            item.Selected = true;
                        }
                        ;
                    }

                    if (Match3InLeftDirection(map, bTile.Coords, match3List))
                    {
                        Console.WriteLine("successfully deleted from RIGHT-TO-LEFT");

                        foreach (var item in match3List)
                        {
                            Console.WriteLine(item);
                            item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                            item.Selected = true;
                        }

                        ;
                    }

                    else if (Match3InBetween(map, bTile.Coords, match3List))
                    {
                        Console.WriteLine("successfully deleted from INBETWEEN");

                        foreach (var item in match3List)
                        {
                            Console.WriteLine(item);
                            item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                            item.Selected = true;
                        }

                        ;
                    }

                    match3List.Clear();*/
                }
            }

            Raylib.EndDrawing();
        }
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