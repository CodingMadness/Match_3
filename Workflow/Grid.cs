//using DotNext;

using System.Diagnostics;
using System.Numerics;
using DotNext;
using DotNext.Runtime;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Setup;

namespace Match_3.Workflow;

public static class Grid
{
    public enum Direction : byte
    {
        PositiveX = 0,
        NegativeX = 1,
        PositiveY = 2,
        NegativeY = 3,
    }

    private static Tile[,]? _bitmap;
    private static Tile? _lastMatchTrigger;
    private static byte _match3FuncCounter;
    private static int TileWidth;
    private static int TileHeight;
    
    public delegate void GridAction(Span<byte> countPerType);

    public static event GridAction? NotifyOnGridCreationDone;

    private static void CreateMap()
    {
        void Fill(Span<TileColor> toFill)
        {
            for (int i = 0; i < Utils.TileColorLen; i++)
                toFill[i] = i.ToColor();
        }

        const byte ySize = 4, xSize = 2;
        Span<byte> counts = stackalloc byte[Utils.TileColorLen];
        Span<TileColor> allKinds = stackalloc TileColor[Utils.TileColorLen];
        Vector2 twoBy4Block = new(xSize, ySize);
        Vector2 begin = new(0, 0);
        Fill(allKinds);
        int j = 0;

        Next8Block:
        for (int x = (int)begin.X; x < twoBy4Block.X; x++)
        {
            for (int y = (int)begin.Y; y < twoBy4Block.Y; y++)
            {
                Vector2 current = new(x, y);
                Tile tmpTile = _bitmap![x, y] = Bakery.CreateTile(current, allKinds[j++]);
                int index = tmpTile.Body.TileKind.ToIndex();
                counts[index]++;
            }
        }
        //6rows => 6x2 = 12 tiles in X, but since we are beginning from 0,
        //we have to -2 like with array index
        if (begin.X < TileWidth - xSize)  
        {
            begin.X = twoBy4Block.X;
            twoBy4Block.X += xSize;
            allKinds.Shuffle();
            j = 0;
            goto Next8Block;  
        }
        //3columns => 3x4 = 12 tiles in Y, but since we are beginning from 0,
        //we have to -4 like with array index
        else if (begin.Y < TileHeight - ySize)
        {
            //set begin to point to (x=0, y=4)
            begin.X = 0;
            begin.Y = twoBy4Block.Y;
            twoBy4Block.X = xSize;
            twoBy4Block.Y += ySize;
            allKinds.Shuffle();
            j = 0;
            goto Next8Block;  
        }
        bool s = true;
        NotifyOnGridCreationDone?.Invoke(counts[1..]);
    }

    private static float GetWeightedNoise(Vector2 coord, float scale = 4f)
    {
        // double perlinValue = fn.Perlin(x / scale, y / scale);
        float perlinValue = Utils.NoiseMaker.GetWhiteNoise(coord.X / scale, coord.Y / scale);
        // Convert Perlin value from [-1, 1] to [0, 1]
        float normalizedValue = (perlinValue + 1f) / 2.0f;
        return normalizedValue;
    }

    public static void Init() //--> RECEIVER!
    {
        // ----> The "NOTIFIER" has to DECLARE the event-type!  AND has to invoke() it in his own code-base somewhere!
        // ----> The "RECEIVER" has to REGISTER the event AND handle with appropriate code logic the specific event-case!

        //--> registrations/subscriptions to events!
        ClickHandler.Instance.OnSwapTiles += Swap;
        SwapHandler.Instance.OnCheckForMatch += CheckForMatch;
        MatchHandler.Instance.OnDeleteMatch += Delete;
        var current = GameState.CurrentLvl!;
        TileWidth = current.GridWidth;
        TileHeight = current.GridHeight;
        _bitmap = new Tile[TileWidth, TileHeight];
        CreateMap();
    }

    private static void CheckForMatch()
    {
        var state = GameState.CurrData!;
        state.WasMatch = WasAMatchInAnyDirection();
        Debug.WriteLine($"CheckForMatch() => returns: {state.WasMatch}");
    }

