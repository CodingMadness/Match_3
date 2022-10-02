using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<ITile> matches;

    public MatchX(int matchCount)
    {
        matches = new HashSet<ITile?>(matchCount);
    }

    public virtual void Add(Tile matchTile)
    {
        if (!matches.Add(matchTile))
            return;

        MapRect = MapRect.Union((matchTile as ITile)!.Bounds);
        Match3Body ??= (matchTile.Body as TileShape)!.Clone() as TileShape;
        Count++;
    }
    public int Count { get; protected set; }
    public TileShape? Match3Body { get; private set; }
    public Rectangle MapRect { get; protected set; }
    public void Empty()
    {
        matches.Clear();
    }
    public EnemyMatches Transform(Grid map)
    {
        EnemyMatches list = new(Count, map);

        foreach (ITile match in matches)
        {
            //UndoBuffer.Add(match);
            map.Delete(match.Cell);
            map[match.Cell] = Bakery.Transform((match as Tile)!);
            list.Add((map[match.Cell] as Tile)!);
        }

        return list;
    }
}

public class EnemyMatches : MatchX
{
    private readonly Grid _map;

    public EnemyMatches(int matchCount, Grid map) : base(matchCount)
    {
        _map = map;
    }
    public override void Add(Tile matchTile)
    {
        if (matchTile is EnemyTile enemy1 && !matches.Add(enemy1))
            return;
        else if (matchTile is EnemyTile enemy2)
        {
            Count++;
            enemy2.BlockSurroundingTiles(_map, true);
        }
    }

    public void BuildBorder()
    {
        if (matches.Count == 0)
            return;

        Vector2 begin = -Vector2.One;
        int match3RectWidth = 0;
        int match3RectHeight = 0;
        
        if (matches.IsRowBased())
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            var first = matches.OrderBy(x => x.Cell.X).ElementAt(0);
            begin = first.Cell - Vector2.One;
            match3RectWidth = matches.Count + 2;
            match3RectHeight = matches.Count;
        }
        else
        {
            //its column based rectangle
            //-*--*--*--|
            // *  X  *  |
            // *  Y  *  |
            // *  Z  *  |
            // *  *  *  |
            //----------|
            var first = matches.OrderBy(x => x.Cell.Y).ElementAt(0);
            begin = first.Cell - Vector2.One;
            match3RectWidth = matches.Count;
            match3RectHeight = matches.Count+2;
        }
        MapRect = Utils.GetMatch3Rect(begin, match3RectWidth, match3RectHeight);
    }
}