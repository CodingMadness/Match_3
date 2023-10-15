﻿using DotNext;
using DotNext.Runtime;
using FastEnumUtility;

namespace Match_3.Variables;

public readonly record struct Quest(TileColor TileColor, 
                                    SubQuest? Match, 
                                    SubQuest? Swap,
                                    SubQuest? Replacement,
                                    SubQuest? MissMatch)
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
    
    public const string MatchCountName    = nameof(Quest.Match)+ "." + nameof(Quest.Match.Value.Count);
    public const string MatchIntervalName = nameof(Quest.Match)+ "." + nameof(Quest.Match.Value.Interval);
    public const string SwapCountName = nameof(Quest.Swap)+ "." + nameof(Quest.Swap.Value.Count);
    public const string ReplacementCountName = nameof(Quest.Replacement)+ "." + nameof(Quest.Replacement.Value.Count);
    public const string MissMatchName = nameof(Quest.MissMatch)+ "." + nameof(Quest.MissMatch.Value.Count);
    
    public int GetValueByMemberName(ReadOnlySpan<char> name)
    {
        //FOR NOW we compare the contents but only until I have written a replace method to work merely on 
        // Spans and does not create a new copy!!!
        if (name.BitwiseCompare(MatchCountName) == 0)
            return Match!.Value.Count;
        else if (name == MatchIntervalName)
            return (int)Match!.Value.Interval;
        else if (name == SwapCountName)
            return Swap!.Value.Count;
        else if (name == ReplacementCountName)
            return Replacement!.Value.Count;
        else if (name == MissMatchName)
            return MissMatch!.Value.Count;

        throw new ArgumentException("this code should not be reached at all!");
    }
    
}