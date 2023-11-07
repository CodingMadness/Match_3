using System.Collections;
using DotNext;
using DotNext.Collections.Generic;
using NoAlloq;
using Comparer = Match_3.Service.Comparer;

namespace Match_3.DataObjects;

/// <summary>
/// This class shall track which tiles(values) close to another tile(key)
/// </summary>
public class TileNodes : IEnumerable<Tile>
{
    private readonly Dictionary<Tile, IEnumerable<Tile>> _nodes;
    
    private readonly Tile[] _sameColored;

    private readonly Comparer.DistanceComparer _distanceComparer;

    // private int _index = 0;
    private BitPack _indices;

    public TileNodes(Tile[,] bitmap, TileColor color)
    {
        _sameColored = bitmap
            .OfType<Tile>()
            .Where(x => x.Body.TileKind == color)
            .OrderBy(x => x, Comparer.CellComparer.Singleton).ToArray();

        _distanceComparer = new();
        _nodes = new(_sameColored.Length, _distanceComparer);

        BuildLinks();

        int s = 1;
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

    private bool ContainsKeyAsValue(Tile key, out Tile? existentKey)
    {
        var found = _nodes
            .Select(x => x)
            .FirstOrNone(y => y.Value.Any(z => z.GridCell == key.GridCell));

        existentKey = !found.IsUndefined ? found.Value.Key : null;
        return existentKey is not null;
    }

    private Tile[] Intersection(Tile existentKey, IEnumerable<Tile> value)
    {
        var onlyPotentialNewOnes = value.Skip(1);
        _nodes.TryGetValue(existentKey, out var existent);
        var result = existent!.Intersect(onlyPotentialNewOnes);
        return result.ToArray();
    }
    
    private static void ReorderOccurrences(Tile[] distant, Tile node)
    {
        distant.AsSpan().OrderBy(distant.AsSpan(), x => x.GridCell == node.GridCell);
    }
    
    private int AddNode(Tile newKey)
    {
        var nodes = _sameColored.Where(x => _distanceComparer.Are2Close(newKey, x) == true).ToArray();
        int count = nodes.Length;
        int toSkip = 0;
        int returnValue = count;
        
        //in error-cases, such as when there has been already a Node added to the dict OR
        //when 
        switch (count)
        {
            case 0:
                returnValue = 1;
                break;
            // with this we deny the occurence of this: (key, value) <-> (value, key)
            // which is the same and hence has not to be stored!
            case 1 when ContainsKeyAsValue(newKey, out _):
                returnValue = 1;
                break;
            case > 1 when ContainsKeyAsValue(newKey,  out var existentKey):
                // ReorderOccurrences(nodes, nodeKey);
                // var intersection = Intersection(existentKey!, nodes);
                toSkip = 1; //we skip only the one we have
                returnValue = count - toSkip;
                break;
        }
        ++newKey.NodeCount ;
        nodes.Skip(toSkip).ForEach(x => ++x.NodeCount);
        _nodes.TryAdd(newKey, nodes.Skip(toSkip).ToArray());
        return returnValue;
    }

    // private void Loop()
    // {
    //     Optional<Tile> last;
    //     
    //     for (_index = 0; _index < _sameColored.Length; _index = Array.IndexOf(_sameColored, last.Value) + 1)
    //     {
    //         var first = _sameColored[_index];
    //     
    //         last = _sameColored
    //             .Skip(_index)
    //             .FirstOrNone(x => _distanceComparer.Are2Close(first, x) == true);
    //     
    //         if (last.IsUndefined)
    //             break;
    //     
    //         last.Value.Disable(true);
    //     }
    // }

    private void BuildLinks()
    {
        int next;

        for (int i = 1; i < _sameColored.Length; i += AddNode(_sameColored[i]))
        { }
    }

    public IEnumerator<Tile> GetEnumerator()
    {
        // foreach (var node in _nodes)
        // {
        //    //yield return those elements whose "NodeCount" > 1
        // }
        return null;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}