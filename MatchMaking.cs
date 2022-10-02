using System.Numerics;
using Raylib_CsLo;

namespace Match_3;

public class MatchX
{
    protected readonly ISet<Tile> Matches;
    public bool IsRowBased => Matches.IsRowBased();
    public int Count => Matches.Count;
    public TileShape? Match3Body { get; private set; }
    public Rectangle MapRect { get; protected set; }
    public Vector2 Begin
    {
        get
        {
            if (MapRect.x == 0 && MapRect.y == 0)
                return Vector2.Zero;
            
            var tmp = MapRect with { x = MapRect.width / Count, y = MapRect.width / Count };
            return new Vector2(tmp.x, tmp.y);
        }
    }
    public MatchX(int matchCount)
    {
        Matches = new HashSet<Tile>(matchCount);
    }
    public virtual void Add(Tile matchTile)
    {
        if (Matches.Add(matchTile))
        {
            var body = matchTile!.Body;
            MapRect = MapRect.Union(body.Rect);
            Console.WriteLine(MapRect.ToStr());
            Match3Body ??= (body as TileShape)!.Clone() as TileShape;
        }
    }
    public void Empty()
    {
        Matches.Clear();
    }
    public EnemyMatches Transform(Grid map)
    {
        EnemyMatches list = new(Count, map);

        foreach (var match in Matches)
        {
            map[match.Cell] = Bakery.Transform(match!);
            list.Add((map[match.Cell] as EnemyTile)!);
        }
        return list;
    }
}

public class EnemyMatches : MatchX
{
    private readonly Grid _map;
    private Rectangle _border;

    public EnemyMatches(int matchCount, Grid map) : base(matchCount)
    {
        _map = map;
    }
    public override void Add(Tile enemyTile)
    {
        if (enemyTile is not EnemyTile e)
            return;
        
        if (Matches.Add(e))
        {
            e.BlockSurroundingTiles(_map, true);
        }
    }
    private Rectangle BuildBorder()
    {
        if (Matches.Count == 0)
            return default;

        Vector2 begin;
        int match3RectWidth = 0;
        int match3RectHeight = 0;
        
        if (Matches.IsRowBased())
        {
            //its row based rectangle
            //-----------------|
            // X     Y      Z  |
            //-----------------|
            var first = Matches.OrderBy(x => x.Cell.X).ElementAt(0);
            begin = first.Cell - Vector2.One;
            match3RectWidth = Matches.Count + 2;
            match3RectHeight = Matches.Count;
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
            var first = Matches.OrderBy(x => x.Cell.Y).ElementAt(0);
            begin = first.Cell - Vector2.One;
            match3RectWidth = Matches.Count;
            match3RectHeight = Matches.Count+2;
        }
        return Utils.GetMatch3Rect(begin, match3RectWidth, match3RectHeight);
    }
    public Rectangle Border
    {
        get
        {
            if (_border.x == 0 && _border.y == 0)
                _border = BuildBorder();

            return _border;
        }
    }
}