//using DotNext;

using System.Numerics;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using DotNext.Runtime;
using Match_3.Service;
using Match_3.StateHolder;

namespace Match_3.Workflow;

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
    private static readonly Dictionary<TileColor, EventState> TypeStats = new(Utils.TileColorLen);
    
    public static ref EventState GetStatsByType(TileColor t)
    {
        ref var x = ref CollectionsMarshal.GetValueRefOrAddDefault(TypeStats, t, out var existedB4);
        if (!existedB4) x = new();
        return ref x; 
    }

    public delegate void GridAction(Span<byte> countPerType);

    public static event GridAction NotifyOnGridCreationDone;
    
    public static event GridAction OnTileCreated;
  
    public static Grid Instance { get; } = new();

    private Grid()
    {
        
    }
        
    private void CreateMap()
    {
        Span<byte> counts = stackalloc byte[Utils.TileColorLen];
        
        for (int x = 0; x < TileWidth; x++)
        {
            for (int y = 0; y < TileHeight; y++)
            {
                Vector2 current = new(x, y);
                var img = GenImagePerlinNoise(TileWidth, TileHeight, x, y, 0.89f);
                var f = LoadTextureFromImage(img);
                //Utils.NoiseMaker.GetNoise(x * -0.5f, y * -0.5f);
                Intrinsics.Bitcast(f, out float noise);
                var tile = _bitmap[x, y] = Bakery.CreateTile(current, noise);
                //EventStats.TileX = tile;
                //we yet dont care for side quests and hence we dont need to keep track of ALL tiles, only match-based information
                /*OnTileCreated(Span<byte>.Empty);*/
                int index = tile.Body.TileColor.ToIndex();
                counts[index]++;
            }
        }
        
        NotifyOnGridCreationDone(counts[1..]);
    }
        
    public ref EventState GetTileStatsBy<T>(T key) where T : notnull
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
                        case TileColor type:
                            if (tile.Body.TileColor == type)
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
                        case TileColor type:
                            if (tile.Body.TileColor == type)
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
        TileHeight = current.GridHeight;
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
                    case Level.MaxTilesPerMatch:
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

        if (!matches.IsMatchActive &&
            //if he could not get a match by the 2.tile which was clicked, try the 1.clicked tile!
            ++_match3FuncCounter <= 1)
        {
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