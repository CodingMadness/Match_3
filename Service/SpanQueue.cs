using System.Buffers;
using DotNext;
using DotNext.Buffers;
using Match_3.DataObjects;

namespace Match_3.Service;

/// <summary>
/// This Pool has the following attributes:
///   * It checks if the incoming span is already inside the pool to avoid copy and just return back the one inside the pool.
///   * It is a recyclable pool, so when End reached it begins from 0 again.
///   * It works like a FIFO-Queue, and gives you back the first -> last in that order
///   * It gives you only back spans, which all points to the inner pool, and dont create new ones. 
///   * It can grow dynamically, if need be, so you can EnQueue() infinitive.
///  </summary>
///  <typeparam name="T"></typeparam>
/// <param name="length">the total length of the span, if shallMultiply is true,
///  it will allocate: (approxCount * length) * sizeof(T) elements</param>
public sealed class SpanQueue<T>(int length) : IDisposable where T : unmanaged, IEquatable<T>
{
    /*
       <0 = ~5, self estimated, means he is unsure of how many resizes there could occur!
        0  = NO resizes at all!
       >0  = the exact value he wants for the resize 
     */
    private MemoryOwner<T> _content = new(ArrayPool<T>.Shared, length + 1);
    private BitPack _lengthPack;
 
    private uint
        _enQCount, 
        _enQCharIdx,
        _deQCharIdx,
        _currLogLen, 
        _nextLen;

    /// <summary>
    /// This one is only for internal use, like for performance-based algorithms,
    /// which shall skip a lot of checks, since they are not needed for certain scenarios!
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    internal ReadOnlySpan<T> CoreEnqueue(ReadOnlySpan<T> items)
    {
        var span = _content.Span; 
        //copy the new span into the pool and update members.
        _currLogLen = (uint)items.Length;
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
    /// <returns></returns>
    public ReadOnlySpan<T> Enqueue(ReadOnlySpan<T> input)
    {
        //check if we are already at max or the current "items" is already in the span, if so, just return back the one from the pool!
        // if (_Infos.Span[_enQCount].hash == items.BitwiseHashCode())
        //     return ReadOnlySpan<T>.Empty;
        var entireSpan=_content.Span;

        if (entireSpan.Length == _enQCharIdx)
            return ReadOnlySpan<T>.Empty;

        //check if the current "items" is only a slice of what is already in the pool
        //OR if the current pool is only a slice of "items"
        if (_enQCharIdx > 0)
        {
            var withoutEmpties = _content.Span.TrimLength((int)_enQCharIdx);

            if ((entireSpan.IndexOf(input)) != -1)
                return withoutEmpties;

            else if ((input.LastIndexOf(withoutEmpties)) != -1)
            {
                input = input[withoutEmpties.Length..];
            }
        }

        var copyOfPoolSlice = CoreEnqueue(input);
        
        //these 2 lines here are only needed if you intent to call "Dequeue()" at some point!
        _lengthPack.Pack(_currLogLen);
        _enQCount++;
        return copyOfPoolSlice;
    }
    
     /// <summary>
     /// Gives you back, based on FIFO model, the current frame of the Pool
     /// </summary>
     /// <param name="shallRecycle">A value indicating if the very 1. valid return of this method
     /// shall give you the same span over and over again</param>
     /// <returns></returns>
    public ReadOnlySpan<T> Dequeue(bool shallRecycle=false)
    {
        if (_enQCount == 0)
            return ReadOnlySpan<T>.Empty;

        var span = _content.Span;
        //we have to avoid somehow, that once the "nextLen" is obtained we need to check 
        //if the value is already in the pack, so it doesnt iterate the entire pack if it can just 
        //give you back the same stuff over and over!
        _nextLen = _lengthPack.Unpack() ?? _nextLen;
        var currPart = span.Slice((int)_deQCharIdx, (int)_nextLen);
        
        //when the 'End=_lengthPack.Count' is reached, we will just recycle and begin from 0 again..
        if (!shallRecycle)
        {
            if (_deQCharIdx == span.Length)
                _deQCharIdx = 0;

            _deQCharIdx += _nextLen;
        }
        
        return currPart;
    }

    public void Clear()
    {
        _enQCharIdx = 0;
        _enQCount = 0;
        _deQCharIdx = 0;
    }

    public void Dispose()
    {
        _content.Dispose();
    }
}