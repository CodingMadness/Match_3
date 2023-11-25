﻿using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rectangle = Raylib_cs.Rectangle;

namespace Match_3.DataObjects;

public enum Direction : byte
{
    None=0,
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
    Diagonal, Block, Linear
}

public interface ICell
{
    public Vector2 Start { get; }

    public int Count { get; }

    public bool IsEmpty => Count is 0;

    public Vector2 WorldPos => Start * Config.TileSize;

    public void DoScale(Scale factor)
    {
        // return rayRect with
        // {
        //     Width = (rayRect.Width * factor.GetFactor()),
        //     Height = (rayRect.Height * factor.GetFactor())
        // };
    }

    public float Distance(Vector2 other) => Vector2.Distance(Start, other);

    public bool IsDiagonal(Vector2 other)
    {
        float distanceX = Math.Abs(Start.X - other.X);
        float distanceY = MathF.Abs(Start.Y - other.Y);
        return (int)distanceX == (int)distanceY;
    }

    public (Vector2 Direction, bool isRow) GetDirectionTo(Vector2 next)
    {
        bool sameRow = (int)Start.Y == (int)next.Y;

        //switch on direction
        if (sameRow)
        {
            //the difference is positive
            if (Start.X < next.X)
                return (Vector2.UnitX, sameRow);

            if (Start.X > next.X)
                return (-Vector2.UnitX, sameRow);
        }
        //switch on direction
        else
        {
            //the difference is positive
            if (Start.Y < next.Y)
                return (Vector2.UnitY, sameRow);

            if (Start.Y > next.Y)
                return (-Vector2.UnitY, sameRow);
        }

        return (-Vector2.One, false);
    }

    public Vector2 GetDiametricalCell(Vector2 begin)
    {
        var pair = GetDirectionTo(begin);

        if (pair.isRow)
        {
            if (pair.Direction == -Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return Start + Vector2.UnitX;
            }
            else if (pair.Direction == Vector2.UnitX)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return Start - Vector2.UnitX;
            }
        }
        else
        {
            if (pair.Direction == -Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return Start + Vector2.UnitY;
            }

            if (pair.Direction == Vector2.UnitY)
            {
                //store the "PlaceHere" vector2 to set
                //the 3.tile to that position to be X-aligned
                return Start - Vector2.UnitY;
            }
        }

        throw new ArgumentException("line should never be reached!");
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

public interface IRectCell : ICell
{
    public Size UnitSize { get; }
    
    public new int Count => UnitSize.Width * UnitSize.Height;
    
    private RectangleF ToWorld()
    {
        return new(GridBox.X * Config.TileSize,
            GridBox.Y * Config.TileSize,
            GridBox.Width * Config.TileSize,
            GridBox.Height * Config.TileSize);
    }

    public RectangleF GridBox => new(Start.X, Start.Y, UnitSize.Width, UnitSize.Width);
    public RectangleF WorldBox => ToWorld();
    public Rectangle RaylibWorldBox => new(WorldBox.X, WorldBox.Y, WorldBox.Width, WorldBox.Height);
    public int Area => UnitSize.Width * UnitSize.Height;
}


[StructLayout(LayoutKind.Auto)]
public readonly struct SingleCell : IRectCell
{
    public static implicit operator SingleCell(Vector2 position)
    {
        var gameWindowSize = new Vector2(GameState.Lvl.GridWidth, GameState.Lvl.GridHeight);
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
    public required Vector2 Start { get; init; }
    public int Count => 1; //1x1, cause UnitSize=1x1

    public Size UnitSize => new(1, 1);
    
    public override string ToString() => ((ICell)this).ToString();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct CellBlock : IRectCell, IMultiCell
{
    public ref struct CellEnumerator
    {
        private Vector2 _current;
        private int _nextLine;
        private int _currCount;

        private readonly int _totalCount;
        private readonly CellBlock _caller;
        private readonly int _currLine;
        private readonly int _direction;

        public CellEnumerator(CellBlock caller)
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

    
    public SingleCell End
    {
        get
        {
            var step = new Vector2(UnitSize.Width - 1, UnitSize.Height - 1);
            Vector2 start = Begin;

            return Route switch
            {
                Direction.RectBotLeft => start with { X = start.X - step.X, Y = start.Y + step.Y },
                Direction.RectBotRight => start with { X = start.X + step.X, Y = start.Y + step.Y },
                Direction.RectTopLeft => start with { X = start.X - step.X, Y = start.Y - step.Y },
                Direction.RectTopRight => start with { X = start.X + step.X, Y = start.Y - step.Y },
                _ => throw new ArgumentException("Only a direction of kind 'Rect' in its name can be validated" +
                                                 "all other directional values are invalid for this type!")
            };
        }
    }

    public Size UnitSize { get; init; }

    public CellEnumerator GetEnumerator()
    {
        return new CellEnumerator(this);
    }
    
    Vector2 ICell.Start => Begin.Start;

    int ICell.Count => ((IRectCell)this).Count;

    public override string ToString() => ((IMultiCell)this).ToString();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LinearCellLine : IRectCell, IMultiCell
{
    Vector2 ICell.Start => Begin.Start;

    public Size UnitSize
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

    public SingleCell End
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
    
    public override string ToString() => ((IMultiCell)this).ToString();
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