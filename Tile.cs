using System.Numerics;

namespace Match_3;

using Raylib_cs;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    //private GameTime fadeTimer;
    public float CurrentAlpha, TargetAlpha, AlphaSpeed, ElapsedTime;
    
    private FadeableColor(Color color)
    {
        _toWrap = color;
        CurrentAlpha = 1f;
        TargetAlpha = 1f;
        AlphaSpeed = 0f;
    }

    private static float _lerp(float? firstFloat, float secondFloat, float? by)
    {
        return firstFloat ?? (float) (firstFloat * (1 - by) + secondFloat * by);
    }

    private static readonly Dictionary<Color, string> Strings = new ()
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
        Color compare = _toWrap;
        compare.a = byte.MaxValue;
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }
    
    private Color Lerp()
    {
        _toWrap = Raylib.ColorAlpha(_toWrap, CurrentAlpha);
        CurrentAlpha = _lerp(CurrentAlpha, TargetAlpha, AlphaSpeed * ElapsedTime);
        return _toWrap;
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
        c1._toWrap.a == c2._toWrap.a &&
        c1._toWrap.b == c2._toWrap.b &&
        c1._toWrap.g == c2._toWrap.g &&
        c1._toWrap.r == c2._toWrap.r;

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
        return HashCode.Combine(_toWrap, CurrentAlpha);
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
    public static abstract ITile Create(Int2 position, float? noise);
    public bool Equals(ITile? other);
}

public record struct Shape
{
    private readonly ShapeKind GetKindByRect()
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

    private static readonly Rectangle[] Frames = 
    {
        new Rectangle(0f, 0f, 64f, 64f), //blue
        new Rectangle(64f, 0f, 64f, 64f), //yellow
        new Rectangle(0f, 64f, 64f, 64f), //red
        new Rectangle(64f, 64f, 64f, 64f), //green
    };

    private void DefineDestRectByNoise(float? noise)
    {
        //noise = (noise < 0f ? -noise : noise) * 10f;
        if (noise <= 0f)
        {
            noise += Random.Shared.NextSingle();
            DefineDestRectByNoise(noise);
        }

        if (noise <= 0.25f)
            DestRect = Frames[0];
        
        else if (noise is >= 0.15f and <= 0.40000f)
            DestRect = Frames[1];
        
        else if (noise is >= 0.400001f and <= 0.650001f)
            DestRect = Frames[2];
        
        else if (noise is >= 0.660001f and <= 1f)
            DestRect = Frames[3];

        //Console.WriteLine(DestRect.width + "   " + DestRect.height);
    }

    public Shape(float? noise)
    {
        DefineDestRectByNoise(noise);
        Kind = GetKindByRect();
    }
    
    public readonly ShapeKind Kind { get; }

    public Rectangle DestRect { get; private set; }

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
    public int Size => Grid<Tile>.TileSize;
    public Int2 CoordsB4Swap { get; set; }

    private bool _selected;

    private Shape _backingShape;

    public bool Selected
    {
        get => _selected;

        set
        {
            var color = _backingShape.SrcColor;
            
            if (!value)
            {
                color.AlphaSpeed = 1f; 
            }
            else
            {
                color.TargetAlpha = color.CurrentAlpha = 0f;
            }

            _backingShape.SrcColor = color;
            _selected = value;
        }
    }
    
    private Tile()
    {
       
    }

    private static Tile CreateNewTile(Int2 coord, float? noise)
    {
        var mapTile = new Tile
        {
            Cell = coord,
            CoordsB4Swap = -Int2.One,
            _selected = false,
           
            _backingShape = new(noise)
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
    
    public static ITile Create(Int2 position, float? noise)
    {
        return CreateNewTile(position, noise);
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