namespace Match_3;

public unsafe ref struct LinkedSpan<T> where T : unmanaged, IEquatable<T>
{
    private readonly bool IsEmpty => Count == 0;
    private int Count { get; set; }
    
    private StackBuffer buffer;
    private ref T begin;
    private int offset;

    public LinkedSpan()
    {
        //firstOfBuf = null;
        buffer = default;
        offset = 0;
        Count = 0;
    }
   
    private LinkedSpan(ref StackBuffer buffer) : this()
    {
        begin = ref buffer.GetFirst<T>();
    }

    public static implicit operator Span<T>(LinkedSpan<T> total)
    {
        /*
        return new Span<T>(total.firstOfBuf != null ?
                                  total.firstOfBuf :
                                  Unsafe.AsPointer(ref total.buffer.GetFirst<T>()), total.Count);
                                  */
        return new Span<T>(ref total.begin);
    }

    public static implicit operator ReadOnlySpan<T>(LinkedSpan<T> total)
    {
        return new(total.begin);
    }

    private LinkedSpan<T> BuildFilterPattern(delegate*<T, bool> lambda)
    {
        LinkedSpan<T> filtered = new();

        Span<T> src = this;

        for (int i = 0; i < Count; i++)
        {
            if (lambda(src[i]) && !filtered.Contains(src[i]))
                filtered.Append(src[i]);
        }
        //var f = filtered.ToString();
        return filtered.Count > 0 ? filtered : this;
    }

    private readonly bool Contains(T item)
    {
        if (IsEmpty)
            return false;

        Span<T> values = this;
        return values.Contains(item);
    }

    public readonly Span<T>.Enumerator GetEnumerator()
    {
        Span<T> slice = this;
        return slice.GetEnumerator();
    }

    [SkipLocalsInit]
    public readonly ref readonly T this[int index]
    {
        get
        {
            Span<T> tmp = this;
            return ref tmp[index];
        }
    }

    [UnscopedRef]
    public LinkedSpan<T> this[Range area]
    {
        get
        {
            var pair = area.GetOffsetAndLength(Count);

            LinkedSpan<T> copy = new(ref buffer)
            {
                Count = pair.Length,
                offset = pair.Offset,
            };

            return copy;
        }
    }

    public readonly override string ToString()
    {
        ReadOnlySpan<T> slice = this;
        return slice.ToString();
    }

    /*
    public readonly LinkedLinks<T> Append(ref LinkedSpan<T> b)
    {
        if (b.IsEmpty)
            throw new ArgumentException(nameof(b) + " do not append empty ones!");

        LinkedLinks<T> result = new(this);
        return result.Append(ref b);
    }
    */
    private LinkedSpan<T> Append(Span<T> src)
    {
        buffer.Store<T>(src, offset);
        Count += src.Length;
        offset = Count + 1;
        return this;
    }

    public LinkedSpan<T> Append(ReadOnlySpan<T> src)
    {
        buffer.Store(src, offset);
        Count += src.Length;
        offset = Count + 1;
        return this;
    }
    public LinkedSpan<T> Append(T one)
    {
        buffer.Store<T>(new(Unsafe.AsPointer(ref one), 1), offset);
        Count++;
        offset = Count + 1;
        return this;
    }

    /*
    public LinkedSpan<T> RemoveByPattern(delegate*<T, bool> lambda)
    {
        static LinkedSpan<T> Remove(ref LinkedSpan<T> x, ref LinkedSpan<T> toRemove, int index)
        {
            LinkedSpan<T> filtered = new();

            Span<T> tmp = x;

            int foundTrash;
            int startPos = 0;

            while (index < toRemove.Count && (foundTrash = tmp.IndexOf(toRemove[index])) > -1)
            {
                //swap trash with start
                (tmp[startPos], tmp[foundTrash]) = (tmp[foundTrash], tmp[startPos]);

                //slice only to get the area where we want to shift from right to left
                //so  we get back the regular order
                var slice = tmp.Slice(startPos + 1, foundTrash);

                //we want here to do the actual shift from right to left so  we get back the regulary order
                for (int i = foundTrash - 1; i > 0; i--)
                {
                    (slice[i], slice[i - 1]) = (slice[i - 1], slice[i]);
                }

                //now to further find new occurences, we have to slice from startPos+1
                tmp = tmp[(startPos + 1)..];
            }

            filtered.Append(tmp);

            return index < toRemove.Count ? Remove(ref filtered, ref toRemove, ++index) : filtered;
        }
        var removePattern = BuildFilterPattern(lambda);
        var r = Remove(ref this, ref removePattern, 0);
        return r;
    }
    */
}