    public static Tile? GetTile(Vector2 coord)
    {
        Tile? tmp = null;

        switch (coord.X)
        {
            case >= 0 when coord.X < TileWidth && coord.Y >= 0 && coord.Y < TileHeight:
            {
                //its within bounds!
                tmp = _bitmap![(int)coord.X, (int)coord.Y];
                tmp = tmp is { IsDeleted: true } ? null : tmp;
                break;
            }
        }

        return tmp;
    }

    public static void SetTile(Tile? value, Vector2? newCoord = null)
    {
        if (value is null)
            throw new ArgumentException("you cannot Add a NULL tile! check your tile-creation logic!");

        Vector2 coord = newCoord ?? value.GridCell;

        _bitmap![(int)coord.X, (int)coord.Y] = coord.X switch
        {
            >= 0 when coord.Y >= 0 && coord.X < TileWidth && coord.Y < TileHeight
                => value ?? throw new NullReferenceException(
                    "You cannot store NULL inside the Grid anymore, use Grid.Delete(vector2) instead"),
            _ => _bitmap[(int)coord.X, (int)coord.Y]
        };
    }

    private static bool WasAMatchInAnyDirection()
    {
        var dataForMatchLogic = GameState.CurrData!;
        var matches = dataForMatchLogic.Matches!;
        const Direction lastDir = (Direction)4;
        _lastMatchTrigger = dataForMatchLogic.TileX!;

        bool Add2MatchesWhenEqual(Tile? first, Tile? next)
        {
            if (Comparer.StateAndBodyComparer.Singleton.Equals(first, next))
            {
                matches.Add(first!);
                matches.Add(next!);
                return true;
            }

            return false;
        }

        static Vector2 GetNextCell(Vector2 input, Direction direction)
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

        if (matches.Count == Level.MaxTilesPerMatch)
            return false;

        for (Direction i = 0; i < lastDir; i++)
        {
            Vector2 nextCoords = GetNextCell(_lastMatchTrigger.GridCell, i);
            var next = GetTile(nextCoords);

            while (Add2MatchesWhenEqual(_lastMatchTrigger, next))
            {
                //compute the proper (x,y) for next round, because
                //we found a match between a -> b, now we check
                //a -> c and so on
                nextCoords = GetNextCell(nextCoords, i);
                next = GetTile(nextCoords);
            }
        }

        if (!matches.IsMatchActive &&
            //if he could not get a match by the 2.tile which was clicked on, try the 1.clicked tile!
            ++_match3FuncCounter <= 1)
        {
            matches.Clear();
            dataForMatchLogic.TileX = GetTile(_lastMatchTrigger.CoordsB4Swap);
            return WasAMatchInAnyDirection();
        }

        _match3FuncCounter = _match3FuncCounter switch
        {
            >= 1 => 0,
            _ => _match3FuncCounter
        };
        return matches.IsMatchActive;
    }

    private static void Swap()
    {
        var currData = GameState.CurrData!;

        Tile? a = currData.TileX,
            b = currData.TileY;

        if (a is null || b is null ||
            a.IsDeleted || b.IsDeleted)
        {
            currData.WasSwapped = false;
            return;
        }

        if (a.Options.HasFlag(Options.UnMovable) ||
            (b.Options & Options.UnMovable) == Options.UnMovable)
        {
            currData.WasSwapped = false;
            return;
        }

        SetTile(b, a.GridCell);
        SetTile(a, b.GridCell);
        a.CoordsB4Swap = a.GridCell;
        b.CoordsB4Swap = b.GridCell;
        (a.GridCell, b.GridCell) = (b.GridCell, a.GridCell);
        currData.WasSwapped = true;
    }

    private static void Delete()
    {
        var match = GameState.CurrData!.Matches;

        for (int i = 0; i < match!.Count; i++)
        {
            var gridCell1 = match[i].GridCell; //works good!
            GetTile(gridCell1)?.Disable(true);
        }

        match.Clear();
    }
}