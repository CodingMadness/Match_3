//using DotNext;

using Match_3.GameTypes;
using Microsoft.Toolkit.HighPerformance;

namespace Match_3;

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
    private static Tile _lastMatchTrigger;
    private byte _match3FuncCounter;
    private bool _hasBeenSorted;
    
    public int TileWidth;
    public int TileHeight;
    private static readonly Dictionary<TileType, Stats> TypeStats = new((int)TileType.Length);

    public static event GridAction OnTileCreated;

    public static ref Stats GetStatsByType(TileType t)
    {
        ref var x = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeStats, t, out var existedB4);
        if (!existedB4) x = new();
        return ref x; 
    }

    public delegate void GridAction(Span<byte> countPerType);

    public static event GridAction NotifyOnGridCreationDone;
  
    public static Grid Instance { get; } = new();

    private Grid()
    {
        
    }
        
    private void CreateMap()
    {
        Span<byte> counts = stackalloc byte[(int)TileType.Length];
        
        for (int x = 0; x < TileWidth; x++)
        {
            for (int y = 2; y < TileHeight; y++)
            {
                Vector2 current = new(x, y);
                float noise = Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                var tile = _bitmap[x, y] = Bakery.CreateTile(current, noise);
                
                if (tile.Body.TileType is TileType.Empty or TileType.Length)
                    continue;
                
                GameState.Current = tile;
                OnTileCreated(Span<byte>.Empty);
                counts[(byte)tile.Body.TileType]++;
            }
        }
        
        NotifyOnGridCreationDone(counts[1..]);
    }
        
    public ref Stats GetTileStatsBy<T>(T key) where T : notnull
    {
        var map = _bitmap.AsSpan();
        ref var eventData = ref map[2].EventData;
        
        if (!_hasBeenSorted)
        {
            map.Sort();
            _hasBeenSorted = true;
        }

        var iterator = new FastSpanEnumerator<Tile>(map);
            
        foreach (Tile tile in iterator)
        {
            switch (tile)
            {
                case null:
                    continue;
                
                default:
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
                    break;
            }
        }
        return ref eventData;
    }

    public ref readonly Quest GetTileQuestBy<T>(T key) where T : notnull
    {
        var map = _bitmap.AsSpan();
        ref readonly var eventData = ref map[2].Quest;
        var iterator = new FastSpanEnumerator<Tile>(map);
        
        foreach (var tile in iterator)
        {
            switch (tile)
            {
                case null:
                    continue;
                
                default:
                    switch (key)
                    {
                        case Tile x:
                            if (x.Equals(tile))
                                eventData = ref tile.Quest;
                            break;
                        case TileType type:
                            if (tile.Body.TileType == type)
                                eventData = ref tile.Quest;
                            break;
                        case Vector2 pos:
                            if (tile.GridCell == pos)
                                eventData = ref tile.Quest;
                            break;
                        case TileShape body:
                            if (tile.Body == body)
                                eventData = ref tile.Quest;
                            break;
                    }
                    break;
            }
        }

        return ref eventData;
    }
        
    public void Init(Level current)
    {
        TileWidth = current.GridWidth;
        TileHeight = current.GridHeight-2;
        _bitmap = new Tile[TileWidth, TileHeight];
        CreateMap();
    }
        
    public Tile? this[Vector2 coord]
    {
        get
        {
            Tile? tmp = null;
                
            switch (coord.X)
            {
                case >= 0 when coord.X < TileWidth && coord.Y >= 0 && coord.Y < TileHeight:
                {
                    //its within bounds!
                    tmp = _bitmap[(int)coord.X, (int)coord.Y];
                    tmp = tmp is { IsDeleted: true } ? null : tmp;
                    break;
                }
            }

            return tmp;
        }
        set
        {
            _bitmap[(int)coord.X, (int)coord.Y] = coord.X switch
            {
                >= 0 when coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight => 
                    value ?? throw new NullReferenceException("You cannot store NULL inside the Grid anymore, use Grid.Delete(vector2) instead"),
                _ => _bitmap[(int)coord.X, (int)coord.Y]
            };
        }
    }

    public bool WasAMatchInAnyDirection(Tile match3Trigger, MatchX matches)
    {
        bool AddWhenEqual(Tile? first, Tile? next)
        {
            if (StateAndBodyComparer.Singleton.Equals(first, next))
            {
                switch (matches.Count)
                {
                    case Level.MAX_TILES_PER_MATCH:
                        return false;
                }

                matches.Add(first!);
                matches.Add(next!);
                return true;
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

        _lastMatchTrigger = match3Trigger;

        for (Direction i = 0; i < lastDir; i++)
        {
            Vector2 nextCoords = Next(_lastMatchTrigger.GridCell, i);
            var next = this[nextCoords]; //when a new tile is give back, the state == 0??

            while (AddWhenEqual(_lastMatchTrigger, next))
            {
                //compute the proper (x,y) for next round, because
                //we found a match between a -> b, now we check
                //a -> c and so on
                nextCoords = Next(nextCoords, i);
                next = this[nextCoords];
            }
        }

        switch (matches.IsMatchActive)
        {
            //if he could not get a match by the 2.tile which was clicked, try the 1.clicked tile!
            case false when ++_match3FuncCounter <= 1:
                matches.Clear();
                return WasAMatchInAnyDirection(this[_lastMatchTrigger.CoordsB4Swap]!, matches);
        }

        _match3FuncCounter = _match3FuncCounter switch
        {
            >= 1 => 0,
            _ => _match3FuncCounter
        };
        return matches.IsMatchActive;
    }
        
    public bool Swap(Tile? a, Tile? b)
    {
        if (a is null || b is null || 
            a.IsDeleted || b.IsDeleted)
        {
            return false;
        }
        if (a.Options.HasFlag(Options.UnMovable) ||
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
            var gridCell1 = match[i].GridCell; //works good!
            this[gridCell1]?.Disable(true);
        }
        match.Clear();
    }
}