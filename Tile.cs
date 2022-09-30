using System.Numerics;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public struct FadeableColor : IEquatable<FadeableColor>
{
    private Color _toWrap;
    public float CurrentAlpha, TargetAlpha, ElapsedTime;
    /// <summary>
    /// The greater this Value, the faster it fades!
    /// </summary>
    public float AlphaSpeed;

    private FadeableColor(Color color)
    {
        _toWrap = color;
        AlphaSpeed = 0.5f; //this basically states that we cannot fade!
        CurrentAlpha = 1.0f;
        TargetAlpha = 0.0f;
        ElapsedTime = 1f;
    }
    
    private static readonly Dictionary<Color, string> Strings = new()
    {
        {BLACK, "Black"},
        {BLUE, "Blue"},
        {BROWN, "Brown"},
        {DARKGRAY, "DarkGray"},
        {GOLD, "Gold"},
        {GRAY, "Gray"},
        {GREEN, "Green"},
        {LIGHTGRAY, "LightGray"},
        {MAGENTA, "Magenta"},
        {MAROON, "Maroon"},
        {ORANGE, "Orange"},
        {PINK, "Pink"},
        {PURPLE, "Purple"},
        {RAYWHITE, "RayWhite"},
        {RED, "Red"},
        {SKYBLUE, "SkyBlue"},
        {VIOLET, "Violet"},
        {WHITE, "White"},
        {YELLOW, "Yellow"}
    };
    
    private void _Lerp()
    {
        //if u wana maybe stop fading at 0.5f so we explicitly check if currALpha > targetalpha
        if (CurrentAlpha > TargetAlpha)  
            CurrentAlpha -= AlphaSpeed * (1f / ElapsedTime);
    }

    public string? ToReadableString()
    {
        Color compare = _toWrap;
        compare.a = byte.MaxValue;
        return Strings.TryGetValue(compare, out var value) ? value : _toWrap.ToString();
    }

    public FadeableColor Apply()
    {
        _Lerp();
        return this with { _toWrap = ColorAlpha(_toWrap, CurrentAlpha) };
    }

    public static implicit operator FadeableColor(Color color)
    {
        return new FadeableColor(color);
    }
    
    public static implicit operator Color(FadeableColor color)
    {
        return color._toWrap;
    }

    public static bool operator ==(FadeableColor c1, FadeableColor c2)
    {
        int bytes4C1 = Unsafe.As<Color, int>(ref c1._toWrap);
        int bytes4C2 = Unsafe.As<Color, int>(ref c2._toWrap);
        return bytes4C1 == bytes4C2;//&& Math.Abs(c1.CurrentAlpha - (c2.CurrentAlpha)) < 1e-3; ;
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

/// <summary>
/// A hardcoded type which is created from a look into the AngryBallsTexture!
/// </summary>
public enum Balls
{
    Red, Blue, Green, Purple, Orange, Yellow, Brown, Violet,
    Length = Violet + 1, Empty = -1,
}

public enum ShapeKind
{
    Circle,
    Quader,
    Rectangle,
    Heart,
    Trapez
}

public enum Coat
{
    A, B, C, D, E, F, G, H
}

public abstract class Shape
{
    public virtual ShapeKind Form { get; set; }
    public virtual Vector2 FrameLocation { get; init; }

    private FadeableColor _f;
    //public FadeableColor FadeTint => _f;

    public ref FadeableColor Color => ref _f;
    public void ChangeColor(Color c, float alphaSpeed, float targetAlpha)
    {
        _f = c;
        _f.AlphaSpeed = alphaSpeed;
        _f.TargetAlpha = targetAlpha;
    }
}

public class CandyShape : Shape, IEquatable<CandyShape>//, IShape<CandyShape>
{
    public CandyShape()
    {
       ChangeColor(WHITE,0f, 1f);
    }
    public Balls Ball { get; init; }
    public Coat Layer { get; init; }
    public override ShapeKind Form { get; set; }
    public override Vector2 FrameLocation { get; init; }
    public bool Equals(CandyShape? other) =>
        other is not null && Ball == other.Ball && Layer == other.Layer;
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Ball);
    }

    public override string ToString() =>
        $"Tile type: <{Ball}> with Tint: <{Color}>"; //and Opacitylevel: {FadeTint.CurrentAlpha}";

    public override bool Equals(object obj)
    {
        return obj is CandyShape shape && Equals(shape);
    }

    public static bool operator ==(CandyShape left, CandyShape? right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CandyShape left, CandyShape right)
    {
        return !(left == right);
    }
}

