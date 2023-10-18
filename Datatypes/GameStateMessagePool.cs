using System.Buffers;
using DotNext.Buffers;

namespace Match_3.Datatypes;

public sealed class GameStateMessagePool(int approxCount, int length) : IDisposable
{
    private MemoryOwner<char> _logPool = new(ArrayPool<char>.Shared, approxCount * length);
    private MemoryOwner<int> _lengthPool = new(ArrayPool<int>.Shared, approxCount);

    private int
        _enQCount,   _deQCount,
        _deQCharIdx, _enQCharIdx,
        _currLogLen;

    private bool _shallClearPool;
    
    /// <summary>
    /// Enqueues a span/string into the message pool and gives you back the same span for comfortable reuse
    /// </summary>
    /// <param name="fullLog"></param>
    /// <returns></returns>
    public ReadOnlySpan<char> Enqueue(ReadOnlySpan<char> fullLog)
    {
        _enQCharIdx = _shallClearPool ? 0 : _enQCharIdx;
        var span = _logPool.Span;
        _currLogLen = fullLog.Length;
        var copyOfFullLog = span.Slice(_enQCharIdx, _currLogLen);
        fullLog.CopyTo(copyOfFullLog);
        _enQCharIdx += _currLogLen;
        span[^1] = ' ';
        _lengthPool.Span[_enQCount++] = _currLogLen;
        return copyOfFullLog;
    }
    
    /// <summary>
    /// Returns based on FIFO-principle the first log from the Queue
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<char> Dequeue()
    {
        if (_enQCount == 0)
            return ReadOnlySpan<char>.Empty;

        var span = _logPool.Span;

        //we will recycle and begin from 0 again..
        if (_deQCount == _lengthPool.Length)
        {
            _deQCount = 0;
            _deQCharIdx = 0;
        }

        int nextLen = _lengthPool.Span[_deQCount++];
        var currPart = span.Slice(_deQCharIdx, nextLen);
        _deQCharIdx += nextLen;
        return currPart;
    }

    public void Clear() => _shallClearPool = true; 

    public void Dispose()
    {
        _logPool.Dispose();
        _lengthPool.Dispose();
    }
}