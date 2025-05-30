global using TileColorTypes = System.Drawing.KnownColor;
global using Comparer = Match_3.Service.Comparer;
using System.Collections;
using System.Drawing;
using System.Numerics;

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

public abstract class Texture
{
    private readonly Vector2 _textureLoc;

    public required Vector2 TextureLocation
    {
        init => _textureLoc = value * Config.TileSize;
        get => _textureLoc;
    }

    public static readonly Size TextureSize = new(Config.TileSize, Config.TileSize);

    private CSharpRect CsTextureRect => new(TextureLocation.X, TextureLocation.Y, TextureSize.Width, TextureSize.Height);

    public RayRect AssetRect => new(CsTextureRect.X, CsTextureRect.Y, CsTextureRect.Width, CsTextureRect.Height);
}

public class Shape : Texture
{
    public FadeableColor Colour = White;
    public override string ToString() => $"Tile type: <{Colour.Type}>";
}

public class RectShape(IGridRect cellRect) : Shape
{
    private UpAndDownScale _resizeFactor;
    private CSharpRect _scaledRect = cellRect.GridBox;

    private CSharpRect AsWorld => new(
        _scaledRect.X * Config.TileSize,
        _scaledRect.Y * Config.TileSize,
        _scaledRect.Width * Config.TileSize,
        _scaledRect.Height * Config.TileSize);

    public RayRect WorldRect => new(AsWorld.X, AsWorld.Y, AsWorld.Width, AsWorld.Height);

    public void ScaleBox(float currTime)
    {
        _resizeFactor = new(currTime: currTime);
        _scaledRect = _resizeFactor * cellRect.GridBox;
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

    public SingleCell CellB4Swap { get; set; }

    public required SingleCell Cell { get; set; }
    
    public bool IsDeleted => State.HasFlag(TileState.Disabled) &&
                             State.HasFlag(TileState.NotRendered);

    public required RectShape Body { get; init; }
    
    /// <summary>
    /// Body consists of :
    ///   * Colour
    ///   * ColourType
    ///   * Rectangle
    /// </summary>
    Shape IGameObject.Body => Body;

    Vector2 IGameObject.Position => Cell.Start;
    
    public override string ToString() => $"SingleCell: {Cell.Start}; ---- {Body}";
}

public class MatchX : IGameObject, IEnumerable<Tile>
{
    private Vector2 _position;

    private readonly SortedSet<Tile> _matches = new(Comparer.CellComparer.Singleton);

    private static readonly Dictionary<Layout, IMultiCell> CachedCellTemplates = new(3)
    {
        {
            Layout.Block,
            MultiCell<Grid>.FromIMultiCell(new Grid
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
                Count = 0,
                Begin = default,
                Route = Direction.None
            })
        },
        {
            Layout.Diagonal,
            MultiCell<DiagonalCellLine>.FromIMultiCell(new DiagonalCellLine
            {
                Count = 0,
                Begin = default,
                Route = Direction.None,
            })
        }
    };

    private static void ClearMatchBox(Direction lookUpUsedInMatchFinder)
    {
        if (lookUpUsedInMatchFinder is Direction.None)
            return;
        var matchBox = CachedCellTemplates[IMultiCell.GetLayoutFrom(lookUpUsedInMatchFinder)];

        switch (matchBox)
        {
            case MultiCell<LinearCellLine> wrapper:
            {
                wrapper.Cell = default;
                break;
            }
            case MultiCell<DiagonalCellLine> wrapper:
            {
                wrapper.Cell = default;
                break;
            }
        }
    }

    public int Count => _matches.Count;

    public bool IsMatchFilled => Count == Config.MaxTilesPerMatch;

    private IMultiCell? _place;

    public Tile? FirstInOrder { get; set; }

    public Shape? Body { get; private set; }

    Vector2 IGameObject.Position => _position;
    
    Shape IGameObject.Body => Body;

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
        //we use pre-cached cell types to avoid runtime-boxing,
        //by instantiating them at startup and using a class as a wrapper for the actual
        //structs which implement IMultiCell, so then we have allocation free interfaces.
        var matchBox = CachedCellTemplates[IMultiCell.GetLayoutFrom(direction)];

        switch (matchBox)
        {
            case MultiCell<LinearCellLine> wrapper:
            {
                var lcl = new LinearCellLine
                {
                    Begin = FirstInOrder!.Cell,
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
                    Begin = FirstInOrder!.Cell,
                    Count = Count,
                    Route = direction
                };

                wrapper.Cell = lcl;
                break;
            }
        }

        _place = matchBox;
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

    public new string ToString() => $"A match{Count} of type: {Body.Colour.Name} starting at position: {_place}";
}