[Flags]
public enum TileState
{
    UnDestroyable = 0,
    UnMovable = 1,
    UnShapeable = 2,
    
    Destroyable = 4,
    Movable = 8,
    Shapeable = 16
}

public interface ITile : IEquatable<ITile>
{
    public bool IsDeleted { get; set; }
    public TileState State { get; set; }
    public bool IsDisabled { get; protected internal set; }
    public Rectangle DrawRectAround()
    {
        DrawRectangle((int)Cell.X * Size, 
            (int)Cell.Y* Size,
            Size, Size, 
            ColorAlpha(RED, 1f));
        
        return new(Cell.X * Size, Cell.Y * Size, Size, Size);
    }
    public Vector2 TileUnitX => Vector2.UnitX * Size;
    public Vector2 TileUnitY => Vector2.UnitY * Size;
    public static bool IsOnlyDefaultTile(ITile? current) =>
        current is Tile and not EnemyTile; 
        
    private static Texture atlas;

    public static ref Texture GetAtlas() => ref atlas;
    public Vector2 Cell { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public static int Size => 64;
    public Vector2 WorldPosition => (Cell * Size) + (Vector2.One * Size);
    public Vector2 ChangeTileSize(float xFactor, float yFactor) =>
        WorldPosition with { X = WorldPosition.X * xFactor, Y = WorldPosition.Y * yFactor };
    public bool Selected { get; set; }
    public void Draw(float elapsedTime);
}

public class Tile : ITile
{
    private bool _selected;
    public virtual bool IsDeleted { get; set; }
    
    public virtual TileState State { get; set; }

    protected internal bool IsDisabled { get; set; }

    bool ITile.IsDisabled
    {
        get => IsDisabled;
        set => IsDisabled = value;
    }

    /// <summary>
    /// Important always is: Match Cell with the actual Drawing-Location of the window!
    /// </summary>
    public Vector2 Cell { get; set; }
    public Vector2 CoordsB4Swap { get; set; }
    public Shape Body { get; init; }
    public bool Selected
    {
        get => _selected;

        set
        {
            if (value)
            {
                Body.Color.CurrentAlpha = 1f;
                Body.Color.TargetAlpha = 0.35f;
                Body.Color.AlphaSpeed = 0.35f;
            }
            else
            {
                Body.Color.AlphaSpeed = 0f;
            }
            _selected = value;
        }
    }
    private Rectangle DestRect => new(Body.FrameLocation.X, Body.FrameLocation.Y, ITile.Size, ITile.Size);
    
    public Tile()
    {
        //we just init the variable with a dummy value to have the error gone, since we will 
        //overwrite the Body anyway with the Factorymethod "CreateNewTile(..)";
        Body = null!;
    }

    public override string ToString() => $"Cell: {Cell}; ---- {Body}";
    
    public virtual void Draw(float elapsedTime)
    {
        void DrawTextOnTop(in Vector2 worldPosition, bool selected)
        {
            Font copy = GetFontDefault() with { baseSize = 1024 };
            /*Vector2 drawAt = worldPosition + Vector2.One *
                             15f - (Vector2.UnitX *6f) + 
                             (Vector2.UnitY * 6f);*/
            var begin = (this as ITile).WorldPosition;
            float halfSize = ITile.Size * 0.5f;
            begin = begin with { X = begin.X - halfSize - 0, Y = begin.Y - halfSize - ( halfSize * 0.3f)};

            GameText coordText = new(copy, (worldPosition / ITile.Size).ToString(), 11.5f) 
            {
                Begin = begin,
                Color = selected ? RED : BLACK,
            };
            
            coordText.Color.AlphaSpeed = 0f;
            coordText.ScaleText();
            //Console.WriteLine(coordText.Begin / ITile.Size);
            //DrawLine((int)coordText.Begin.X, (int)coordText.Begin.Y, 512, 0, RED);
            //DrawCircle((int)begin.X, (int)begin.Y, 3.5f, BLACK);
            coordText.Draw(2f);
            //Console.WriteLine(begin);
        }

        //we draw 1*Tilesize in Y-Axis,
        //because our game-timer occupies an entire row so we begin 1 further down in Y 
        //var pos = Cell == Vector2.Zero ? Cell + Vector2.UnitY * ITile.Size : Cell * ITile.Size;
        Body.Color.ElapsedTime = elapsedTime;
        DrawTextureRec(ITile.GetAtlas(), DestRect, Cell * ITile.Size, Body.Color.Apply());
        DrawTextOnTop(Cell * ITile.Size, _selected);
    }
    public void Disable()
    {
        Body.ChangeColor(BLACK, 0f, 1f);
        State = TileState.UnMovable | TileState.UnShapeable;
        IsDisabled = true;
    }
    
