global using CSharpRect = System.Drawing.RectangleF;
global using RayRect = Raylib_cs.Rectangle;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;


namespace Match_3.DataObjects;

public enum Direction : byte
{
    None = 0,
    Right,
    Left,
    Top,
    Bot,
    DiagonalTopLeft,
    DiagonalTopRight,
    DiagonalBotLeft,
    DiagonalBotRight,
    RectBotLeft,
    RectBotRight,
    RectTopLeft,
    RectTopRight,
    EntireMap
}

public enum Layout : byte
{
    Diagonal,
    Block,
    Linear
}

public interface ICell
{
    public Vector2 Start { get; }

    public int Count { get; }

    public bool IsEmpty => Count is 0;

    public Vector2 WorldPos => Start * Config.TileSize;

    public float Distance(Vector2 other) => Vector2.Distance(Start, other);

    public bool IsDiagonal(Vector2 other)
    {
        float distanceX = Math.Abs(Start.X - other.X);
        float distanceY = MathF.Abs(Start.Y - other.Y);
        return (int)distanceX == (int)distanceY;
    }

    public Vector2 GetDirectionTo(Vector2 next)
    {
        Vector2 direction = Vector2.Normalize(Start - next);
        float distance = Vector2.Distance(Start, next);
        Vector2 newVector = Start + direction * distance;
        return newVector;
    }

    public Vector2 GetDiametricalCell(Vector2 begin)
    {
        var direction = GetDirectionTo(begin);
        return Vector2.Negate(direction);
    }

    public string ToString() => $"Starts at: {Start}";
}

