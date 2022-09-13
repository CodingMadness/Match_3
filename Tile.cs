using System.Numerics;
using System.Runtime.CompilerServices;

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

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes_4_c1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes_4_c2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes_4_c1 == bytes_4_c2 && Math.Abs(c1.CurrentAlpha - (c2.CurrentAlpha)) < 1e-3;;
    }

    public bool Equals(FadeableColor other)
    {
        return this == other;
    }

    public override bool Equals(object? obj)
    {
        return obj is FadeableColor other && this == other;
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
    public void Draw(Int2 position);
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

    private readonly Rectangle[] Frames = new []
    {
        new Rectangle(0f, 0f, 64f, 64f), //blue
        new Rectangle(64f, 0f, 64f, 64f), //yellow
        new Rectangle(0f, 64f, 64f, 64f), //red
        new Rectangle(64f, 64f, 64f, 64f), //green
    };

    private int _id = 0, _noiseCounter = 0;
    
    /// <summary>
    /// TODO: PLAY WITH THIS FUNCTION ABIT MORE TO GET BETTER PROBABILITY
    /// </summary>
    /// <param name="noise"></param>
    private void DefineDestRectByNoise(float? noise)
    {
        /*
        if (_noiseCounter++ == 3) ;
        {
            Array.Reverse(Frames);
            _noiseCounter = 0;
        }
        */
        if (noise <= 0f)
        {
            noise += Random.Shared.NextSingle();
            DefineDestRectByNoise(noise);
        }
        else if (noise <= 0.1)
            noise *= 10;
        
        if (noise <= 0.25f)
            DestRect = Frames.ElementAt(0);
        
        else if (noise is >= 0.15f and <= 0.40000f)
            DestRect = Frames.ElementAt(1);
        
        else if (noise is >= 0.400001f and <= 0.650001f)
            DestRect = Frames.ElementAt(2);
        
        else if (noise is >= 0.660001f and <= 1f)
            DestRect = Frames.ElementAt(3);

        //Console.WriteLine(DestRect.width + "   " + DestRect.height);
    }

    public Shape(float? noise)
    {
        FadeTint = Color.WHITE;
        DefineDestRectByNoise(noise);
        Kind = GetKindByRect();
    }
    
    public readonly ShapeKind Kind { get; }
    public Rectangle DestRect { get; private set; }

    ///THIS is the MAINSRC color, the actualy pixeldata behind the texture!
    public FadeableColor PixelColor { get; set; }
    public FadeableColor FadeTint { get; set; }
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
            var tint = _backingShape.FadeTint;
            
            if (!value) 
            {
                tint.AlphaSpeed = 0.2f;
                tint.TargetAlpha = 1f;
                Console.WriteLine("TARGET ALPHA  : " + tint.TargetAlpha );
            }
            else
            {
                tint.TargetAlpha = tint.CurrentAlpha = 0f;
            }

            _backingShape.FadeTint = tint;
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
                FadeTint = Color.WHITE
            }
        };

        return mapTile;
    }
    
    public override string ToString() => $"CurrentPos: {Cell}; --" +
                                         $" ShapeKind: {_backingShape.Kind} -- FadeTint: {_backingShape.FadeTint} ";

    public void ChangeTo(FadeableColor color)
    {
        _backingShape.FadeTint = color;
    }
    
    public static ITile Create(Int2 position, float? noise)
    {
        return CreateNewTile(position, noise);
    }
    
    public void Draw(Int2 position)
    { 
        void  DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            float xCenter = worldPosition.X + _backingShape.DestRect.width / 4.3f;
            float yCenter = worldPosition.Y < 128f ? worldPosition.Y + _backingShape.DestRect.height / 2.5f :
                worldPosition.Y >= 128f ? (worldPosition.Y + _backingShape.DestRect.height / 2f) - 5f : 0f;
        
            Vector2 drawPos = new(xCenter - 10f, yCenter);
            FadeableColor drawColor = selected ? Color.BLACK : Color.WHITE;
            Raylib.DrawTextEx(AssetManager.Font, worldPosition.ToString(), drawPos, 
                14f, 1f, selected ? Color.BLACK : drawColor);
        }
        position *= Size;

        FadeableColor drawColor = _selected ? Color.WHITE : Color.WHITE;//_backingShape.FadeTint;
        
        Raylib.DrawTextureRec(AssetManager.SpriteSheet,
            _backingShape.DestRect,
            position, 
            drawColor);
        
        DrawTextOnTop(position, _selected);
    }

    public bool Equals(ITile? other)
    {
        if (other is Tile d)
        {
            return _backingShape.Kind == d._backingShape.Kind &&
                   _backingShape.FadeTint == d._backingShape.FadeTint;
        }
        return false;
    }
}