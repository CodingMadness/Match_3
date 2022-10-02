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

        State xGoodState = (x1.State & State.Clean) == State.Clean ||
                          (x1.State & State.Hidden) == State.Hidden
            ? x1.State
            : State.Deleted;
        State yGoodState = (y1.State & State.Clean) == State.Clean ||
                           (y1.State & State.Hidden) == State.Hidden
            ? y1.State
            : State.Deleted;
        bool xHasY = (xGoodState & yGoodState) == yGoodState;
        bool yHasX = (yGoodState & xGoodState) == xGoodState;
        
        return (xHasY ||yHasX)  && ((TileShape)x1.Body).Equals(y1.Body as TileShape);
    }
    public override int GetHashCode(Tile obj)
    {
        return HashCode.Combine((int)obj.State, obj.Body);
    }
    public static StateAndBodyComparer Singleton => new();
}

public sealed class CellComparer : EqualityComparer<Tile>
{
    public override bool Equals(Tile? x, Tile? y)
    {
        if (x is null) return false;
        if (y is null) return false;
        return x.Cell == y.Cell;
    }

    public override int GetHashCode(Tile obj)
    {
        return obj.Cell.GetHashCode();
    }

    public static CellComparer Singleton => new();
}
