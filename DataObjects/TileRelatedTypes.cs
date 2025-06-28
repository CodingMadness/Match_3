global using TileColorTypes = System.Drawing.KnownColor;
global using Comparer = Match_3.Service.Comparer;
using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using DotNext.Collections.Generic;
using Match_3.Workflow;

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

public interface ICoordinates
{
    public Vector2 Start { get; }
    public Vector2 End { get; }

    public float Distance(ICoordinates other) => Vector2.Distance(Start, other.Start);

    public bool IsDiagonal(ICoordinates other)
    {
        float distanceX = Math.Abs(Start.X - other.Start.X);
        float distanceY = MathF.Abs(Start.Y - other.Start.Y);
        return (int)distanceX == (int)distanceY;
    }

    public Vector2 GetDirectionTo(ICoordinates next)
    {
        Vector2 direction = Vector2.Normalize(Start - next.Start);
        float distance = Vector2.Distance(Start, next.Start);
        var newVector = Start + direction * distance;
        return newVector;
    }

    public Vector2 GetDiametricalCell(ICoordinates begin)
    {
        var direction = GetDirectionTo(begin);
        return Vector2.Negate(direction);
    }

    public string ToString() => $"Starts at: {Start}";
}

public interface IGridRect : ICoordinates
{
    public Size UnitSize { get; }
    public int Count => UnitSize.Width * UnitSize.Height;
    public Rectangle Skeleton => new((int)Start.X, (int)Start.Y, UnitSize.Width, UnitSize.Width);
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Cell : IGridRect
{
    public required Vector2 Start { get; init; }
    public Vector2 End => Start;
    public Size UnitSize => new(1, 1);
    public static implicit operator Cell(Vector2 position) => new() { Start = position };
    public static implicit operator Vector2(Cell position) => position.Start;
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Grid : IGridRect
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

        public CellEnumerator(in Grid caller)
        {
            _totalCount = ((IGridRect)_caller).Count;
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
            _current = _caller.Start.Start with { Y = _caller.Start.Start.Y + _direction };
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
            _current = _caller.Start;
            _nextLine = 0;
        }

        public readonly Cell Current => _current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public required Direction Route { get; init; }
    public required Cell Start { get; init; }

    public Cell End
    {
        get
        {
            var step = new Vector2(UnitSize.Width, UnitSize.Height);
            Vector2 start = Start;

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

    public Size UnitSize { get; init; }

    public CellEnumerator GetEnumerator()
    {
        return new CellEnumerator(in Unsafe.AsRef(in this));
    }

    Vector2 ICoordinates.Start => Start;
    Vector2 ICoordinates.End => End;
    
    public new string ToString() => ((IGridRect)this).Skeleton.ToString();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Line : IGridRect
{
    Vector2 ICoordinates.Start => Begin.Start;

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

    public Cell End
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

    public required Cell Begin { get; init; }
    public required Direction Route { get; init; }
    public required int Count { get; init; }

    Vector2 ICoordinates.End => End;
    
    public IEnumerator<ICoordinates> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public override string ToString() => $"{((IGridRect)this).ToString()} and follows {Route} pathing";
}

[StructLayout(LayoutKind.Auto)]
public readonly struct Diagonal : IGridRect
{
    public required Cell Begin { get; init; }
    public Size UnitSize { get; }
    public required int Count { get; init; }
    public required Direction Route { get; init; }
    
    public Cell End
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

    public IEnumerator<ICoordinates> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    Vector2 ICoordinates.Start => Begin;
    Vector2 ICoordinates.End => Begin;
    
    public override string ToString() => ((IGridRect)this).ToString();
}

public interface IGameObject
{
    public Vector2 Position { get; }
    
    public ConcreteShape Body { get; }
    
    public string ToString() => $"Tile at: {Position}; ---- with type: {Body.Colour.Type}";
}

/// <summary>
/// This class shall track which Tile(KEY) has to close neighbors(VALUES) which shall be moved away!
/// </summary>
public class TileGraph : IEnumerable<Tile>
{
    public class Node(Tile root) : IGameObject
    {
        private const int MaxEdges = 4;
        public readonly Tile Root = root;
        public readonly List<Node> Links = new(MaxEdges);

        /// <summary>
        /// Describes the amount of how many connections to close neighbors it got
        /// </summary>
        public int Edges;

        public override string ToString() => ((IGameObject)this).ToString();

        public void CutLink()
        {
            int i = 0;
            foreach (var node in Links)
            {
                node.Edges--;
                node.Links.Remove(this);
                Links.AsSpan()[i++] = null!;
            }

            Edges = 0;
        }

        public Cell Cell => Root.Cell;

        Vector2 IGameObject.Position => Root.Cell.Start;

        ConcreteShape IGameObject.Body => Root.Body;
    }

    private readonly Node[] _sameColored;
    private readonly Comparer.DistanceComparer _distanceComparer;

    public TileGraph(Tile[,] bitmap, TileColorTypes colorTypes)
    {
        _sameColored =
        [
            .. bitmap
                .OfType<Tile>()
                .Where(x => x.Body.Colour.Type == colorTypes)
                .OrderBy(x => x, Comparer.CellComparer.Singleton)
                .Select(x => new Node(x))
        ];

        _distanceComparer = new();
    }

    private IEnumerable<IEnumerable<Node>> AddEdges()
    {
        static void UpdateNode(Node first, Node second)
        {
            first.Edges++;
            first.Links.Add(second);
        }

        for (var i = 0; i < _sameColored.Length; i++)
        {
            var current = _sameColored[i];

            var list = _sameColored
                .Skip(1 + i)
                .Where(x =>
                {
                    bool isClose = _distanceComparer.Are2Close(current.Root, x.Root) == true;

                    if (!isClose)
                        return false;

                    //add both to each other in their own list
                    UpdateNode(current, x);
                    UpdateNode(x, current);

                    return true;
                });

            var enumerable = list as Node[] ?? list.ToArray();

            if (enumerable.Length is 0)
                continue;

            yield return enumerable.Prepend(current);
        }
    }

    private IOrderedEnumerable<Node>? SortByEdge()
    {
        var allAdjacentTiles = new HashSet<Node>(_sameColored.Length, Comparer.CellComparer.Singleton);

        foreach (var adjacent in AddEdges())
        {
            allAdjacentTiles.AddAll(adjacent.Where(x => x.Edges > 0));
        }

        return allAdjacentTiles.Count > 0
            ? allAdjacentTiles.OrderByDescending(x => x, Comparer.EdgeComparer.Singleton)
            : null;
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        //Filter all "most-edges" out of the already filtered (edges > 1)-list
        var sortedNodes = SortByEdge();

        if (sortedNodes is null)
            yield break;

        foreach (var current in sortedNodes)
        {
            if (current.Edges == 0)
                continue;

            current.CutLink();

            yield return current.Root;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[Flags]
public enum TileState : byte
{
    Disabled = 1,
    NotRendered = 4,
    Selected = 8,
    UnChanged = 16,
    Pulsate = 32
}

public abstract class Texture
{
    public required Vector2 TextureLocation
    {
        init => field = value * Config.TileSize;
        get;
    }

    private static readonly Size TextureSize = new(Config.TileSize, Config.TileSize);
    public Raylib_cs.Rectangle AtlasInfo => new((int)TextureLocation.X, (int)TextureLocation.Y, TextureSize.Width, TextureSize.Height);
}

public class ConcreteShape : Texture
{
    public FadeableColor Colour = White;
    public override string ToString() => $"Tile type: <{Colour.Type}>";
}

public class ConcreteRectangularBody(IGridRect cellRect) : ConcreteShape
{
    private UpAndDownScale _resizeFactor;
    private Rectangle _scaledRect = cellRect.Skeleton;

    private Rectangle AsWorld => new(
        _scaledRect.X * Config.TileSize,
        _scaledRect.Y * Config.TileSize,
        _scaledRect.Width * Config.TileSize,
        _scaledRect.Height * Config.TileSize);

    public Raylib_cs.Rectangle WorldRect => new(AsWorld.X, AsWorld.Y, AsWorld.Width, AsWorld.Height);

    public void ScaleBox(float currTime)
    {
        _resizeFactor = new(currTime: currTime);
        _scaledRect = _resizeFactor * cellRect;
    }
}

/// <summary>
/// Notes for Tile:
///   * mutable
///   * simple (no big modeling/ complex class hierarchy)
///   * light-weight, does have more value properties/fields than anything else
///   * has to be stored inside a 2D array where access and GC does matter
/// </summary>
public class Tile : IGameObject
{
    public TileState State { get; set; }

    public Cell CellB4Swap { get; set; }

    public required Cell Cell { get; set; }

    public bool IsDeleted => State.HasFlag(TileState.Disabled) &&
                             State.HasFlag(TileState.NotRendered);

    public required ConcreteRectangularBody Body { get; init; }

    /// <summary>
    /// Body consists of :
    ///   * Colour
    ///   * ColourType
    ///   * Rectangle
    /// </summary>
    ConcreteShape IGameObject.Body => Body;

    Vector2 IGameObject.Position => Cell.Start;

    public override string ToString() => $"Cell: {Cell.Start}; ---- {Body}";
}

public class MatchX : IGameObject, IEnumerable<Tile>
{
    private Vector2 _position;

    private readonly SortedSet<Tile> _matches = new(Comparer.CellComparer.Singleton);

    private static readonly Dictionary<Layout, IGridRect> CachedCellTemplates = new(3)
    {
        {
            Layout.Block,
            new Grid
            {
                Start = default,
                UnitSize = default,
                Route = Direction.None
            }
        },
        {
            Layout.Linear,
            new Line
            {
                Count = 0,
                Begin = default,
                Route = Direction.None
            }
        },
        {
            Layout.Diagonal,
            new Diagonal
            {
                Count = 0,
                Begin = default,
                Route = Direction.None,
            }
        }
    };

    private static void ClearMatchBox(Direction lookUpUsedInMatchFinder)
    {
        // if (lookUpUsedInMatchFinder is Direction.None)
        //     return;
        //
        // var matchBox = CachedCellTemplates[IMultiCell.GetLayoutFrom(lookUpUsedInMatchFinder)];
        //
        // switch (matchBox)
        // {
        //     case Line wrapper:
        //     {
        //         wrapper.Cell = default;
        //         break;
        //     }
        //     case Diagonal wrapper:
        //     {
        //         wrapper.Cell = default;
        //         break;
        //     }
        // }
    }

    public int Count => _matches.Count;

    public bool IsMatchFilled => Count == Config.MaxTilesPerMatch;

    // private IMultiCell? _place;

    public Tile? FirstInOrder { get; set; }

    public ConcreteShape? Body { get; private set; }

    Vector2 IGameObject.Position => _position;

    ConcreteShape IGameObject.Body => Body;

    public void Add(Tile matchTile)
    {
        _matches.Add(matchTile);

        if (!IsMatchFilled)
            return;

        Body = FirstInOrder!.Body;
        _position = FirstInOrder.Cell;
    }

    public void BuildMatchBox(Direction direction)
    {
        // //we use pre-cached cell types to avoid runtime-boxing,
        // //by instantiating them at startup and using a class as a wrapper for the actual
        // //structs which implement IMultiCell, so then we have allocation free interfaces.
        // var matchBox = CachedCellTemplates[IMultiCell.GetLayoutFrom(direction)];
        //
        // switch (matchBox)
        // {
        //     case MultiCell<Line> wrapper:
        //     {
        //         var lcl = new Line
        //         {
        //             Begin = FirstInOrder!.Cell,
        //             Count = Count,
        //             Route = direction
        //         };
        //
        //         wrapper.Cell = lcl;
        //         break;
        //     }
        //     case MultiCell<Diagonal> wrapper:
        //     {
        //         var lcl = new Diagonal
        //         {
        //             Begin = FirstInOrder!.Cell,
        //             Count = Count,
        //             Route = direction
        //         };
        //
        //         wrapper.Cell = lcl;
        //         break;
        //     }
        // }
        //
        // _place = matchBox;
    }

    public void Clear(Direction lookUpUsedInMatchFinder)
    {
        _matches.Clear();
        FirstInOrder = null;
        Body = null!;
        ClearMatchBox(lookUpUsedInMatchFinder);
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        return _matches.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public new string ToString() => $"A match{Count} of type: {Body!.Colour.Name} starting at position: {_position}";
}