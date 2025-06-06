﻿using System.Diagnostics;
using Match_3.DataObjects;
using Match_3.Service;
using Match_3.Setup;

namespace Match_3.Workflow;

public static class TileMap
{
    private static Tile[,] _map = null!;
    private static Tile? _lastMatchTrigger;
    private static byte _match3FuncCounter;
    private static int _tileWidth, _tileHeight;

    private static void CreateMap()
    {
        const byte ySize = 4, xSize = 3;
        scoped Span<byte> counts = stackalloc byte[Config.TileColorCount];
        scoped Span<TileColorTypes> allKinds = stackalloc TileColorTypes[Config.TileColorCount];
        Vector2 twoBy4Block = new(xSize, ySize);
        Vector2 begin = new(0, 0);
        FadeableColor.Fill(allKinds);
        int j = 0;
        
        Next8Block:
        for (int x = (int)begin.X; x < twoBy4Block.X-1; x++)
        {
            for (int y = (int)begin.Y; y < twoBy4Block.Y-1; y++)
            {
                Vector2 current = new(x, y);
                SingleCell cell = new Vector2(current.X, current.Y);
                Tile tmpTile = _map[x, y] = Bakery.CreateTile(cell, allKinds[j++]);
                int index = FadeableColor.ToIndex(tmpTile.Body.Colour.Type);
                counts[index]++;
            }
        }
        
        //6rows => 6x2 = 12 tiles in X, but since we are beginning from 0,
        //we have to -2 like with array index
        if (begin.X < _tileWidth - xSize)  
        {
            begin.X = twoBy4Block.X;
            twoBy4Block.X += xSize;
            allKinds.Shuffle();
            j = 0;
            goto Next8Block;  
        }
        //3columns => 3x4 = 12 tiles in Y, but since we are beginning from 0,
        //we have to -4 like with array index
        if (begin.Y < _tileHeight - ySize)
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
    }
    
    public static void Init() //--> RECEIVER!
    {
        // ----> The "NOTIFIER" has to DECLARE the event-type!  AND has to invoke() it in his own code-base somewhere!
        // ----> The "RECEIVER" has to REGISTER the event AND handle with appropriate code logic the specific event-case!
        //--> registrations/subscriptions to events!
        ClickHandler.Instance.OnSwapTiles += Swap;
        SwapHandler.Instance.OnCheckForMatch += CheckForMatch;
        //MatchHandler.Instance.OnDeleteMatch += Delete;
        _tileWidth = Game.ConfigPerStartUp.GridWidth;
        _tileHeight = Game.ConfigPerStartUp.GridHeight;
        _map = new Tile[_tileWidth, _tileHeight];
        CreateMap();
        Console.Clear();
    }

    private static void CheckForMatch()
    {
        var state = GameState.Instance;
        state.HaveAMatch = WasAMatchInAnyDirection();
        Debug.WriteLine($"CheckForMatch() => returns: {state.HaveAMatch}");
    }

    /// <summary>
    /// Gets you a @tile based on the cell-coordinates,
    /// if it is marked as 'deleted' or 'disabled', then you get back null!
    /// </summary>
    /// <param name="cellPos">the cell to pass, from (0,0) -> (_tileWidth-1, _tileHeight-1)</param>
    /// <returns></returns>
    public static Tile? GetTile(Vector2 cellPos)
    {
        Tile? tmp = null;

        switch (cellPos.X)
        {
            case >= 0 when cellPos.X < _tileWidth && cellPos.Y >= 0 && cellPos.Y < _tileHeight:
            {
                //it's within bounds!
                tmp = _map[(int)cellPos.X, (int)cellPos.Y];
                tmp = tmp is { IsDeleted: true, State: TileState.Disabled  } ? null : tmp;
                break;
            }
        }

        return tmp;
    }

    private static void SetTile(Tile value, Vector2? newCoord = null)
    {
        if (value is null)
            throw new ArgumentException("you cannot Add a NULL @tile! check your @tile-creation logic!");

        Vector2 coord = newCoord ?? value.Cell.Start;

        _map[(int)coord.X, (int)coord.Y] = coord.X switch
        {
            >= 0 when coord.Y >= 0 && coord.X < _tileWidth && coord.Y < _tileHeight
                => value ?? throw new NullReferenceException(
                    "You cannot store NULL inside the EntireGrid anymore, use EntireGrid.Delete(vector2) instead"),
            _ => _map[(int)coord.X, (int)coord.Y]
        };
    }

    private static bool WasAMatchInAnyDirection()
    {
        var matchData = GameState.Instance;
        var matches = matchData.Matches;
        const Direction last = Direction.DiagonalBotRight;
        _lastMatchTrigger = matchData.TileX;
       
        bool Add(Tile? first, Tile? next)
        {
            if (first is not null && next is not null &&
                Comparer.BodyComparer.Singleton.Equals(first, next))
            {
                matches.Add(first);
                matches.Add(next);
                return true;
            }

            return false;
        }

        static Vector2 GetNextCell(Vector2 input, Direction direction)
        {
            Vector2 tmp = direction switch
            {
                Direction.Left => input with { X = input.X - 1 },
                Direction.Right => input with { X = input.X + 1 },
                Direction.Bot => input with { Y = input.Y - 1 },
                Direction.Top => input with { Y = input.Y + 1 },
                Direction.DiagonalTopLeft => input with { X = input.X - 1, Y = input.Y - 1 },
                Direction.DiagonalTopRight => input with { X = input.X + 1, Y = input.Y - 1 },
                Direction.DiagonalBotLeft => input with { X = input.X - 1, Y = input.Y + 1 },
                Direction.DiagonalBotRight => input with { X = input.X + 1, Y = input.Y + 1 },
                _ => Vector2.Zero
            };

            return tmp;
        }

        if (matches.Count == Config.MaxTilesPerMatch)
            return false;
        
        for (Direction i = Direction.Right; i < last; i++)
        {
            Vector2 nextCoords = GetNextCell(_lastMatchTrigger!.Cell.Start, i);
            var next = GetTile(nextCoords);

            while (Add(_lastMatchTrigger, next))
            {
                matchData.LookUpUsedInMatchFinder = i;
                //compute the proper (x,y) for next round, because
                //we found a match between a -> b, now we check
                //a -> c and so on
                nextCoords = GetNextCell(nextCoords, i);
                next = GetTile(nextCoords);
            }
        }

        //if he could not get a match by the 2.@tile which was clicked on, try the 1.clicked @tile!
        if (!matches.IsMatchFilled && ++_match3FuncCounter <= 1)
        {
            matches.Clear(matchData.LookUpUsedInMatchFinder);
            matchData.TileX = GetTile(_lastMatchTrigger!.CellB4Swap.Start);
            matches.FirstInOrder = matchData.TileX!;
            return WasAMatchInAnyDirection();
        }

        matches.FirstInOrder = matchData.TileX!;
        
        _match3FuncCounter = _match3FuncCounter switch
        {
            >= 1 => 0,
            _ => _match3FuncCounter
        };
        
        return matches.IsMatchFilled;
    }

    private static void Swap()
    {
        var currData = GameState.Instance;

        Tile? a = currData.TileX!,
              b = currData.TileY!;

        if (a.IsDeleted || b.IsDeleted)
        {
            currData.WasSwapped = false;
            return;
        }
        
        SetTile(b, a.Cell.Start);
        SetTile(a, b.Cell.Start);
        a.CellB4Swap = a.Cell;
        b.CellB4Swap = b.Cell;
        (a.Cell, b.Cell) = (b.Cell, a.Cell);
        currData.WasSwapped = true;
    }
}