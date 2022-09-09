using Raylib_cs;
using System.Diagnostics;
using Match_3;

//INITIALIZATION:................................
Raylib.InitWindow((int) GridData.Window_Width, (int) GridData.Window_Height, "Hello World");
string net6Path = Environment.CurrentDirectory;
const string ProjectName = "Match_3";
int LastProjectNameOccurence = net6Path.LastIndexOf(ProjectName) + ProjectName.Length;
var fontPath = $"{net6Path.AsSpan(0, LastProjectNameOccurence)}/font3.ttf";
var tilePath = $"{net6Path.AsSpan(0, LastProjectNameOccurence)}/shapes.png";
Console.WriteLine(tilePath);
Texture2D tileSheet = Raylib.LoadTexture(tilePath.ToString());
string destroyedTilePath = net6Path.AsSpan(0, LastProjectNameOccurence).ToString() + "/destroyedTile";
Tile.FontPath = fontPath;
Stopwatch sw = Stopwatch.StartNew();

Tile?[,] FillGrid()
{
    Tile?[,] map = new Tile?[(int) GridData.TileCountInX, (int) GridData.TileCountInY];

    Tile.SpriteSheet = tileSheet;

    for (int x = 0; x < (int) GridData.TileCountInX; x++)
    {
        for (int y = 0; y < (int) GridData.TileCountInY; y++)
        {
            var tile = Tile.GetRandomTile();
            tile.Coords = (x, y);
            map[x, y] = tile;
        }
    }

    return map;
}

void DrawTilemap(in Tile?[,] map)
{
    var deltaTime = (float)sw.Elapsed.TotalSeconds;

    for (int x = 0; x < (int)GridData.Window_Width; x += 64)
    {
        for (int y = 0; y < (int)GridData.Window_Height; y += 64)
        {
            var currentTile = map[x / 64, y / 64];
            currentTile?.Draw(deltaTime);
        }
    }
    sw.Restart();
    //Console.WriteLine(++drawCall + "  DRAWCALLS!");
}

bool FindTilePosByMousePos(in Tile?[,] map, out (int x, int y) coord)
{
    coord = (-1, -1);

    if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
    {
        var mouseVec2 = Raylib.GetMousePosition();

        coord.x = (int)(mouseVec2.X / (float)GridData.TileWidth);
        coord.y = (int)(mouseVec2.Y / (float)GridData.TileWidth);

        if (coord.x >= (int)GridData.TileCountInX || coord.y >= (int)GridData.TileCountInY)
            return false;

            if (map[coord.x, coord.y] is not null)
            return true;

        coord = (-1, -1);
        return false;
    }

    return false;
}

void Swap2Tiles(Tile?[,] map, (int x, int y) aCoord, (int x, int y) bCoord)
{
    var tileA = map[aCoord.x, aCoord.y];
    var tileB = map[bCoord.x, bCoord.y];

    if (tileA is null || tileB is null)
        throw new Exception("Ein Swap ist null!!!");

    tileA.Coords = bCoord;
    map[bCoord.x, bCoord.y] = tileA;

    tileB.Coords = aCoord;
    map[aCoord.x, aCoord.y] = tileB;
}

bool AddWhenEqual(Tile first, Tile next, HashSet<Tile> rowOf3)
{
    if (first.Equals(next) && first.Coords.gridY == next.Coords.gridY)
    {
        rowOf3.Add(first);
        rowOf3.Add(next);
        return true;
    }

    return false;
}

bool Match3InRightDirection(Tile?[,] map, (int x, int y) clickedCoord, HashSet<Tile> rowOf3)
{
    if (rowOf3.Count == 3)
        return true;

    if (clickedCoord.x == (int)GridData.TileCountInX - 1)
        return Match3InLeftDirection(map, clickedCoord, rowOf3);

    Tile first = map[clickedCoord.x, clickedCoord.y]!;
    Tile next = map[clickedCoord.x+1, clickedCoord.y]!;

    return AddWhenEqual(first, next, rowOf3) && Match3InRightDirection(map, (clickedCoord.x + 1, clickedCoord.y), rowOf3);
}

