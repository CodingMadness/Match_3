namespace Match_3.Variables;

public readonly record struct Quest(TileType ItemType, SubQuest? Match, SubQuest? Swap, SubQuest? Replacement)
{
    public int CompareReplacements(in AllStats? stats)
    {
        return -1;
    }
    
    public int CompareSwaps(in AllStats? stats)
    {
        return stats switch
        {
            { Swapped: { } statsClick } when Swap is { Count : var count } => count.CompareTo(statsClick.Count),
            null => 1,
            { Swapped: null } => 1,
            { Swapped: { } } when Swap is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }

    public int CompareMatches(in AllStats? stats)
    {
        return stats switch
        {
            /*stats.Matched*/
            { Matched: var matchedStats } when Match is not null
                => Match.Value.CompareTo(matchedStats!.Value),
            null => 1,
            { Matched: null } => 1,
            { Matched: not null } when Match is null => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(stats), stats, null)
        };
    }
}