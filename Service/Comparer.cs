using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.Intrinsics;
using DotNext.Runtime;
using Match_3.DataObjects;

namespace Match_3.Service;

public static class Comparer
{
    public sealed class BodyComparer : EqualityComparer<Tile>
    {
        /// <summary>
        /// Since I derive this class from "EqualityComparer", I HAVE TO take Tile? params
        /// but I make sure that they NEVER can be null, it is illogical for that to ever happen
        /// and I make sure that I check that in the specific sections of code where needed but not here
        /// since it is not needed for every Equals() call and would waste senselessly performance
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override bool Equals(Tile? x, Tile? y)
        {
            return ReferenceEquals(x, y) || x!.Body.Equals(y!.Body);
        }

        public override int GetHashCode(Tile obj)
        {
            return HashCode.Combine(obj.Body);
        }

        public static readonly BodyComparer Singleton = new();
    }

    public class CellComparer : EqualityComparer<Tile>, IComparer<Tile>
    {
        public static readonly CellComparer Singleton = new();

        protected internal CellComparer()
        {}

        [Pure]
        public override bool Equals(Tile? x, Tile? y)
            => ReferenceEquals(x, y) || x!.GridCell == y!.GridCell;

        [Pure]
        public override int GetHashCode(Tile obj)
            => obj.GridCell.GetHashCode();
 
        [Pure]
        public virtual int Compare(Tile? x, Tile? y)
        {
            if (Equals(x, y))
                return 0;
            
            Intrinsics.Bitcast(x!.GridCell, out (float, float) tuple);
            Intrinsics.Bitcast(y!.GridCell, out (float, float) tuple2);
            
            return tuple.CompareTo(tuple2);
        }
    }

    public class DistanceComparer : CellComparer
    {
        private readonly float _toleratedDistance;
        
        public DistanceComparer(float toleratedDistance = 3.5f)
        {
            _toleratedDistance = toleratedDistance;
        }
        
        public override int Compare(Tile? x, Tile? y)
        {
            if (base.Equals(x, y))
                return 0;
            
            float distance = Vector2.Distance(x!.GridCell, y!.GridCell);
            int result;
            
            //to close! they cluster...
            if (distance <= _toleratedDistance)
            {
                result = -1;
                // Console.WriteLine($"Is {x!.GridCell} to close to {y!.GridCell}?   Range is: {distance}");
                return result;
            }

            result = 1;
            return result;
        }
        
        public bool? Are2Close(Tile? a, Tile? b)
        {
            //-1 => TOP/LEFT;
            // 1 => BOT/RIGHT
            bool? res = Compare(a, b) switch
            {
               -1 => true,   /*-1 means that the distance between x and y is < 3f (_tolerance) and hence to close!
                                so they will be sorted to the top of the collection!*/
                               
                1 => false,  /* 1 means its > 3f, so its enough space, so they will be sorted to the bottom of the collection!*/
               
                0 => null,
            };
            return res;
        }
    }
}