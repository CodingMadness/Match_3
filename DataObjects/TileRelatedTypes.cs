global using TileColor = System.Drawing.KnownColor;
global using Comparer = Match_3.Service.Comparer;

using System.Collections;
using System.Drawing;
using System.Numerics;
using Match_3.Service;

[assembly:
    FastEnumToString(typeof(TileColor), IsPublic = true, ExtensionMethodNamespace = "Match_3.Variables.Extensions")]

namespace Match_3.DataObjects;

[Flags]
public enum TileState : byte
{
    Disabled = 1,
    NotRendered = 4,
    Selected = 8,
    UnChanged = 16,
    Pulsate = 32
}

public abstract class Resource
{
    private readonly Vector2 _atlasLoc;

    public required Vector2 AtlasLocation
    {
        init => _atlasLoc = value * DataOnLoad.TileSize;
        get => _atlasLoc;
    }

    public static readonly Size Size = new(DataOnLoad.TileSize, DataOnLoad.TileSize);

    public RectangleF TextureRect => new(AtlasLocation.X, AtlasLocation.Y, Size.Width, Size.Height);

    public Raylib_cs.Rectangle RayTextureRect =>
        new(TextureRect.X, TextureRect.Y, TextureRect.Width, TextureRect.Height);
}

public class Shape : Resource
{
    public Scale ResizeFactor = 1f;

    private FadeableColor _color = WHITE;
    public ref readonly FadeableColor Color => ref _color;

    public ref readonly FadeableColor Fade(Color c, float targetAlpha, float elapsedTime)
    {
        _color = c;
        _color.CurrentAlpha = 1f;
        _color.AlphaSpeed = 0.5f;
        _color.TargetAlpha = targetAlpha;
        _color.AddTime(elapsedTime);
        _color = _color.Apply();
        return ref _color;
    }

    public ref readonly FadeableColor Fade(Color c, float elapsedTime) => ref Fade(c, 0f, elapsedTime);
    public ref readonly FadeableColor ChangeColor2(Color c) => ref Fade(c, 1f, 1f);
    protected ref readonly FadeableColor FixedWhite => ref ChangeColor2(WHITE.AsSysColor());
    
    public override string ToString() => $"Tile type: <{TileKind}>";
    
    public required TileColor TileKind { get; init; }
}

public class Tile(Shape body) : IGameTile
{
    public TileState State { get; set; }

    public SingleCell CellB4Swap { get; set; }
  
    public SingleCell Cell { get; set; }

    public Shape Body { get; } = body;

    Vector2 IGameTile.Position => Cell.Start;
    
    public bool IsDeleted => State.HasFlag(TileState.Disabled) &&
                             State.HasFlag(TileState.NotRendered);

    public override string ToString() => $"SingleCell: {Cell.Start}; ---- {Body}";
}

public class MatchX : IGameTile, IEnumerable<Tile>
{
    private Vector2 _position;
    protected class MultiCell<TCell> : IMultiCell where TCell: struct, IMultiCell
    {
        public TCell Cell;

        // Implement IMultiCell interface methods by delegating to StructData
        public Vector2 Start => Cell.Start;

        public int Count => Cell.Count;

        public SingleCell Begin => Cell.Begin;

        public SingleCell End => Cell.End;

        public Direction Route => Cell.Route;
        
        public Layout Layout => Cell.Layout;

        public static MultiCell<TCell> FromIMultiCell(TCell self) => new() { Cell = self };
    }
    protected readonly SortedSet<Tile> Matches = new(Comparer.CellComparer.Singleton);
    protected static readonly Dictionary<Layout, IMultiCell> CachedCellTypes = new(3)
    {
        {
            Layout.Block,
            MultiCell<CellBlock>.FromIMultiCell(new CellBlock
            {
                Begin = default,
                UnitSize = default,
                Route = Direction.None
            })
        },
        {
            Layout.Linear,
            MultiCell<LinearCellLine>.FromIMultiCell(new LinearCellLine
            {
                Begin = default,
                Count = 0,
                Route = Direction.None
            })
        },
        { 
            Layout.Diagonal, 
            MultiCell<DiagonalCellLine>.FromIMultiCell(new DiagonalCellLine
            {
                Begin = default,
                Count = 0,
                Route = Direction.None,
            })
        }
    };

    public int Count => Matches.Count;

    public bool IsMatchFilled => Count == DataOnLoad.MaxTilesPerMatch;

    public IMultiCell Place { get; private set; } = null!;
    
    public Tile FirstInOrder { get; set; } = null!;
    
    public Shape Body { get; private set; } = null!;

    Vector2 IGameTile.Position => _position;
    
    public void Add(Tile matchTile)
    {
        Matches.Add(matchTile);

        if (!IsMatchFilled) 
            return;

        Body = FirstInOrder.Body;
        _position = FirstInOrder.Cell;
    }

    public void BuildMatchBox(Direction direction)
    {
        //we use pre-cached cell types to avoid runtime-boxing,
        //by instantiating them at startup and using a class as a wrapper for the actual
        //structs which implement IMultiCell, so then we have allocation free interfaces.
        var matchBox = CachedCellTypes[IMultiCell.GetLayoutFrom(direction)];

        switch (matchBox)
        {
            case MultiCell<LinearCellLine> wrapper:
            {
                var lcl = new LinearCellLine
                {
                    Begin = FirstInOrder.Cell,
                    Count = Count,
                    Route = direction
                };
                
                wrapper.Cell = lcl;
                break;
            }
            case MultiCell<DiagonalCellLine> wrapper:
            {
                var lcl = new DiagonalCellLine
                {
                    Begin = FirstInOrder.Cell,
                    Count = Count,
                    Route = direction
                };

                wrapper.Cell = lcl;
                break;
            }
        }

        Place = matchBox;
    }

    public void Clear()
    {
        Matches.Clear();
        Body = null!;
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        return Matches.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}