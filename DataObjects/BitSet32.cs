using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Match_3.DataObjects;

public struct BitSet32 : IEquatable<BitSet32>
{
    /// <summary>
    /// Enumerates the bits of the array from least-significant to
    /// most-significant. It's OK to change the array while enumerating.
    /// </summary>
    public ref struct BitEnumerator
    {
        /// <summary>
        /// Pointer to the bits
        /// </summary>
        private readonly ref uint _bits;
 
        /// <summary>
        /// Index into the bits
        /// </summary>
        private int _index;
 
        /// <summary>
        /// GetProperMultiCell the enumerator with index at -1
        /// </summary>
        /// 
        /// <param name="bits">
        /// Bits to enumerate
        /// </param>
        public BitEnumerator(uint bits)
        {
            _bits = ref Unsafe.AsRef(ref bits);
            _index = -1;
        }
 
        /// <summary>
        /// Move to the next bit
        /// </summary>
        /// 
        /// <returns>
        /// If a bit is available via <see cref="Current"/>. If not, enumeration
        /// is done.
        /// </returns>
        public bool MoveNext() => _index++ < 32;

        /// <summary>
        /// Get the current bit. If <see cref="MoveNext"/> has not been called
        /// or the last call of <see cref="MoveNext"/> returned false, this
        /// function asserts.
        /// </summary>
        /// 
        /// <value>
        /// The current bit
        /// </value>
        public bool Current
        {
            get
            {
                RequireIndexInBounds();
                uint mask = 1u << _index;
                return (_bits & mask) == mask;
            }
        }

        private void RequireIndexInBounds()
        {
            Debug.Assert(_index is >= 0 and < 32, "Index out of bounds: " + _index);
        }
    }

    public readonly Index LastBit; 
    
    /// <summary>
    /// Integer whose bits make up the array
    /// </summary>
    private uint _bits;
    /// <summary>
    /// GetProperMultiCell the array with the given bits
    /// </summary>
    /// 
    /// <param name="bits">
    /// Bits to make up the array
    /// </param>
    private BitSet32(uint bits)
    {
        _bits = bits;
        //_lastBit = can only be 31 at max! since 31 bits are the maximum for uint/int32.Max
        byte range = _bits switch
        {
            < 256 => 8,
            > 256 and < ushort.MaxValue => 16,
            > ushort.MaxValue and < uint.MaxValue => 32,
            _ => throw new IndexOutOfRangeException("Value was out of index")
        };

        LastBit = Index.FromEnd(range);
    }

