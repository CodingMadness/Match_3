using System.Numerics;
using System.Runtime.CompilerServices;
using NoAlloq;

namespace Match_3.DataObjects;

/// <summary>
/// The goal of this struct is to store multiple smaller int-values into a single int!
/// </summary>
public struct BitPack64
{
    [InlineArray(16)]
    private struct Buffer
    {
        public byte bits;
    }
    
    private BigInteger _packedSlot = 0;
    private byte _idxAdd, _idxGet, _shiftCount;
    private Buffer _countOfBits;

    public readonly int Count => _idxAdd; 
        
    public BitPack64(){ }
    
    public byte GetBitWidth(uint value)
    {
        var bitWidth = (byte)((BigInteger)value).GetBitLength();
        return (_countOfBits[_idxAdd++] = bitWidth);
    }
    
    public void Pack(uint value)
    {
        if (_packedSlot == value)
            return;
        
        // Calculate the shift amount based on the value, but if its the very first Add()
        byte bitWidth = GetBitWidth(value);
        BigInteger mask = (1UL << bitWidth) - 1UL;
        _packedSlot |=  (value & mask) << _shiftCount;
        _shiftCount += bitWidth;
    }
    
    public uint? Unpack()
    {
        if (_idxGet >= _idxAdd)
            return null;
        
        //bitWidth=get the amount of shifts needed to store the value in the lower bits area
        Span<byte> bitCount = _countOfBits;
        byte bitWidth = bitCount[_idxGet];
        byte shiftCount = (byte)(_shiftCount - bitCount
                                               .Slice(_idxGet++, _idxAdd)
                                               .Sum(x => x));        
        
        // Calculate the mask based on bit width
        uint mask = (1u << bitWidth) - 1; 
        uint shiftedValue = (uint)((_packedSlot >> shiftCount) & mask);
        return shiftedValue;
    }
}