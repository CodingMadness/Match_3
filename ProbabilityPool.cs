using System.Numerics;
using Raylib_cs;

namespace Match_3
{
    public record Probability<T>(float Chance, T Value) : IComparable<Probability<T>>
    {        
        public float Chance { get; init; } = checkChance(Chance);

        private static float checkChance(float chance)
        {            
            //make it positive if its negative
            var tmp = (chance > 0.0f) ? chance : -chance;
            //Any set > 1.0f doesnt make sense since you cant say, you have a probability of 300% or the like
            tmp = tmp > 1.0f ? 1.0f : tmp;
            return tmp;
        }

        public int CompareTo(Probability<T> other)
        {
            return other.Chance.CompareTo(Chance);
        }
    }

    public abstract class ProbabilityPool<TValue> /*:IEnumerable<Probability<TValue>>*/
    {
        protected Probability<TValue>[] m_FixedItems { get; set; }
        protected readonly HashSet<TValue> CellsOccupiedDict;
        protected readonly FastNoiseLite m_FastNoise;

        protected readonly System.Random m_Random;
        protected int runner { get; set; }

        public int Count { get; protected set; }

        protected abstract Vector2 ComputeNoise(TValue initial);

        protected ProbabilityPool(IEnumerable<TValue>? values)
        {
            runner = -1;           
            int bigHash = DateTime.UtcNow.GetHashCode();
            CellsOccupiedDict = new(Count);
            m_Random = new(bigHash);
            m_FastNoise = new(bigHash);
 
            //this codeblock implies, that the user has very likely an enum, 
            //which would be overkill to use an IEnumerable for that, when 
            //we could simply just pass the Flaged-Enums biggest set and 
            //get the other enum values by dividing with 2!
            if (values == null)
                return;

            Count = values.Count();
            m_FixedItems = new Probability<TValue>[Count];

            foreach (var value in values)
            {
                var noise = ComputeNoise(value);
                float chance = m_FastNoise.GetNoise(noise.X, noise.Y);
                Probability<TValue> probability = new(MathF.Round(chance), value);
                m_FixedItems[++runner] = probability;
            }
        }
        
        public TValue Next
        {
            get
            {
                float FindNearest(float compare)
                {
                    var min = float.MaxValue;
                    var minIndex = 0;

                    for (var i = 0; i < m_FixedItems.Length; i++)
                    {
                        var distance = Math.Abs(compare - m_FixedItems[i].Chance);
                        if (distance < min)
                        {
                            min = distance;
                            minIndex = i;
                        }
                    }

                    return m_FixedItems[minIndex].Chance;
                }
                int runner = 0;

            Recursion:
                if (runner == m_FixedItems.Length)
                    return default;

                float val = m_Random.NextSingle();
                float result = FindNearest(val);
                var different = m_FixedItems.FirstOrDefault(x => x.Chance == result).Value;
                //var sorted = CellsOccupiedDict.Keys.OrderBy(state => state.X).OrderBy(state => state.Y).ToArray();

                if (!CellsOccupiedDict.Contains(different))
                {
                    CellsOccupiedDict.Add(different);
                    return different;
                }
                else
                {
                    runner++;
                    goto Recursion;
                }
            }
        }
        
        public bool IgnoreProbability(TValue key) { return false; }
    }
    
    public class WeightedCellPool : ProbabilityPool<IntVector2>
    {
        public WeightedCellPool(IEnumerable<IntVector2> values) : base(values)
        {
        }

        protected override Vector2 ComputeNoise(IntVector2 initial)
        {
            return new(initial.X, initial.Y);
        }
    }

    //public class WeightedStatePool : ProbabilityPool<TileAttributes>
    //{ 
    //    public WeightedStatePool(TileAttributes set) : base(null)
    //    {
    //        int moveBy2 = 1;

    //        while (moveBy2 <= (int)set)
    //        {
    //            Count++;
    //            moveBy2 *= 2;
    //        }

    //        m_FixedItems = new Probability<TileAttributes>[Count];
    //        moveBy2 = 1;

    //        while (moveBy2 <= (int)set)
    //        {                
    //            Vector2 noise = ComputeNoise(set);
    //            float chance = m_FastNoise.GetNoise(noise.X, noise.Y);
    //            Probability<TileAttributes> probability = new(chance.RoundCloser(), set);
    //            m_FixedItems[++runner] = probability;
    //            moveBy2 *= 2;
    //        }
    //    }

    //    protected override Vector2 ComputeNoise(TileAttributes initial)
    //    {
    //        var roundUp = 1 << m_Random.Next(1,10);
    //        return new((float)initial, (float)(TileAttributes)roundUp);
    //    }
    //}

    //public class WeightedItemCategoryPool : ProbabilityPool<PickupCategory>
    //{
    //    public WeightedItemCategoryPool(PickupCategory value) : base(null)
    //    {            
    //        PickupCategory copy = value;
            
    //        while (copy >= 0)
    //        {
    //            Count++;
    //            copy--;
    //        }

    //        m_FixedItems = new Probability<PickupCategory>[Count];

    //        while (value >= 0)
    //        {
    //            Vector2 noise = ComputeNoise(value);
    //            float chance = m_FastNoise.GetNoise(noise.X, noise.Y);
    //            Probability<PickupCategory> probability = new(chance.RoundCloser(), value);
    //            m_FixedItems[++runner] = probability;
    //            value--;
    //        }
    //    }

    //    protected override Vector2 ComputeNoise(PickupCategory initial)
    //    {
    //        var roundUp = 1 << m_Random.Next(1, 10);
    //        return new((float)initial, (float)(TileAttributes)roundUp);
    //    }
    //}

    //public class WeightedSpritePool : ProbabilityPool<Sprite>
    //{
    //    public WeightedSpritePool(SpriteSheet value) : base(null)
    //    {
    //        SpriteSheet copy = value;
    //        Count = value.Sprites.Count;

    //        m_FixedItems = new Probability<Sprite>[Count];

    //        foreach (var sprite in copy.Sprites)
    //        {
    //            Vector2 noise = ComputeNoise(sprite);
    //            float chance = m_FastNoise.GetNoise(noise.X, noise.Y);
    //            Probability<Sprite> probability = new(chance.RoundCloser(), sprite);
    //            m_FixedItems[++runner] = probability;
    //        }

    //    }

    //    protected override Vector2 ComputeNoise(Sprite initial)
    //    {
    //        return initial.Origin;
    //    }
    //}

}