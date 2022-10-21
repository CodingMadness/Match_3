//using DotNext;

using System.Numerics;
using System.Runtime.InteropServices;
using Match_3.GameTypes;
using Microsoft.Toolkit.HighPerformance;

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

        private Tile[,] _bitmap;

        public int TileWidth;
        public int TileHeight;
        private static readonly Dictionary<TileType, Stats> TypeStats = new((int)TileType.Length);

        public static event Action NotifyOnGridCreationDone;
        public static event Action OnTileCreated;

        public static Tile LastMatchTrigger { get; private set; }
        private byte _match3FuncCounter;

        public static ref Stats GetStatsByType(TileType t)
        {
            ref var x = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeStats, t, out var existedB4);
            if (!existedB4)
                x = new();
            return ref x; 
        }

        public static ref readonly Stats GetStatsByType2(TileType t)
        {
            ref readonly var x = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeStats, t, out _);
            return ref x;
        }
        
        public static Grid Instance { get; } = new();

        private Grid()
        {
            //Instance = new();
        }
        
        private void CreateMap()
        {
            Span<int> counts = stackalloc int[(int)TileType.Length];
                    
            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 2; y < TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                    var tile = _bitmap[x, y] = Bakery.CreateTile(current, noise);
                    Game.State.Current = tile;
                    OnTileCreated();
                    counts[(int)tile.Body.TileType]++;
                }
            }
            Game.State.TotalAmountPerType = counts.ToArray();
            NotifyOnGridCreationDone();
        }
        
        public ref Stats GetTileStatsBy<T>(T key) where T : notnull
        {
            var map = _bitmap.AsSpan();
            ref var eventData = ref map[2].EventData;
            var iterator = new SpanEnumerator<Tile>(map);
            
            foreach (var tile in iterator)
            {
                if (tile is null)
                    continue;
                
                switch (key)
                {
                    case Tile x:
                        if (x.Equals(tile))
                            eventData = ref tile.EventData;
                        break;
                    case TileType type:
                        if (tile.Body.TileType == type)
                            eventData = ref tile.EventData;
                        break;
                    case Vector2 pos:
                        if (tile.GridCell == pos)
                            eventData = ref tile.EventData;
                        break;
                    case TileShape body:
                        if (tile.Body == body)
                            eventData = ref tile.EventData;
                        break;
                }
            }
            return ref eventData;
        }

        public ref readonly Goal GetTileGoalBy<T>(T key) where T : notnull
        {
            var map = _bitmap.AsSpan();
            ref readonly var eventData = ref map[2].Goal;
            var iterator = new SpanEnumerator<Tile>(map);

            foreach (var tile in iterator)
            {
                if (tile is null)
                    continue;

                switch (key)
                {
                    case Tile x:
                        if (x.Equals(tile))
                            eventData = ref tile.Goal;
                        break;
                    case TileType type:
                        if (tile.Body.TileType == type)
                            eventData = ref tile.Goal;
                        break;
                    case Vector2 pos:
                        if (tile.GridCell == pos)
                            eventData = ref tile.Goal;
                        break;
                    case TileShape body:
                        if (tile.Body == body)
                            eventData = ref tile.Goal;
                        break;
                }
            }

            return ref eventData;
        }
        
        public void Init(Level current)
        {
            TileWidth = current.GridWidth;
            TileHeight = current.GridHeight;
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
                    tmp = tmp is { IsDeleted: true } ? null : tmp;
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
                        if (matches.Count == Level.MAX_TILES_PER_MATCH)
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
            if (!matches.IsMatchActive && ++_match3FuncCounter <= 1)
            {
                matches.Clear();
                return WasAMatchInAnyDirection(this[LastMatchTrigger.CoordsB4Swap]!, matches);
            }

            if (_match3FuncCounter >= 1)
            {
                _match3FuncCounter = 0;
            }
            return matches.IsMatchActive;
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
                //var gridCell0 = match.Move(i) ?? throw new Exception("why am i even thrown?"); //works not at al.... investigate
                var gridCell1 = match[i].GridCell; //works good!
                //Console.WriteLine(this[gridCell1]);
                this[gridCell1]?.Disable(true);
            }
            /*
            Console.WriteLine();
            foreach (var VARIABLE in match.Matches)
            {
                Console.WriteLine(VARIABLE);
            }
            */
            match.Clear();
        }
    }
}