    public void Enable()
    {
        //draw from whatever was the 1. sprite-atlas 
        Body.ChangeColor(WHITE, 0f, 1f);
        State = TileState.Movable | TileState.Shapeable;
        IsDisabled = false;
    }
    
    public bool Equals(Tile? other)
    {
        return Body switch
        {
            CandyShape c when other?.Body is CandyShape d   && 
                                                !IsDeleted   && 
                                                c.Equals(d) => true,
            _ => false
        };
    }
    public static bool operator ==(Tile? a, Tile? b)
    {
        return a?.Equals(b) ?? false;
    }
    public static bool operator !=(Tile? a, Tile? b)
    {
        return !(a == b);
    }
    bool IEquatable<ITile>.Equals(ITile? other)
    {
        return Equals(other as Tile);
    }
}

public class EnemyTile : Tile
{
    private Vector2 TileUnitX => (this as ITile).TileUnitX;
    public override TileState State => TileState.UnMovable;
    public static Rectangle DangerZone { get; private set; }
    
    public void BlockSurroundingTiles(Grid map, bool disable)
    {
        bool goDiagonal = false;
        
        static Vector2 NextFrom(Vector2 input, Grid.Direction direction, bool goDiagonal)
        {
            if (!goDiagonal)
            {
                return direction switch
                {
                    Grid.Direction.NegativeX => input with { X = input.X - 1 },
                    Grid.Direction.PositiveX => input with { X = input.X + 1 },
                    Grid.Direction.NegativeY => input with { Y = input.Y - 1 },
                    Grid.Direction.PositiveY => input with { Y = input.Y + 1 },
                    _ => Vector2.Zero
                };
            }
            else
            {
                return direction switch
                {
                    Grid.Direction.NegativeX => input - Vector2.One,
                    Grid.Direction.PositiveX => input + Vector2.One,
                    Grid.Direction.NegativeY => input with { X = input.X + 1, Y = input.Y - 1},
                    Grid.Direction.PositiveY => input with { X = input.X - 1, Y = input.Y + 1},
                    _ => Vector2.Zero
                };
            }
        }
            
        const Grid.Direction lastDir = (Grid.Direction)4;

        if (map[Cell] is not null)
        {
            for (Grid.Direction i = 0; i < lastDir; i++)
            {
                if (i == lastDir - 1 && goDiagonal == false)
                {
                    goDiagonal = true;
                    i = 0;
                }
                
                Vector2 nextCoords = NextFrom(Cell, i, goDiagonal);

                if (ITile.IsOnlyDefaultTile(map[nextCoords]))
                {
                    var t = map[nextCoords] as Tile;

                    if (disable)
                        t!.Disable();
                    
                    else
                        t!.Enable();
                }
            }
        }
    }

    public Vector2? ComputeBeginOfMatch3Rect(Grid map)
    {
        Vector2 nextLeft = Cell - Vector2.UnitX;
        //take the DiagonalNegativeX-Vector2 ffrom the enemy who 
        //has only a default tile as neighbor
        if (map[nextLeft] is Tile tile)
        {
            tile.Cell = tile.Cell with { Y = tile.Cell.Y - 1 };
            return tile.Cell;
        }
        return null;
    }    
    
    public static Rectangle DrawRectAroundMatch3(Grid map, Vector2 beginRect)
    {
        var allEnemies = map.FindAllEnemies();
        allEnemies.TryGetNonEnumeratedCount(out int match3Count);

        var axis  = allEnemies
            .GroupBy(item => item.Cell)
            .OrderBy(gr=>gr.Key.Y)
            .Count();

        if (axis == match3Count)
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            var match3RectWidth = match3Count + 2;
            var match3RectHeight = match3Count;
            return Utils.GetMatch3Rect(beginRect * ITile.Size, 
                                        match3RectWidth * ITile.Size, match3RectHeight * ITile.Size);
        }
        else
        {
            //its column based rectangle
            //-*--*--*--|
            // *  X  *  |
            // *  Y  *  |
            // *  Z  *  |
            // *  *  *  |
            //----------|
            var match3RectWidth = match3Count;
            var match3RectHeight = match3Count+2;
            return Utils.GetMatch3Rect(beginRect * ITile.Size, 
                match3RectWidth * ITile.Size, match3RectHeight * ITile.Size);
        }
    }

    public override void Draw(float elapsedTime)
    {
        Selected = false;
        base.Draw(elapsedTime);
    }
}