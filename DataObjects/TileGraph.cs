using System.Collections;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using DotNext.Collections.Generic;

namespace Match_3.DataObjects;

/// <summary>
/// This class shall track which Tile(KEY) has to close neighbors(VALUES) which shall be moved away!
/// </summary>
public class TileGraph : IEnumerable<Tile>
{
    public class Node : IGameObject
    {
        private const int MaxEdges = 4;
        public readonly Tile Root;
        public readonly List<Node> Links = new(MaxEdges);
        /// <summary>
        /// Describes the amount of how many connections to close neighbors it got
        /// </summary>
        public int Edges;

        public Node(Tile root)
        {
            Root = root;
        }

        public override string ToString() => ((IGameObject)this).ToString();
        
        public void CutLink()
        {
            int i = 0;
            foreach (var node in Links)
            {
                node.Edges--;
                node.Links.Remove(this);
                Links.AsSpan()[i++] = null!;
            }
            Edges = 0;
        }

        public SingleCell Cell => Root.Cell;
        
        Vector2 IGameObject.Position => Root.Cell.Start;
        
        Shape IGameObject.Body => Root.Body;
    }

    private readonly Node[] _sameColored;
    private readonly Comparer.DistanceComparer _distanceComparer;

    public TileGraph(Tile[,] bitmap, TileColor color)
    {
        _sameColored = [.. bitmap
            .OfType<Tile>()
            .Where(x => x.Body.Colour.Type == color)
            .OrderBy(x => x, Comparer.CellComparer.Singleton)
            .Select(x => new Node(x))];

        _distanceComparer = new();
    }

    private IEnumerable<IEnumerable<Node>> AddEdges()
    {
        static void UpdateNode(Node first, Node second)
        {
            first.Edges++;
            first.Links.Add(second);
        }
        
        for (var i = 0; i < _sameColored.Length; i++)
        {
            var current = _sameColored[i];
            
            var list = _sameColored
                .Skip(1 + i)
                .Where(x =>
                {
                    bool isClose = _distanceComparer.Are2Close(current.Root, x.Root) == true;
                    
                    if (!isClose)
                        return false;
                    
                    //add both to each other in their own list
                    UpdateNode(current, x);
                    UpdateNode(x, current);
                   
                    return true;
                });

            if (!list.Any())
                continue;

            yield return list.Prepend(current);
        }
    }

    private IOrderedEnumerable<Node>? SortByEdge()
    {
        var allAdjacentTiles = new HashSet<Node>(_sameColored.Length, Comparer.CellComparer.Singleton);
        
        foreach (var adjacent in AddEdges())
        {
            allAdjacentTiles.AddAll(adjacent.Where(x => x.Edges > 0));
        }
        
        return allAdjacentTiles.Count > 0 
            ? allAdjacentTiles.OrderByDescending(x => x, Comparer.EdgeComparer.Singleton) 
            : null;
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        //Filter all "most-edges" out of the already filtered (edges > 1)-list
        var sortedNodes = SortByEdge();
        
        if (sortedNodes is null)
            yield break;

        foreach (var current in sortedNodes)
        {
            if (current.Edges == 0)
                continue;
            
            current.CutLink();
            
            yield return current.Root;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}