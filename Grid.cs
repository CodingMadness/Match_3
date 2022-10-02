﻿//using DotNext;

using System.Numerics;
using Match_3.GameTypes;
using Raylib_CsLo;
using Rectangle = System.Drawing.Rectangle;

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
      
        private readonly ITile[,] _bitmap;

        //public double Timer { get; private set; } = 10;
        public readonly int TileWidth;
        public readonly int TileHeight;

        public static event Action<int[]> NotifyOnGridCreationDone;

        private const int MaxDestroyableTiles = 3;

        public static ITile LastMatchTrigger { get; private set; }
        private byte _match3FuncCounter;

        private void CreateMap()
        {
            Span<int> counts = stackalloc int[(int)Type.Length];

            for (int x = 0; x < TileWidth; x++)
            {
                for (int y = 0; y < TileHeight; y++)
                {
                    Vector2 current = new(x, y);
                    float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                    _bitmap[x, y] = Bakery.CreateTile(current, noise);
                    var kind = _bitmap[x, y] is Tile { Body: TileShape c } ? c.Ball : Type.Empty;
                    //Console.WriteLine(( _bitmap[x, y] as Tile).State);
                    counts[(int)kind]++;
                }
            }
            NotifyOnGridCreationDone(counts.ToArray());
        }

        public (int start, int end) MakePlaceForTimer()
        {
            float beginX = (TileWidth - 1) / 2f;
            this[new(beginX-1, 0)] = null;
            this[new(beginX-0, 0)] = null;
            this[new(beginX+1, 0)] = null;
            this[new(beginX+2, 0)] = null;
            
            int start = (int)(beginX-1) * ITile.Size;
            int end = (int)(beginX+3) * ITile.Size;
            return (start, end);
        }
        
        public Grid(Level current)
        {
            TileWidth = current.TilemapWidth;
            TileHeight = current.TilemapHeight;
            _bitmap = new ITile[TileWidth, TileHeight];
            NotifyOnGridCreationDone += GameRuleManager.SetCountPerType;
            CreateMap();
        }
        
        public ITile? this[Vector2 coord]
        {
            get
            {
                ITile? tmp = null;
                if (coord.X >= 0 && coord.X < TileWidth
                                 && coord.Y >= 0 && coord.Y < TileHeight)
                {
                    //its within bounds!
                    tmp = _bitmap[(int)coord.X, (int)coord.Y];
                    tmp = tmp.IsDeleted ? null : tmp;
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

        public bool WasAMatchInAnyDirection(ITile match3Trigger, MatchX<ITile> matches)
        {
            static bool AddWhenEqual(ITile first, ITile next, MatchX<ITile> matches)
            {
                if (first is Tile f && next is Tile n)
                {
                    if (StateAndBodyComparer<ITile>.Singleton.Equals(f, n))
                    {
                        if (matches.Count == MaxDestroyableTiles)
                            return false;

                        matches.Add(f);
                        matches.Add(n);
                        return true;
                    }
                }
                return false;
            }

            static Vector2 NextFrom(Vector2 input, Direction direction)
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
                Vector2 nextCoords = NextFrom(LastMatchTrigger.Cell, i);
                var next = this[nextCoords]; //when a new tile is give back, the state == 0??

                while (AddWhenEqual(LastMatchTrigger, next, matches))
                {
                    //compute the proper (x,y) for next round, because
                    //we found a match between a -> b, now we check
                    //a -> c and so on
                    nextCoords = NextFrom(nextCoords, i);
                    next = this[nextCoords];
                }
            }
            //if he could not get a match by the 2.tile which was clicked, try the 1.clicked tile!
            if (matches.Count < MaxDestroyableTiles && ++_match3FuncCounter <= 1)
            {
                matches.Empty();
                return WasAMatchInAnyDirection(this[LastMatchTrigger.CoordsB4Swap]!, matches);
            }

            if (_match3FuncCounter >= 1)
            {
                _match3FuncCounter = 0;
            }
            return matches.Count == MaxDestroyableTiles;
        }
        
        public bool Swap(ITile? a, ITile? b)
        {
            if (a is null || b is null || a.IsDeleted || b.IsDeleted)
            {
                return false;
            }
            if ((a.Options & Options.UnMovable) == Options.UnMovable ||
                (b.Options & Options.UnMovable) == Options.UnMovable)
                return false;
            
            this[a.Cell] = b;
            this[b.Cell] = a;
            a.CoordsB4Swap = a.Cell;
            b.CoordsB4Swap = b.Cell;
            (a.Cell, b.Cell) = (b.Cell, a.Cell);
            return true;
        }

        public void Delete(MatchX<ITile> match)
        {
            Vector2 begin = match.Begin;
            
            for (int i = 0; i < match.Count; i++)
            {
                (this[begin] as Tile)!.Disable(true);
                begin = match.MapRect.Move(match.IsRowBased).ToWorldCoord();
            }
        }
    }
}