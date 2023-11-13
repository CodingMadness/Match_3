using System.Collections;
using System.Numerics;
using CommunityToolkit.HighPerformance;
using DotNext.Collections.Generic;

using Comparer = Match_3.Service.Comparer;
 
namespace Match_3.DataObjects;

/// <summary>
/// This class shall track which Tile(KEY) has to close neighbors(VALUES) which shall be moved away!
/// </summary>
public class TileGraph : IEnumerable<Tile>
{
    public class Node : IGameTile
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

        public override string ToString() => $"Node of:  [-{Root}-]";
        
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
        
        public Vector2 Cell
        {
            get => Root.Cell;
            set => Root.Cell = value;
        }

        public Shape Body => Root.Body;
    }

    private readonly Node[] _sameColored;
    private readonly Comparer.DistanceComparer _distanceComparer;
    private readonly HashSet<IEnumerable<Node>> _adjacencyGraph;

    public TileGraph(Tile[,] bitmap, TileColor color)
    {
        _sameColored = bitmap
            .OfType<Tile>()
            .Where(x => x.Body.TileKind == color)
            .OrderBy(x => x, Comparer.CellComparer.Singleton)
            .Select(x => new Node(x))
            .ToArray();

        _distanceComparer = new();
        _adjacencyGraph = new(_sameColored.Length / 2);
        AddEdges();
    }

    // private IEnumerable<Tile> TakeClusteredItems()
    // {
    //     //get this value dynamically at runtime so its always the right amount for efficiency
    //     const int pseudoCount = 12;
    //     Dictionary<Tile, Tile> clusteredTiles = new(pseudoCount, distanceComparer);
    //
    //     var result = bitmap.ToArray();
    //
    //     foreach (var first in result)
    //     {
    //         //toReplace.AddAll();
    //         var distant =
    //             result.Where(x => distanceComparer.Are2Close(first, x) == true);
    //
    //         foreach (var xTile in distant)
    //         {
    //             //with this we deny the occurence of this: (key, value) <-> (value, key) which is the same
    //             //and hence has not to be stored
    //             if (clusteredTiles.ContainsKey(xTile) || clusteredTiles.ContainsValue(xTile))
    //                 continue;
    //
    //             clusteredTiles.TryAdd(first, xTile);
    //         }
    //     }
    //
    //     // Get counts of all keys and all values
    //     var keys = clusteredTiles.Keys.GroupBy(x => x).Select(x => x.Key);
    //     var values = clusteredTiles.Values.GroupBy(x => x).Select(x => x.Key);
    //     var difference = keys.Concat(values)
    //         .DistinctBy(x => x, Comparer.CellComparer.Singleton)
    //         .OrderBy(x => x, Comparer.CellComparer.Singleton)
    //         .ToArray();
    //     int index;
    //
    //     Optional<Tile> last;
    //
    //     for (index = 0; index < difference.Length; index = Array.IndexOf(difference, last.Value) + 1)
    //     {
    //         var first = difference[index];
    //
    //         last = difference
    //             .Skip(index)
    //             .FirstOrNone(x => distanceComparer.Are2Close(first, x) == true);
    //
    //         if (last.IsUndefined)
    //             break;
    //
    //         last.Value.Disable(true);
    //     }
    //
    //     return difference;
    // }

    private void AddEdges()
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

            _adjacencyGraph.Add(list.Prepend(current));
        }
    }

    private IOrderedEnumerable<Node>? SortByEdge()
    {
        var allAdjacentTiles = new HashSet<Node>(_sameColored.Length, Comparer.CellComparer.Singleton);

        foreach (var adjacent in _adjacencyGraph)
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
            
            // current.Root.Body.ChangeColor(Color.Red);
            current.CutLink();
            
            yield return current.Root;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}