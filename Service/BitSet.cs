using System.Collections.Specialized;

namespace Match_3.Service;

/// <summary>
/// This class is meant to allow to pass any amount of byte-values 
/// and remember the individual values, like a usual set or "BitVector32" or "BitArray"
/// but with the difference to get them back later on
/// </summary>
public struct BitSet
{
    private BitVector32 bitVector = new(0);

    /// <summary>
    /// We allow to store a range of bytes, representing values ---<= 255
    /// </summary>
    /// <param name="valuesUnder256"></param>
    public BitSet(Span<byte> valuesUnder256)
    {
        if (valuesUnder256.Length is < 0 or > 31)
            throw new IndexOutOfRangeException(nameof(valuesUnder256));
       
        FastSpanEnumerator<byte> valueEnumerator = new(valuesUnder256);

        foreach (var value in valueEnumerator)
        {
            // bitVector[1 << index] = true;     
        }
    }

    // public void SetBit(int index)
    // {
    //     if (index is < 0 or > 31)
    //         throw new IndexOutOfRangeException("Index out of range.");
    //     
    //     bitVector[1 << index] = true;
    // }

    
    // public int[] GetSetValues()
    // {
    //     var values = new List<int>();
    //     int index = 0;
    //     while (index < 32)
    //     {
    //         if (bitVector[1 << index])
    //         {
    //             values.Add(index);
    //         }
    //         index++;
    //     }
    //     return values.ToArray();
    // }
}