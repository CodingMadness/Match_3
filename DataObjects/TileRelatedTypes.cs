global using TileColor = System.Drawing.KnownColor;
global using Comparer = Match_3.Service.Comparer;
using System.Collections;
using System.Drawing;
using System.Numerics;
using Match_3.Service;
using Raylib_cs;
using Color = System.Drawing.Color;

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
    private readonly Vector2 mTextureLoc;

    public required Vector2 TextureLocation
    {
        init => mTextureLoc = value * Config.TileSize;
        get => mTextureLoc;
    }

    public static readonly Size TextureSize = new(Config.TileSize, Config.TileSize);

    public CSharpRect CSTextureRect => new(TextureLocation.X, TextureLocation.Y, TextureSize.Width, TextureSize.Height);

    public RayRect AssetRect => new(CSTextureRect.X, CSTextureRect.Y, CSTextureRect.Width, CSTextureRect.Height);
}

public class Shape : Texture
{
    public FadeableColor Color = White;
    public override string ToString() => $"Tile type: <{TileKind}>";
    public required TileColor TileKind { get; init; }
}

public class RectShape(IGridRect cellRect) : Shape
{
    private UpAndDownScale ResizeFactor;
    private CSharpRect _scaledRect = cellRect.GridBox;
    
    public CSharpRect AsWorld => new(
        _scaledRect.X * Config.TileSize,
        _scaledRect.Y * Config.TileSize,
        _scaledRect.Width * Config.TileSize,
        _scaledRect.Height * Config.TileSize);

    public RayRect WorldRect => new(AsWorld.X, AsWorld.Y, AsWorld.Width, AsWorld.Height);

    public void ScaleBox(float currTime)
    {
        ResizeFactor = new(currTime: currTime);
        _scaledRect = ResizeFactor * cellRect.GridBox;
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
    ///   * Color
    ///   * TileKind
    ///   * Rectangle
    /// </summary>
    Shape IGameObject.Body => Body;

    Vector2 IGameObject.Position => Cell.Start;
    
    public override string ToString() => $"SingleCell: {Cell.Start}; ---- {Body}";
}

public class MatchX : IGameObject, IEnumerable<Tile>
{
    private Vector2 _position;

    protected readonly SortedSet<Tile> Matches = new(Comparer.CellComparer.Singleton);

    protected static readonly Dictionary<Layout, IMultiCell> CachedCellTemplates = new(3)
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

    public int Count => Matches.Count;

    public bool IsMatchFilled => Count == Config.MaxTilesPerMatch;

    public IMultiCell? Place { get; private set; }

    public Tile? FirstInOrder { get; set; }

    public Shape? Body { get; private set; }

    Vector2 IGameObject.Position => _position;
    
    Shape IGameObject.Body => Body;

    public void Add(Tile matchTile)
    {
        Matches.Add(matchTile);

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

        Place = matchBox;
    }

    public void Clear(Direction lookUpUsedInMatchFinder)
    {
        Matches.Clear();
        FirstInOrder = null;
        Body = null!;
        ClearMatchBox(lookUpUsedInMatchFinder);
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        return Matches.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public new string ToString() => $"A match{Count} of type: {Body.TileKind} starting at position: {Place}";
}