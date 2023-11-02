using Match_3.DataObjects;

namespace Match_3.Service;

public static class Comparer
{
    public sealed class StateAndBodyComparer : EqualityComparer<Tile>
    {
        public override bool Equals(Tile? x, Tile? y)
        {
            if (x is not { }) return false;
            if (y is not { }) return false;
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            if ((x.TileState & TileState.Deleted) == TileState.Deleted ||
                (x.TileState & TileState.Disabled) == TileState.Disabled) return false;
        
            return x.Body.Equals(y.Body);
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
}