using System.Buffers;
using DotNext;
using DotNext.Buffers;
using Match_3.DataObjects;

namespace Match_3.Service;

/// <summary>
/// This Pool has the following attributes:
///   * It checks if the incoming span is already inside the pool to avoid copy and just return back the one inside the pool.
///   * It is a recyclable pool, so when end reached it begins from 0 again.
///   * It works like a FIRO-Queue(First-In-Random-Out), and gives you back the desired slice at any arbitrary position
///   * It gives you only back pushed-in spans nothing new will be created
///   * It is an one-time init Pool, meaning you define an initial size as precisely as needed
///     and the Pool will allocate a <example>maximumPushCount *</example> more than that
///  </summary>
///  <typeparam name="T"></typeparam>
/// <param name="length">the total length of the span, if 'maximumPushCount' is set,
///  it will allocate: (approxCount * length) * sizeof(T) elements</param>
public struct SpanPool<T>(int length, int? sliceCount2Track) : IDisposable where T : unmanaged, IEquatable<T>
{
    private MemoryOwner<T> _content = new(ArrayPool<T>.Shared, length);
    private readonly MemoryOwner<Distance>? _ContentSlices
        = sliceCount2Track is not null ? new(ArrayPool<Distance>.Shared, sliceCount2Track.Value) : null;

    private int _enQCharIdx, _currLogLen;

    /// <summary>
    /// The amount of times a Push(span) has been made
    /// </summary>
    private int PushCount { get; set; }

    /// <summary>
    /// This one is only for internal use, like for performance-based algorithms,
    /// which shall skip a lot of checks, since they are not needed for certain scenarios!
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    internal ReadOnlySpan<T> CorePush(ReadOnlySpan<T> items)
    {
        var span = _content.Span; 
        //copy the new span into the pool and update members.
        _currLogLen = items.Length;
        var fullItemsCopy = span.Slice((int)_enQCharIdx, (int)_currLogLen);
        items.CopyTo(fullItemsCopy);
        _enQCharIdx += _currLogLen;
        return fullItemsCopy;
    }

    /// <summary>
    /// Enqueues a span into the message pool and
    /// gives you back the span from the pool
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ignoreSeparatorChars">notes if the user actually do not want any delimiters in his span</param>
    /// <returns></returns>
    public ReadOnlySpan<T> Push(ReadOnlySpan<T> input, bool ignoreSeparatorChars = false)
    {
        //check if we are already at max or the current "items" is already in the span, if so, just return back the one from the pool!
        // if (_Infos.Span[_pushCount].hash == items.BitwiseHashCode())
        //     return ReadOnlySpan<T>.Empty;
        var entireSpan=_content.Span;

        if (EndReached)
            return [];

        //check if the current "items" is only a slice of what is already in the pool
        //OR if the current pool is only a slice of "items"
        if (_enQCharIdx > 0 || ignoreSeparatorChars)
        {
            var withoutEmpties = _content.Span.TrimLength((int)_enQCharIdx);

            if (entireSpan.IndexOf(input) != -1)
                return withoutEmpties;

            if (input.LastIndexOf(withoutEmpties) != -1)
                input = input[withoutEmpties.Length..];
        }

        //we save here the '_enQCharIdx' before it was pushed in, because we need that snapshot for later "Peeks()";
        int first = _enQCharIdx;
        var copyOfPoolSlice = CorePush(input);

        if (_ContentSlices is not null)
        {
            Distance spanInfo = new(first..(first + _currLogLen), entireSpan.Length);
            _ContentSlices.Value[PushCount++] = spanInfo;
        }

        //now we push as well the updated length so we basically pushed a '(int start, int length)'
        return copyOfPoolSlice;
    }

    /// <summary>
    /// Gives you back a slice within the buffer based on <param name="index"></param>
    /// if 'sliceCount2Track' is not null otherwise throws notsupported exception
    /// </summary>
    /// <returns>The Span based on <param name="index"></param></returns>
    public ReadOnlySpan<T> Peek(int index)
    {
        if (PushCount == 0)
            return [];

        if (_ContentSlices is null)
            throw new NotSupportedException("Since we do not care about the internal positioning of " +
                                            "our pushed-slices/spans, you make a statement that you dont want to get these spans in any order back" +
                                            "bur rather you want to get back the last pushed");

        var slice = (Range)_ContentSlices.Value[index];
        return _content.Span[slice];
    }

    private readonly bool EndReached => _enQCharIdx == _content.Length;

    private void Flush()
    {
        _enQCharIdx = 0;
        PushCount = 0;
    }

    public void Dispose()
    {
        Flush();
        _content.Dispose();
    }
}