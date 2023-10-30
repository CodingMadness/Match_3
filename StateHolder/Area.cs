namespace Match_3.StateHolder;

public readonly struct Area<T> : IEquatable<Area<T>>, IComparable<Area<T>>
    where T : unmanaged, IEquatable<T>
{
    [Obsolete("Dont use the default Equals(object) and use the one with Equals(Area<T>) to avoid boxing")]
    public override bool Equals(object? obj)
    {
        return obj is Area<T> other && Equals(other);
    }

    public override int GetHashCode() => Start.GetHashCode();

    private readonly int _srcLen;
    public readonly int Start;
    public readonly int Length;
    public readonly int End;

    public Area(int start, int length) : this(.., 0)
    {
        (int offset, int len) tmpRange = (start, length);
        Start  = tmpRange.offset;
        Length = tmpRange.len;
        End    = (tmpRange.offset + tmpRange.len);
        _srcLen = 0;
    }

    public Area(Range r, int srcLen)
    {
        _srcLen = srcLen;
        (int offset, int len) tmpRange = r.GetOffsetAndLength(srcLen);
        Start  = tmpRange.offset;
        Length = tmpRange.len;
        End    = (tmpRange.offset + tmpRange.len);
    }

    public Area<T> Slice(Area<T> other)
    {
        int diff = Math.Abs(Length - other.Length);
        Area<T> newOne = this - diff;
        return newOne;
    }

    public (int start, int length, int end) Deconstruct() => (Start, Length, End);

    public bool Equals(Area<T> other) => Start == other.Start;

    public int CompareTo(Area<T> other) => Start.CompareTo(other.Start);

    public override string ToString() => ((Range)this).ToString();

    public static Area<T> operator +(Area<T> self, int incRangeBy)
    {
        var copy = new Area<T>(self.Start..(self.End + incRangeBy), self._srcLen);
        return copy;
    }

    public static Area<T> operator -(Area<T> self, int incRangeBy)
    {
        var copy = new Area<T>(self.Start..(self.End - incRangeBy), self._srcLen);
        return copy;
    }

    public static bool operator ==(Area<T> self, Area<T> other) => other.Equals(self);

    public static bool operator !=(Area<T> self, Area<T> other) => !(self == other);

    public static bool operator >(Area<T> self, Area<T> other) => self.CompareTo(other) == 1;

    public static bool operator <(Area<T> self, Area<T> other) => !(self > other || self == other);

    public static implicit operator Range(Area<T> self) => self.Start..(self.End);

    public int Overlaps(Area<T> other)
    {
        (int startOther, _, int endOther) = other.Deconstruct();

        bool isOverlap = Start < endOther && startOther < endOther;
        return isOverlap ? endOther - startOther : 0;
    }
}