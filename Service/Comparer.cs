using System.Diagnostics.Contracts;
using System.Numerics;
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
        private readonly bool _orderByColumns;
        public static readonly CellComparer Singleton = new();

        protected CellComparer(bool orderByColumns = true)
        {
            _orderByColumns = orderByColumns;
        }

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

            //based on Grid logic, there are only integer cells, but since using 
            //Numerics.Vector2, I have to cast them from float to int,
            //because they are by default floats
            Intrinsics.Bitcast(x!.GridCell, out (int x0, int y0) tuple0);
            Intrinsics.Bitcast(y!.GridCell, out (int x1, int y1) tuple1);

            //So, when "_orderByColumns" is true, we first consider "x" only if x1==x2 then 
            //then we only do care for those x values because it
            //doesnt matter if y is different, x is sufficient then
            //but IF x1==x2 THEN we care for y and we check for that, 
            int result;
            bool orderByRows = !_orderByColumns;

            if (_orderByColumns)
            {
                if ((result=tuple0.x0.CompareTo(tuple1.x1)) == 0)
                    return tuple0.y0.CompareTo(tuple1.y1);
                else
                    return result;
            }
            //we have to order first by rows!
            if (orderByRows)
            {
                if ((result=tuple0.y0.CompareTo(tuple1.y1)) == 0)
                    return tuple0.x0.CompareTo(tuple1.x1);
                else
                    return result;
            }

            throw new ArgumentException($"something went heavily wrong inside: CellComparer.CompareTo(x, y)...");
        }
    }

    public sealed class DistanceComparer : CellComparer
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
                // x.NodeCount++;
                //y.NodeCount++;
                return result;
            }

            result = 1;
            
            return result;
        }

        public bool? Are2Close(Tile? x, Tile? y)
        {
            //-1 => TOP/LEFT;
            // 1 => BOT/RIGHT
            bool? res = Compare(x, y) switch
            {
                -1 => true, /*-1 means that the distance between x and y is < 3f (_tolerance) and hence to close!
                                 so they will be sorted to the top of the collection!*/

                1 => false, /* 1 means its > 3f, so its enough space, so they will be sorted to the bottom of the collection!*/

                0 => null,
            };
            return res;
        }
    }
}