    /// <summary>
    /// Get or set the bit at the given index. For faster getting of multiple
    /// bits, use <see cref="GetBits(uint)"/>. For faster setting of single
    /// bits, use <see cref="SetBit(int)"/> or <see cref="Zero"/>. For
    /// faster setting of multiple bits, use <see cref="SetBits(uint)"/> or
    /// <see cref="ZeroBits"/>.
    /// </summary>
    /// 
    /// <param name="index">
    /// Index of the bit to get or set
    /// </param>
    public bool this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            //RequireIndexInBounds(index);
            uint mask = 1u << index;
            return (_bits & mask) == mask;
        }
        set
        {
            RequireIndexInBounds(index);
            uint mask = 1u << index;
            
            _bits = value 
                ? (_bits | mask)
                : (_bits & ~mask);
        }
    }

    /// <summary>
    /// Get the length of the array
    /// </summary>
    /// 
    /// <value>
    /// The length of the array. Always 32.
    /// </value>
    public const byte Length = 31;

    /// <summary>
    /// Set a single bit to 1
    /// </summary>
    /// 
    /// <param name="index">
    /// Index of the bit to set. Asserts if not on [0:31].
    /// </param>
    public void SetBit(int index)
    {
        RequireIndexInBounds(index);
        uint mask = 1u << index;
        _bits |= mask;
    }
    /// <summary>
    /// Set a single bit to 0
    /// </summary>
    /// 
    /// <param name="index">
    /// Index of the bit to unset. Asserts if not on [0:31].
    /// </param>
    public void Zero(int index)
    {
        RequireIndexInBounds(index);
        uint mask = 1u << index;
        _bits &= ~mask;
    }
    /// <summary>
    /// Get all the bits that match a mask
    /// </summary>
    /// 
    /// <param name="mask">
    /// Mask of bits to get
    /// </param>
    /// 
    /// <returns>
    /// The bits that match the given mask
    /// </returns>
    public uint GetBits(uint mask) => _bits & mask;

    /// <summary>
    /// Computes the number of bits which are occupied by "_bits"
    /// </summary>
    public byte GetBitWidth()
    {
        byte bitWidth = 0;
        uint tmp = _bits;
        
        while (tmp > 0)
        {
            //we shift-right until the value becomes 0,
            //this is the trigger for us to know, how much space this value needs. 
            tmp >>= 1;
            bitWidth++;
        }

        if (bitWidth > Length)
            throw new ArgumentOutOfRangeException(nameof(bitWidth), "Value is too large to fit in a 32-bit integer.");

        return bitWidth;
    }
    /// <summary>
    /// Set all the bits that match a mask to 1
    /// </summary>
    /// 
    /// <param name="mask">
    /// Mask of bits to set
    /// </param>
    public void SetBits(uint mask) => _bits |= mask;
    /// <summary>
    /// Set all the bits that match a mask to 0
    /// </summary>
    /// 
    /// <param name="mask">
    /// Mask of bits to unset
    /// </param>
    public void ZeroBits(uint mask) => _bits &= ~mask;
    /// <summary>
    /// Check if this array equals an object
    /// </summary>
    /// 
    /// <param name="obj">
    /// Object to check. May be null.
    /// </param>
    /// 
    /// <returns>
    /// If the given object is a BitArray32 and its bits are the same as this
    /// array's bits
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is BitSet32 array32 && _bits == array32._bits;
    }
    /// <summary>
    /// Check if this array equals another array
    /// </summary>
    /// 
    /// <param name="arr">
    /// Array to check
    /// </param>
    /// 
    /// <returns>
    /// If the given array's bits are the same as this array's bits
    /// </returns>
    public bool Equals(BitSet32 arr) => _bits == arr._bits;
    /// <summary>
    /// Get the hash code of this array
    /// </summary>
    /// 
    /// <returns>
    /// The hash code of this array, which is the same as
    /// the hash code of <see cref="_bits"/>.
    /// </returns>
    public override int GetHashCode() => _bits.GetHashCode();
    /// <summary>
    /// Get a string representation of the array
    /// </summary>
    /// 
    /// <returns>
    /// A newly-allocated string representing the bits of the array.
    /// </returns>
    public override string ToString()
    {
        Span<char> chars = stackalloc char[1 + Length+1 + 1];
        chars[0] = '{';

        int i = 1;

        for (uint num = 1u << Length-1; num > 0; num >>= 1, ++i) 
            chars[i] = (_bits & num) > 0 ? '1' : '0';
        
        chars[^1] = '}';

        return chars.ToString();
    }
    /// <summary>
    /// Assert if the given index isn't in bounds
    /// </summary>
    /// 
    /// <param name="index">
    /// Index to check
    /// </param>
    public static void RequireIndexInBounds(int index)
    {
        Debug.Assert(index is >= 0 and < 32,"Index out of bounds: " + index);
    }
    /// <summary>
    /// Get an enumerator for this array's bits
    /// </summary>
    /// 
    /// <returns>
    /// An enumerator for this array's bits
    /// </returns>
    [UnscopedRef]
    public BitEnumerator GetEnumerator()
    {
        return new BitEnumerator(_bits);
    }

    public static implicit operator BitSet32(uint value) => new(value);
    
    public static bool operator ==(BitSet32 left, BitSet32 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BitSet32 left, BitSet32 right)
    {
        return !(left == right);
    }
}