using System.Data;
using System.Numerics;
using Raylib_cs;

namespace Match_3;

using System.Runtime.CompilerServices;
using Raylib_cs;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color toWrapp;
    //private GameTime fadeTimer;
    public float CurrentAlpha, TargetALpha, AlphaSpeed, ElapsedTime;
    
    private FadeableColor(Color color)
    {
        toWrapp = color;
        CurrentAlpha = 1f;
        TargetALpha = 1f;
        AlphaSpeed = 0f;
    }

    private static float _lerp(float? firstFloat, float secondFloat, float? by)
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
    
    private Color Lerp()
    {
        toWrapp = Raylib.ColorAlpha(toWrapp, CurrentAlpha);
        CurrentAlpha = _lerp(CurrentAlpha, TargetALpha, AlphaSpeed * ElapsedTime);
        return toWrapp;
    }

    public static implicit operator FadeableColor(Color color)
    {
        return new FadeableColor(color);
    }

    public static implicit operator Color(FadeableColor color)
    {
        return color.Lerp();
    }

    public static bool operator ==(FadeableColor c1, FadeableColor c2) =>
        c1.toWrapp.a == c2.toWrapp.a &&
        c1.toWrapp.b == c2.toWrapp.b &&
        c1.toWrapp.g == c2.toWrapp.g &&
        c1.toWrapp.r == c2.toWrapp.r;

    public bool Equals(FadeableColor other)
    {
        return this == other &&
               Math.Abs(CurrentAlpha - (other.CurrentAlpha)) < 1e-3;
    }

    public override bool Equals(object? obj)
    {
        return obj is FadeableColor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(toWrapp, CurrentAlpha);
    }

    public static bool operator !=(FadeableColor c1, FadeableColor c2) => !(c1 == c2);

    public override string ToString() => ToReadableString();
}

public enum ShapeKind : sbyte
{
    Ball,
    Cube,
    Cylinder,
    Triangle,
    Empty = -1
}

public interface ITile //: IEquatable<ITile>
{
    public Int2 Cell { get; set; }
    public Int2 CoordsB4Swap { get; set; }
    public int Size { get; }
    public  bool Selected { get; set; }
    public void Draw();
    public static abstract ITile Create(Int2 position);
    public bool Equals(ITile? other);
}

public record struct Shape
{
    private readonly ShapeKind IdentifyTileKind()
    {
        return DestRect switch
        {
            { x: 0f, y: 0f } => ShapeKind.Ball,
            { x: 64f, y: 0f } => ShapeKind.Cube,
            { x: 0f, y: 64f } => ShapeKind.Cylinder,
            { x: 64f, y: 64f } => ShapeKind.Triangle,
            _ => ShapeKind.Empty
        };
    }

    private static readonly Rectangle[] frames = {
        new Rectangle(0f, 0f, 64f, 64f), //blue
        new Rectangle(64f, 0f, 64f, 64f), //yellow
        new Rectangle(0f, 64f, 64f, 64f), //red
        new Rectangle(64f, 64f, 64f, 64f), //green
    };

    private readonly Rectangle GetRndRect()
    {
        var id = Random.Shared.Next(0, frames.Length);
        return frames[id];
    }

    public Shape()
    {
        DestRect = GetRndRect();
        Kind = IdentifyTileKind();
    }
    
    public readonly ShapeKind Kind { get; }

    public readonly Rectangle DestRect { get; }

    //THIS must be mutable!
    public FadeableColor SrcColor { get; set; }
    
    public readonly void Draw(Vector2 position)
    {
        Raylib.DrawTextureRec(AssetManager.SpriteSheet, DestRect, position, Color.WHITE);
        //Console.WriteLine(DestRect);
    }
    
    public readonly void  DrawTextOnTop(in Vector2 worldPosition, bool selected)
    {
        float xCenter = worldPosition.X + DestRect.width / 4.3f;
        float yCenter = worldPosition.Y < 128f ? worldPosition.Y + DestRect.height / 2.5f :
            worldPosition.Y >= 128f ? (worldPosition.Y + DestRect.height / 2f) - 5f : 0f;
        
        Vector2 drawPos = new(xCenter - 10f, yCenter);
        FadeableColor drawColor = selected ? Color.BLACK : SrcColor;
        Raylib.DrawTextEx(AssetManager.Font, worldPosition.ToString(), drawPos, 
            20f, 1f, drawColor == SrcColor ? Color.WHITE : Color.BLACK);
    }
}

public sealed class Tile :  ITile
{
    public Int2 Cell { get; set; }
    public int Size => Grid<Tile>.TILE_SIZE;
    public Int2 CoordsB4Swap { get; set; }

    private bool selected;

    private Shape _backingShape;

    public bool Selected
    {
        get => selected;

        set
        {
            var color = _backingShape.SrcColor;
            
            if (!value)
            {
                color.AlphaSpeed = 1f; 
            }
            else
            {
                color.TargetALpha = color.CurrentAlpha = 0f;
            }

            _backingShape.SrcColor = color;
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
           
            _backingShape = new()
            {
                SrcColor = Color.WHITE
            }
        };

        return mapTile;
    }
    
    public override string ToString() => $"CurrentPos: {Cell}; --" +
                                         $" ShapeKind: {_backingShape.Kind} -- SrcColor: {_backingShape.SrcColor} ";

    public void Draw()
    {
        Vector2 worldPosition = new Vector2(Cell.X, Cell.Y) * Size;
        _backingShape.Draw(worldPosition);
        //_backingShape.DrawTextOnTop(worldPosition, selected);
    }

    public void ChangeTo(FadeableColor color)
    {
        _backingShape.SrcColor = color;
    }
    
    public static ITile Create(Int2 position)
    {
        return CreateNewTile(position);
    }

    public bool Equals(ITile? other)
    {
        if (other is Tile d)
        {
            return _backingShape.Kind == d._backingShape.Kind &&
                   _backingShape.SrcColor == d._backingShape.SrcColor;
        }
        return false;
    }
}