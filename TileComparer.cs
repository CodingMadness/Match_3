using System.Numerics;

namespace Match_3;

public sealed class StateAndBodyComparer : EqualityComparer<Tile>
{
    public override bool Equals(Tile? x, Tile? y)
    {
        if (x is not { } x1) return false;
        if (y is not { } y1) return false;
        if (ReferenceEquals(x1, y1)) return true;
        if (ReferenceEquals(x1, null)) return false;
        if (ReferenceEquals(y1, null)) return false;
        if (x1.GetType() != y1.GetType()) return false;

        TileState xGoodTileState = (x1.TileState & TileState.Clean) == TileState.Clean ||
                          (x1.TileState & TileState.Hidden) == TileState.Hidden
            ? x1.TileState
            : TileState.Deleted;
        TileState yGoodTileState = (y1.TileState & TileState.Clean) == TileState.Clean ||
                           (y1.TileState & TileState.Hidden) == TileState.Hidden
            ? y1.TileState
            : TileState.Deleted;
        
        bool xHasY = (xGoodTileState & yGoodTileState) == yGoodTileState;
        bool yHasX = (yGoodTileState & xGoodTileState) == xGoodTileState;
        
        return (xHasY ||yHasX)  && ((TileShape)x1.Body).Equals(y1.Body as TileShape);
    }
    public override int GetHashCode(Tile obj)
    {
        return HashCode.Combine((int)obj.TileState, obj.Body);
    }
    public static StateAndBodyComparer Singleton => new();
}

public sealed class CellComparer : EqualityComparer<Tile>, IComparer<Tile>
{
    public override bool Equals(Tile? x, Tile? y)
    {
        return Compare(x, y) == 0;
    }
    public override int GetHashCode(Tile obj)
    {
        return obj.GridCell.GetHashCode();
    }
    public int Compare(Tile? a, Tile? b)
    {
        if (a is null) return -1;
        if (a == b) return 0;
        if (b is null) return 1;
        return a.GridCell.CompareTo(b.GridCell);
    }
    public static CellComparer Singleton => new();
}
