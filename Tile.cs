using System.Data;
using System.Numerics;
using Raylib_cs;

namespace Match_3;

using System.Runtime.CompilerServices;
using Raylib_cs;

public struct FadeableColour : IEquatable<FadeableColour>
{
    private Color toWrapp;
    public float CurrentAlpha, TargetALpha, AlphaSpeed, ElapsedTime;

    private FadeableColour(Color color)
    {
        toWrapp = color;
        CurrentAlpha = 0f;
        TargetALpha = 0f;
        AlphaSpeed = 0f;
        ElapsedTime = 0f;
    }

    private static float Lerp(float? firstFloat, float secondFloat, float? by)
    {
        return firstFloat ?? (float) (firstFloat * (1 - by) + secondFloat * by);
    }

    private static readonly Dictionary<Color, string> strings = new ()
    {
        {Color.BLACK, "Black"},
        {Color.BLUE, "Blue"},
        {Color.BROWN, "Brown"},
        {Color.DARKGRAY, "DarkGray"},
        {Color.GOLD, "Gold"},
        {Color.GRAY, "Gray"},
        {Color.GREEN, "Green"},
        {Color.LIGHTGRAY, "LightGray"},
        {Color.MAGENTA, "Magenta"},
        {Color.MAROON, "Maroon"},
        {Color.ORANGE, "Orange"},
        {Color.PINK, "Pink"},
        {Color.PURPLE, "Purple"},
        {Color.RAYWHITE, "RayWhite"},
        {Color.RED, "Red"},
        {Color.SKYBLUE, "SkyBlue"},
        {Color.VIOLET, "Violet"},
        {Color.WHITE, "White"},
        {Color.YELLOW, "Yellow"}
    };

    public string ToReadableString()
    {
        Color compare = toWrapp;
        compare.a = byte.MaxValue;
        return strings.TryGetValue(compare, out var value) ? value : toWrapp.ToString();
    }
    private Color Lerp(float degree)
    {
        toWrapp = Raylib.ColorAlpha(toWrapp, degree);
        CurrentAlpha = Lerp(CurrentAlpha, TargetALpha, AlphaSpeed * ElapsedTime);
        return toWrapp;
    }

    public static implicit operator FadeableColour(Color color)
    {
        return new FadeableColour(color);
    }

    public static implicit operator Color(FadeableColour color)
    {
        return color.Lerp(color.ElapsedTime);
    }

    public static bool operator ==(FadeableColour c1, FadeableColour c2) =>
        c1.toWrapp.a == c2.toWrapp.a &&
        c1.toWrapp.b == c2.toWrapp.b &&
        c1.toWrapp.g == c2.toWrapp.g &&
        c1.toWrapp.r == c2.toWrapp.r;

    public bool Equals(FadeableColour other)
    {
        return this == other &&
               Math.Abs(CurrentAlpha - (other.CurrentAlpha)) < 1e-3;
    }

    public override bool Equals(object? obj)
    {
        return obj is FadeableColour other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(toWrapp, CurrentAlpha);
    }

    public static bool operator !=(FadeableColour c1, FadeableColour c2) => !(c1 == c2);

    public override string ToString() => nameof(toWrapp);
}

public enum ShapeKind : sbyte
{
    Ball,
    Cube,
    Cylinder,
    Triangle,
    EMPTY = -1
}

public interface ITile //: IEquatable<ITile>
{
    public Int2 Cell { get; set; }
    public Int2 CoordsB4Swap { get; set; }
    public int Size { get; }
    public  bool Selected { get; set; }
    public void Draw(float? elapsedTime);
    public static abstract ITile Create(Int2 position);
}

public struct Shape
{
    public ShapeKind Form { get; }
    
    public FadeableColour Tint;
    
    public Rectangle DestRect { get; }
    
    private static ShapeKind IdentifyTileKind(Rectangle current) =>
        current switch
        {
            { x: 0f, y: 0f } => ShapeKind.Ball,
            { x: 64f, y: 0f } => ShapeKind.Cube,
            { x: 0f, y: 64f } => ShapeKind.Cylinder,
            { x: 64f, y: 64f } => ShapeKind.Triangle,
            _ => ShapeKind.EMPTY
        };
    
    private static readonly Rectangle[] frames = {
        new Rectangle(0f, 0f, 64f, 64f), //blue
        new Rectangle(64f, 0f, 64f, 64f), //yellow
        new Rectangle(0f, 64f, 64f, 64f), //red
        new Rectangle(64f, 64f, 64f, 64f), //green
    };

    public static Rectangle GetRndRect()
    {
        var id = Random.Shared.Next(0, frames.Length);
        return frames[id];
    }
    
    public Shape(Rectangle current)
    {
        Tint = Color.WHITE;
        DestRect = current;
        Form = IdentifyTileKind(current);
    }
    
    public void Draw(Vector2 position)
    {
        Raylib.DrawTextureRec(AssetManager.SpriteSheet, DestRect, position, Color.WHITE);
    }
    
    public void DrawTextOnTop(in Vector2 worldPosition, bool selected)
    {
        float xCenter = worldPosition.X + DestRect.width / 4.3f;
        float yCenter = worldPosition.Y < 128f ? worldPosition.Y + DestRect.height / 2.5f :
            worldPosition.Y >= 128f ? (worldPosition.Y + DestRect.height / 2f) - 5f : 0f;
        
        Vector2 drawPos = new(xCenter - 10f, yCenter);
        FadeableColour drawColor = selected ? Color.BLACK : Tint;
        Raylib.DrawTextEx(AssetManager.Font, worldPosition.ToString(), drawPos, 
            20f, 1f, drawColor == Tint ? Color.WHITE : Color.BLACK);
    }
}

public sealed class Tile :  ITile
{
    public Int2 Cell { get; set; }
    public int Size => 64;
    public Int2 CoordsB4Swap { get; set; }

    private bool selected;

    private Shape _backingShape;
    
    public ref Shape GetTileShape( )
    {
        return ref _backingShape;
    }

    public bool Selected
    {
        get => selected;

        set
        {
            if (!value)
            {
                _backingShape.Tint.TargetALpha = 1;
            }
            else
            {
                _backingShape.Tint.TargetALpha = _backingShape.Tint.CurrentAlpha = 0f;
            }

            selected = value;
        }
    }
    
    private Tile()
    {
       
    }

    private static Tile CreateNewTile(Int2 coord)
    {
        var mapTile = new Tile
        {
            Cell = coord,
            CoordsB4Swap = -Int2.One,
            selected = false,
            _backingShape = new (Shape.GetRndRect())
            {
                Tint = Color.WHITE
            }
            
        };

        return mapTile;
    }
    
    public override string ToString() => $"CurrentPos: {Cell}; --" +
                                         $" ShapeKind: {_backingShape.Form} -- Tint: {_backingShape.Tint.ToReadableString()} ";

    public void Draw(float? elapsedTime)
    {
        Vector2 worldPosition = new Vector2(Cell.X, Cell.Y);
        _backingShape.Draw(worldPosition);
        _backingShape.DrawTextOnTop(worldPosition, selected);
    }

    public static ITile Create(Int2 position)
    {
        return CreateNewTile(position);
    }

    public bool Equals(ITile? other)
    {
        if (other is Tile d)
        {
            return _backingShape.Form == d._backingShape.Form &&
                   _backingShape.Tint == d._backingShape.Tint;
        }
        return false;
    }
}