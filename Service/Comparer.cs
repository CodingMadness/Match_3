using System.Diagnostics.Contracts;
using Match_3.DataObjects;

namespace Match_3.Service;

public static class Comparer
{
    public sealed class BodyComparer : EqualityComparer<IGameObject>
    {
        /// <summary>
        /// Since I derive this class from "EqualityComparer", I HAVE TO take IGameObject? params,
        /// but I make sure that they NEVER can be null, it is illogical for that to ever happen,
        /// and I make sure that I check that in the specific sections of code where needed but not here
        /// since it is unnecessary for every 'Equals()' call and would waste senseless performance
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override bool Equals(IGameObject? x, IGameObject? y)
        {
            return ReferenceEquals(x, y) || x!.Body.Colour.Type == y!.Body.Colour.Type;
        }

        public override int GetHashCode(IGameObject obj)
        {
            return obj.GetHashCode();
        }

        public static readonly BodyComparer Singleton = new();
    }

    public class CellComparer : EqualityComparer<IGameObject>, IComparer<IGameObject>
    {
        private readonly bool _orderByColumns;
        public static readonly CellComparer Singleton = new();

        protected CellComparer(bool orderByColumns = true)
        {
            _orderByColumns = orderByColumns;
        }

        [Pure]
        public override bool Equals(IGameObject? x, IGameObject? y) => ReferenceEquals(x, y) || x!.Position == y!.Position;

        [Pure]
        public override int GetHashCode(IGameObject obj) => obj.Position.GetHashCode();

        [Pure]
        public virtual int Compare(IGameObject? x, IGameObject? y)
        {
            if (Equals(x, y))
                return 0;

            //based on EntireGrid logic, there are only integer cells, but since using 
            //Numerics.Vector2, I have to cast them from float to int,
            //because they are by default floats

            (int x0, int y0) = ((int)x!.Position.X, (int)x.Position.Y);
            (int x1, int y1) = ((int)y!.Position.X, (int)y.Position.Y);

            //So, when "_orderByColumns" is true, we first consider "x" only if x1==x2 then
            // we only do care for those x values because it
            //doesn't matter if y is different, x is sufficient then
            //but IF x1==x2 THEN we care for y and we check for that, 
            int result;
            bool orderByRows = !_orderByColumns;

            if (_orderByColumns)
            {
                if ((result=x0.CompareTo(x1)) == 0)
                    return y0.CompareTo(y1);
                return result;
            }
            //we have to order it first by rows!
            if (orderByRows)
            {
                if ((result=y0.CompareTo(y1)) == 0)
                    return x0.CompareTo(x1);
                return result;
            }

            throw new ArgumentException("something went heavily wrong inside: CellComparer.CompareTo(x, y)...");
        }
    }

    public sealed class DistanceComparer(float toleratedDistance = 3.5f) : CellComparer
    {
        public override int Compare(IGameObject? x, IGameObject? y)
        {
            if (Equals(x, y))
                return 0;

            float distance = Vector2.Distance(x!.Position, y!.Position);
            int result;

            //to close! they cluster...
            if (distance <= toleratedDistance)
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

        public bool? Are2Close(IGameObject? x, IGameObject? y)
        {
            //-1 => TOP/LEFT;
            // 1 => BOT/RIGHT
            bool? res = Compare(x, y) switch
            {
                < 0 => true,    /*-1 means that the distance between x and y is < 3.5f (_tolerance) and hence to close!
                                 so they will be sorted to the top of the collection!*/

                > 0 => false,   /* 1 means its > 3f, so its enough space, so they will be sorted to the bottom of the collection!*/

                0 => null,      /* are same, hence no action needed thats why null is returned */
            };
            return res;
        }
    }

    public sealed class EdgeComparer : EqualityComparer<TileGraph.Node>, IComparer<TileGraph.Node>
    {
        private EdgeComparer() { }
        
        public static readonly EdgeComparer Singleton = new();
        
        public override bool Equals(TileGraph.Node? x, TileGraph.Node? y)
        {
            return ReferenceEquals(x, y) ||
                   ReferenceEquals(x!.Root, y!.Root) || 
                   x.Edges == y.Edges;
        }

        public override int GetHashCode(TileGraph.Node obj)
        {
            return obj.Edges;
        }

        public int Compare(TileGraph.Node? x, TileGraph.Node? y)
        {
            return Equals(x, y) 
                ? 0 
                : x!.Edges.CompareTo(y!.Edges);
        }
    }
}