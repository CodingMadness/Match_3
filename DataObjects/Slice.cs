namespace Match_3.DataObjects;

public readonly struct Slice<T> : IEquatable<Slice<T>>, IComparable<Slice<T>>
    where T : unmanaged, IEquatable<T>
{
    public override int GetHashCode() => Start.GetHashCode();

    private readonly int _srcLen;
    public readonly int Start;
    private readonly int _length;
    public readonly int End;

    public Slice(Range r, int srcLen)
    {
        _srcLen = srcLen;
        (int offset, int len) = r.GetOffsetAndLength(srcLen);
        Start  = offset;
        _length = len;
        End    = offset + len;
    }

    public Slice<T> GetSlice(Slice<T> other)
    {
        int diff = Math.Abs(_length - other._length);
        Slice<T> newOne = this - diff;
        return newOne;
    }

    public (int start, int length, int end) Deconstruct() => (Start, _length, End);

    public bool Equals(Slice<T> other) => Start == other.Start;

    public int CompareTo(Slice<T> other) => Start.CompareTo(other.Start);

    public override string ToString() => ((Range)this).ToString();

    public static Slice<T> operator +(Slice<T> self, int incRangeBy)
    {
        var copy = new Slice<T>(self.Start..(self.End + incRangeBy), self._srcLen);
        return copy;
    }

    public static Slice<T> operator -(Slice<T> self, int incRangeBy)
    {
        var copy = new Slice<T>(self.Start..(self.End - incRangeBy), self._srcLen);
        return copy;
    }

    public static bool operator ==(Slice<T> self, Slice<T> other) => other.Equals(self);

    public static bool operator !=(Slice<T> self, Slice<T> other) => !(self == other);

    public static bool operator >(Slice<T> self, Slice<T> other) => self.CompareTo(other) == 1;

    public static bool operator <(Slice<T> self, Slice<T> other) => !(self > other || self == other);

    public static implicit operator Range(Slice<T> self) => self.Start..(self.End);

    public int Overlaps(Slice<T> other)
    {
        (int startOther, _, int endOther) = other.Deconstruct();

        bool isOverlap = Start < endOther && startOther < endOther;
        return isOverlap ? endOther - startOther : 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is Slice<T> slice && Equals(slice);
    }
}