public interface IMultiCell : ICell
{
    public SingleCell Begin { get; }
    public SingleCell End { get; }
    public Direction Route { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Layout GetLayoutFrom(Direction route)
    {
        return route switch
        {
            Direction.Right or Direction.Left
                or Direction.Top or Direction.Bot => Layout.Linear,

            Direction.DiagonalBotLeft or Direction.DiagonalBotRight
                or Direction.DiagonalTopLeft or Direction.DiagonalTopRight => Layout.Diagonal,

            Direction.RectBotLeft or Direction.RectBotRight
                or Direction.RectTopLeft or Direction.RectTopRight
                or Direction.EntireMap => Layout.Block,

            Direction.None or _ => throw new ArgumentOutOfRangeException(nameof(route), route, null)
        };
    }

    public Layout Layout => GetLayoutFrom(Route);

    public new string ToString() => $"Starts at: {Begin.Start} and ends at: {End.Start}";
}

public interface IGridRect : ICell
{
    public  Size UnitSize { get; }
    public new int Count => UnitSize.Width * UnitSize.Height;
    public CSharpRect GridBox => new(Start.X, Start.Y, UnitSize.Width, UnitSize.Width);
    public int Area => UnitSize.Width * UnitSize.Height;
}

[StructLayout(LayoutKind.Auto)]
public readonly struct SingleCell : IGridRect
{
    public static implicit operator SingleCell(Vector2 position)
    {
        var gameWindowSize = new Vector2(GameState.Instance.Lvl.GridWidth, GameState.Instance.Lvl.GridHeight);
        var gridPos = new Vector2((int)position.X, (int)position.Y);

        if (gridPos.Length() <= gameWindowSize.Length())
        {
            return new()
            {
                Start = gridPos
            };
        }
        else
        {
            return gridPos / Config.TileSize;
        }
    }

    public static implicit operator Vector2(SingleCell position) => position.Start;
    
    public readonly required Vector2 Start { get; init; }
    
    public readonly int Count => 1; //1x1, cause UnitSize=1x1

    public readonly Size UnitSize => new(1, 1);
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Grid : IGridRect, IMultiCell
{
    public ref struct CellEnumerator
    {
        private Vector2 _current;
        private int _nextLine;
        private int _currCount;

        private readonly int _totalCount;
        private readonly Grid _caller;
        private readonly int _currLine;
        private readonly int _direction;

        public CellEnumerator(Grid caller)
        {
            _totalCount = ((ICell)_caller).Count;
            _currLine = _totalCount / caller.UnitSize.Height; //this gets us the 'width'
            _direction = (_caller.Route switch
            {
                Direction.RectBotRight => 1,
                Direction.RectTopLeft => -1,
                _ => -int.MaxValue
            });
            _caller = caller;
            _direction = 0;
            _current = default;
            _nextLine = 0;
        }

        private Vector2 GetNextCell()
        {
            _current = _caller.Begin.Start with { Y = _caller.Begin.Start.Y + _direction };
            //need to know the exact amount of rows/columns!

            if (_nextLine++ < _currLine)
            {
                _current = _current with { X = _current.X + _direction };
            }
            else
            {
                _currCount += _nextLine;
                _nextLine = 0;
                return GetNextCell();
            }

            return _current;
        }

        [Pure]
        public bool MoveNext()
        {
            _ = GetNextCell();
            return _currCount < _totalCount;
        }

        public void Reset()
        {
            _currCount = 0;
            _current = _caller.Begin;
            _nextLine = 0;
        }

        public readonly SingleCell Current => _current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public required Direction Route { get; init; }
    public required SingleCell Begin { get; init; }

    public readonly SingleCell End
    {
        get
        {
            var step = new Vector2(UnitSize.Width, UnitSize.Height);
            Vector2 start = Begin;

            return Route switch
            {
                Direction.RectBotLeft => start with { X = start.X - step.X, Y = start.Y + step.Y },
                Direction.RectBotRight => start with { X = start.X + step.X, Y = start.Y + step.Y },
                Direction.RectTopLeft => start with { X = start.X - step.X, Y = start.Y - step.Y },
                Direction.RectTopRight => start with { X = start.X + step.X, Y = start.Y - step.Y },
                _ => throw new ArgumentException("Only a direction of kind 'Cell' in its name can be validated" +
                                                 "all other directional values are invalid for this type!")
            };
        }
    }

    public readonly Size UnitSize { get; init; }

    public readonly CellEnumerator GetEnumerator()
    {
        return new CellEnumerator(this);
    }

    readonly Vector2 ICell.Start => Begin.Start;

    readonly int ICell.Count => ((IGridRect)this).Count;

    public new readonly string ToString() => ((IMultiCell)this).ToString();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LinearCellLine : IGridRect, IMultiCell
{
    Vector2 ICell.Start => Begin.Start;

    public readonly Size UnitSize
    {
        get
        {
            return Route switch
            {
                Direction.Left or Direction.Right => new(Count, 1),
                Direction.Top or Direction.Bot => new(1, Count),
                _ => new(-1, -1)
            };
        }
    }

    public readonly SingleCell End
    {
        get
        {
            return Route switch
            {
                Direction.Right => Begin.Start + (Vector2.UnitX * Count),
                Direction.Left => Begin.Start - (Vector2.UnitX * Count),
                Direction.Bot => Begin.Start + (Vector2.UnitY * Count),
                Direction.Top => Begin.Start - (Vector2.UnitY * Count),
                _ => new Vector2(-1, -1)
            };
        }
    }

    public required SingleCell Begin { get; init; }
    public required Direction Route { get; init; }
    public required int Count { get; init; }

    public IEnumerator<ICell> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public readonly new string ToString() => ((IMultiCell)this).ToString();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct DiagonalCellLine : IMultiCell
{
    public required SingleCell Begin { get; init; }
    public required int Count { get; init; }
    public required Direction Route { get; init; }

    Vector2 ICell.Start => Begin.Start;

    public SingleCell End
    {
        get
        {
            return Route switch
            {
                Direction.DiagonalBotLeft => Begin.Start + (new Vector2(-1f, 1f) * Count),
                Direction.DiagonalBotRight => Begin.Start + (Vector2.One * Count),
                Direction.DiagonalTopLeft => Begin.Start - (Vector2.One * Count),
                Direction.DiagonalTopRight => Begin.Start + (new Vector2(1f, -1f) * Count),
                _ => new Vector2(-1, -1)
            };
        }
    }

    public IEnumerator<ICell> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => ((IMultiCell)this).ToString();
}

public class MultiCell<TCell> : IMultiCell where TCell: struct, IMultiCell 
{
    public TCell Cell;

    // Implement IMultiCell interface methods by delegating to 'Cell'
    public Vector2 Start => Cell.Start;

    public int Count => Cell.Count;

    public SingleCell Begin => Cell.Begin;

    public SingleCell End => Cell.End;

    public Direction Route => Cell.Route;
        
    public Layout Layout => Cell.Layout;

    public override string ToString() => ((IMultiCell)Cell).ToString();
        
    public static MultiCell<TCell> FromIMultiCell(TCell self) => new() { Cell = self };
}