global using TileColor = System.Drawing.KnownColor;
global using Comparer = Match_3.Service.Comparer;

using System.Collections;
using System.Drawing;
using System.Numerics;
using Match_3.Service;

[assembly: FastEnumToString(typeof(TileColor), IsPublic = true, ExtensionMethodNamespace = "Match_3.Variables.Extensions")]

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

public abstract class RenderInfo
{
    private readonly Vector2 mTextureLoc;

    public required Vector2 TextureLocation
    {
        init => mTextureLoc = value * Config.TileSize;
        get => mTextureLoc;
    }

    public static readonly Size TextureSize = new(Config.TileSize, Config.TileSize);

    public CSharpRect CSTextureRect => new(TextureLocation.X, TextureLocation.Y, TextureSize.Width, TextureSize.Height);

    public RayRect RayTextureRect => new(CSTextureRect.X, CSTextureRect.Y, CSTextureRect.Width, CSTextureRect.Height);
}

public interface IProjectable
{
    public CSharpRect ToWorld(IGridCell cell)
    {
        return new(cell.WorldPos.X * Config.TileSize,
            cell.WorldPos.Y * Config.TileSize,
            cell.UnitSize.Width * Config.TileSize,
            cell.UnitSize.Height * Config.TileSize);
    }
 
    public RayRect AsRayWorldBox => new(WorldBox.X, WorldBox.Y, WorldBox.Width, WorldBox.Height);
    public (CSharpRect newBox, Scale next) ScaleSysBox(Scale scale) => scale * WorldBox;

    public RayRect ScaleRayBox(ref Scale scale)
    {
        // Console.WriteLine(scale);
        var scaledBox = scale * AsRayWorldBox;
        scale = scaledBox.next;
        return scaledBox.newBox;
    }
}

public class Shape() : RenderInfo, IProjectable
{
    private readonly Scale _resizeFactor = new();
    
    public Scale GetScaling(float seconds) => _resizeFactor.GetFactor();

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

/// <summary>
/// Notes for Tile:
///   * mutable
///   * simple (no big modeling/ complex class hierarchy)
///   * light-weight, does have more value properties/fields than anything else
///   * has to be stored inside a 2D array where access and GC does matter
/// </summary>
/// <param name="body"></param>
public class Tile(Shape body) : IGameTile
{
    public TileState State { get; set; }

    public SingleCell CellB4Swap { get; set; }

    public SingleCell Cell { get; set; }

    /// <summary>
    /// Body consists of :
    ///   * Color
    ///   * TileKind
    ///   * Rectangle
    /// </summary>
    public Shape Body { get; } = body;

    Vector2 IGameTile.Position => Cell.Start;

    public bool IsDeleted => State.HasFlag(TileState.Disabled) &&
                             State.HasFlag(TileState.NotRendered);

    public override string ToString() => $"SingleCell: {Cell.Start}; ---- {Body}";
}

public class MatchX : IGameTile, IEnumerable<Tile>
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

    Vector2 IGameTile.Position => _position;
    Shape IGameTile.Body => Body;
    
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