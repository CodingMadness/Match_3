//using DotNext;

using System.Numerics;
using Match_3.GameTypes;

namespace Match_3
{
    public sealed class Grid
    {
        public enum Direction : byte
        {
            PositiveX = 0,
            NegativeX = 1,
            PositiveY = 2,
            NegativeY = 3,
        }
      
        private readonly Tile[,] _bitmap;

        //public double Timer { get; private set; } = 10;
        public readonly int TileWidth;
        public readonly int TileHeight;

        public static event Action<GameState> NotifyOnGridCreationDone;

        private const int MaxDestroyableTiles = 3;

        public static Tile LastMatchTrigger { get; private set; }
        private byte _match3FuncCounter;

        private void CreateMap()
        {
            Span<int> counts = stackalloc int[(int)Type.Length];

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = TileHeight/3; y < TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                    _bitmap[x, y] = Bakery.CreateTile(current, noise);
                    var kind = _bitmap[x, y] is { Body: TileShape c } ? c.TileType : Type.Empty;
                    counts[(int)kind]++;
                }
            }
            Game.State.TotalAmountPerType = counts.ToArray();
            NotifyOnGridCreationDone(Game.State);
        }

        public Grid(Level current)
        {
            TileWidth = current.GridWidth;
            TileHeight = current.GridHeight-3;
            _bitmap = new Tile[TileWidth, TileHeight];
            CreateMap();
        }
        
        
        public Tile? this[Vector2 coord]
        {
            get
            {
                Tile? tmp = null;
                if (coord.X >= 0 && coord.X < TileWidth
                                 && coord.Y >= 0 && coord.Y < TileHeight)
                {
                    //its within bounds!
                    tmp = _bitmap[(int)coord.X, (int)coord.Y];
                    tmp = tmp is null || tmp.IsDeleted ? null : tmp;
                }

                return tmp;
            }
            set
            {
                if (coord.X >= 0 && coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight)
                {
                    _bitmap[(int)coord.X, (int)coord.Y] = value ?? throw new NullReferenceException(
                        "You cannot store NULL inside the Grid anymore, use Grid.Delete(vector2) instead");
                }
            }
        }

        public bool WasAMatchInAnyDirection(Tile match3Trigger, MatchX matches)
        {
            bool AddWhenEqual(Tile? first, Tile? next)
            {
                if (first is not null && next is not null /*&& first.GridCell.GetDirectionTo(next.GridCell)*/)
                {
                    if (StateAndBodyComparer.Singleton.Equals(first, next))
                    {
                        if (matches.Count == MaxDestroyableTiles)
                            return false;

                        matches.Add(first);
                        matches.Add(next);
                        return true;
                    }
                }
                return false;
            }

            static Vector2 Next(Vector2 input, Direction direction)
            {
                var tmp = direction switch
                {
                    Direction.NegativeX => input with { X = input.X - 1 },
                    Direction.PositiveX => input with { X = input.X + 1 },
                    Direction.NegativeY => input with { Y = input.Y - 1 },
                    Direction.PositiveY => input with { Y = input.Y + 1 },
                    _ => Vector2.Zero
                };

                return tmp;
            }

            const Direction lastDir = (Direction)4;

            LastMatchTrigger = match3Trigger;

            for (Direction i = 0; i < lastDir; i++)
            {
                Vector2 nextCoords = Next(LastMatchTrigger.GridCell, i);
                var next = this[nextCoords]; //when a new tile is give back, the state == 0??

                while (AddWhenEqual(LastMatchTrigger, next))
                {
                    //compute the proper (x,y) for next round, because
                    //we found a match between a -> b, now we check
                    //a -> c and so on
                    nextCoords = Next(nextCoords, i);
                    next = this[nextCoords];
                }
            }
            //if he could not get a match by the 2.tile which was clicked, try the 1.clicked tile!
            if (matches.Count < MaxDestroyableTiles && ++_match3FuncCounter <= 1)
            {
                matches.Clear();
                return WasAMatchInAnyDirection(this[LastMatchTrigger.CoordsB4Swap]!, matches);
            }

            if (_match3FuncCounter >= 1)
            {
                _match3FuncCounter = 0;
            }
            return matches.Count == MaxDestroyableTiles;
        }
        
        public bool Swap(Tile? a, Tile? b)
        {
            if (a is null || b is null || 
                a.IsDeleted || b.IsDeleted)
            {
                return false;
            }
            if ((a.Options & Options.UnMovable) == Options.UnMovable ||
                (b.Options & Options.UnMovable) == Options.UnMovable)
                return false;
            
            this[a.GridCell] = b;
            this[b.GridCell] = a;
            a.CoordsB4Swap = a.GridCell;
            b.CoordsB4Swap = b.GridCell;
            (a.GridCell, b.GridCell) = (b.GridCell, a.GridCell);
            return true;
        }

        public void Delete(MatchX match)
        {
            for (int i = 0; i <  match.Count; i++)
            {
                var begin = match.Move(i);
                this[begin / Tile.Size]?.Disable(true);
            }
            match.Clear();
        }
    }
}