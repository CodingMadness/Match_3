using System.Numerics;
using Raylib_cs;

namespace Match_3;

public enum Shape : sbyte
{
    Ball,
    Cube,
    Zylinder,
    Triangle,
    EMPTY = -1
}

public enum OriginalColor : byte
{
    Blue,
    Yellow,
    Red,
    Green
}

public interface ITile : IEquatable<ITile>
{
    public Int2 Cell { get; set; }
    public Int2 CoordsB4Swap { get; set; }
    public int Size { get; }
    public  bool Selected { get; set; }
    public void Draw(float? elapsedTime);
}

public sealed class Tile :  ITile
{
    private bool wasDrawn;
    
    public Color Blur { get; set; }
    public OriginalColor OriginColor { get; set; }
    public Rectangle DrawDestination { get; set; }
    public Int2 Cell { get; set; }
   
    public Int2 SwappedCell { get; set; }
    public int Size => 64;
    public Int2 CoordsB4Swap { get; set; }
    public static string FontPath { get; set; }
    public bool Swapped { get; set; } = true;
    private Shape Shape { get; set; } = Shape.EMPTY;

    private static Font font;

    private bool selected;

    public bool Selected
    {
        get => selected;

        set
        {
            if (!value)
            {
                targetAlpha = 1;
            }
            else
            {
                targetAlpha = CurrentAlpha = 0f;
            }

            selected = value;
        }
    }

    private float targetAlpha = 1;
    public float CurrentAlpha { get; set; } = 1;
    private const float alphaSpeed = 2f;

    private Tile()
    {
       
    }

    private static Tile CreateNewTile(Rectangle drawDestinationRectangle, OriginalColor originCol, Int2 coord,
        bool swapped, Shape kind, Color color)
    {
        var mapTile = new Tile
        {
            DrawDestination =
                drawDestinationRectangle, // this one was calculated from the current map position and the descriptors map offset values.
            Swapped = swapped,
            Shape = kind,
            Blur = color,
            OriginColor = originCol,
            Cell = coord
        };

        return mapTile;
    }

    private static readonly Rectangle[] frames = {
        new Rectangle(0f, 0f, 64f, 64f), //blue
        new Rectangle(64f, 0f, 64f, 64f), //yellow
        new Rectangle(0f, 64f, 64f, 64f), //red
        new Rectangle(64f, 64f, 64f, 64f), //green
    };
    
    private static Shape IdentifyTileKind(Rectangle current)
    {
       return current switch
        {
            Rectangle ballRec when ballRec.x == 0f && ballRec.y == 0f => Shape.Ball,
            Rectangle cubeRec when cubeRec.x == 64f && cubeRec.y == 0f => Shape.Cube,
            Rectangle zylinderRec when zylinderRec.x == 0f && zylinderRec.y == 64f => Shape.Zylinder,
            Rectangle triangleRec when triangleRec.x == 64f && triangleRec.y == 64f => Shape.Triangle,
            _ => Shape.EMPTY
        };
    }

    private static readonly Tile BLUE_BALL = CreateNewTile(frames[0],
        OriginalColor.Blue,
        -Int2.One,
        false,
        IdentifyTileKind(frames[0]),
        Color.WHITE);

    private static readonly Tile YELLOW_CUBE = CreateNewTile(frames[1],
        OriginalColor.Yellow,
        -Int2.One,
        false,
        IdentifyTileKind(frames[1]),
        Color.WHITE);


    private static readonly Tile RED_ZYLINDER = CreateNewTile(frames[2],
        OriginalColor.Red,
        -Int2.One,
        false,
        IdentifyTileKind(frames[2]),
        Color.WHITE);


    private static readonly Tile GREEN_TRIANGLE = CreateNewTile(frames[3],
        OriginalColor.Green,
        -Int2.One,
        false,
        IdentifyTileKind(frames[3]),
        Color.WHITE);

    public static Tile GetRandomTile(WeightedCellPool cellPool)
    {
        var id = (Shape) Random.Shared.Next((int) Shape.Ball, (int) (Shape.Triangle) + 1);

        switch (id, true)
        {
            case (Shape.Ball, _):
                return CreateNewTile(frames[0],
                    OriginalColor.Blue,
                    cellPool.NextRnd,
                    false,
                    IdentifyTileKind(frames[0]),
                    Color.WHITE);

            case (Shape.Cube, _):
                return CreateNewTile(frames[1],
                    OriginalColor.Yellow,
                    cellPool.NextRnd,
                    false,
                    IdentifyTileKind(frames[1]),
                    Color.WHITE);
                ;

            case (Shape.Zylinder, _):
                return CreateNewTile(frames[2],
                    OriginalColor.Red,
                    cellPool.NextRnd,
                    false,
                    IdentifyTileKind(frames[2]),
                    Color.WHITE);
                ;

            case (Shape.Triangle, _):
                return CreateNewTile(frames[3],
                    OriginalColor.Green,
                    cellPool.NextRnd,
                    false,
                    IdentifyTileKind(frames[3]),
                    Color.WHITE);
                ;
        }

        throw new ArgumentNullException("This code shall NEVER be reached!");
    }

    public override string ToString() => $"CurrentPos: {Cell}; -- Shape: {Shape} -- OriginColor: {OriginColor} ";

    public void Draw(float? elapsedTime)
    {
        if (!wasDrawn)
        {
            font = Raylib.LoadFont(FontPath);
            wasDrawn = true;
        }

        Vector2 worldPosition = new Vector2(Cell.X, Cell.Y) * Program.TileSize;
        Color drawColor = Selected ? Color.BLUE : Blur;
        //drawColor = Raylib.ColorAlpha(drawColor, CurrentAlpha);
        //CurrentAlpha = Lerp(CurrentAlpha, targetAlpha, alphaSpeed * elapsedTime);
        Raylib.DrawTextureRec(Program.TileSheet, DrawDestination, worldPosition, drawColor);

        float xCenter = worldPosition.X + Program.TileSize.X / 4.3f;
        float yCenter = worldPosition.Y < 128f ? worldPosition.Y + Program.TileSize.Y / 2.5f :
            worldPosition.Y >= 128f ? (worldPosition.Y + Program.TileSize.Y / 2f) - 5f : 0f;

        Vector2 drawPos = new(xCenter - 10f, yCenter);
        Raylib.DrawTextEx(font, Cell.ToString(), drawPos, 20f, 1f, SameColor(Blur, Color.BLACK) ? Color.WHITE : Color.BLACK );
    }
    
    public bool Equals(ITile? other)
    {
        if (other is Tile d)
        {
            return Shape == d.Shape &&
                   OriginColor == d.OriginColor;
        }

        return false;
    }
    
}