bool Match3InLeftDirection(Tile?[,] map, (int x, int y) clickedCoord,  HashSet<Tile> rowOf3)
{
    if (rowOf3.Count == 3)
        return true;

    if (clickedCoord.x ==  0)
        return Match3InRightDirection(map, clickedCoord, rowOf3);

    Tile first = map[clickedCoord.x, clickedCoord.y]!;
    Tile next = map[clickedCoord.x-1, clickedCoord.y]!;

    return AddWhenEqual(first, next, rowOf3) && Match3InLeftDirection(map, (clickedCoord.x - 1, clickedCoord.y), rowOf3);
}

bool Match3InBetween(Tile?[,] map, (int x, int y) clickedCoord, HashSet<Tile> rowOf3)
{
    //Console.WriteLine("Match3InBetween() was CALLED");

    if (rowOf3.Count == 3)
        return true;

    if (clickedCoord.x == 0)
        return Match3InRightDirection(map, clickedCoord, rowOf3);
    
    if (clickedCoord.x == (int)GridData.TileCountInX)
        return Match3InLeftDirection(map, clickedCoord, rowOf3);

    //before we went 1 LEFT to check the LEFT neighbor
    //now we go 2 RIGHT to check the RIGHT neighbor
    int nextX = rowOf3.Count == 0 ? clickedCoord.x - 1 : clickedCoord.x + 1;

    Tile first = map[clickedCoord.x, clickedCoord.y]!;
    Tile next = map[nextX, clickedCoord.y]!;
    return AddWhenEqual(first, next, rowOf3) && Match3InBetween(map, (nextX, clickedCoord.y), rowOf3);
}

//CALLS!.............................
var map = FillGrid();

var clickedCoords = new (int x, int y)[2];

int swapCounter = 0;

HashSet<Tile> match3List = new(3);

while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();

    Raylib.ClearBackground(Color.BEIGE);

    DrawTilemap(map);

    (int x, int y) position;
    bool found = FindTilePosByMousePos(map, out position); 

    if (found)
    {
        //Console.WriteLine("I WAS CLICKED: !" + position);
        clickedCoords[swapCounter] = position;        
        map[position.x, position.y]!.Selected = true;
        swapCounter++;

        if (swapCounter == 2)
        {
            Swap2Tiles(map, clickedCoords[0], clickedCoords[1]);
            var aTile = map[clickedCoords[0].x, clickedCoords[0].y];
            var bTile = map[clickedCoords[1].x, clickedCoords[1].y];
            bTile!.Selected = false;
            aTile!.Selected = false;
            clickedCoords.AsSpan().Clear();
            swapCounter = 0;

            int moveCounter = 1;
            //DOES NOT WORK YET! INVESTIGATE!
            
            if (Match3InRightDirection(map, bTile.Coords, match3List))
            {
                Console.WriteLine("successfully deleted from LEFT-TO-RIGHT");

                foreach (var item in match3List)
                {
                    Console.WriteLine(item);
                    item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                    item.Selected = true;
                };
            }
            
            if (Match3InLeftDirection(map, bTile.Coords, match3List))
            {
                Console.WriteLine("successfully deleted from RIGHT-TO-LEFT");

                foreach (var item in match3List)
                {
                    Console.WriteLine(item);
                    item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                    item.Selected = true;
                };
            }
            
            else if (Match3InBetween(map, bTile.Coords, match3List))
            {
                Console.WriteLine("successfully deleted from INBETWEEN");

                foreach (var item in match3List)
                {
                    Console.WriteLine(item);
                    item!.Colour = Raylib.ColorAlpha(item.Colour, 0f);
                    item.Selected = true;
                };
            }
            
            match3List.Clear();
        }
    }

    Raylib.EndDrawing();
}

Raylib.UnloadTexture(tileSheet);

Raylib.CloseWindow();