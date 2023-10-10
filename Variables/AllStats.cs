using System.Runtime.CompilerServices;

namespace Match_3.Variables;

public struct AllStats : IComparable<AllStats>
{
    /// <summary>
    /// Count-Clicks, with maxTime in between them
    /// </summary>
    // public EventStats? Clicked = new(count: 0);
    public EventStats? Swapped = new(count: 0);
    public EventStats? Matched = new(count: 0);
    public EventStats? Replacements = new(count: 0);

    public AllStats()
    {
    }

    public int CompareTo(AllStats other)
    {
        for (EventType i = 0; i < EventType.COUNT; i++)
        {
            var comparison = this[i].CompareTo(other[i]);

            if (comparison != 0)
                return comparison;
        }

        return 0;
    }

    public override string ToString()
    {
        string output =
            $"Matches made ->(Count: {Matched?.Count}  - Interval: {Matched?.Interval} {Environment.NewLine}" +
            $"Swapped made  ->(Count: {Swapped?.Count}  - Interval: {Swapped?.Interval}{Environment.NewLine}" +
            $"Repaints made ->(Count: {Replacements?.Count}  - Interval: {Replacements?.Interval}{Environment.NewLine}";
        return output;
    }

    public ref EventStats this[EventType index]
    {
        get
        {
            switch (index)
            {
                case EventType.Swapped when Swapped.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Swapped));
                    return ref tmp;
                }
                case EventType.Matched when Matched.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Matched));
                    return ref tmp;
                }
                // case EventType.Destroyed when Destroyed.HasValue:
                // {
                //     ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Destroyed));
                //     return ref tmp;
                // }
                case EventType.RePainted when Replacements.HasValue:
                {
                    ref EventStats tmp = ref Unsafe.AsRef(Nullable.GetValueRefOrDefaultRef(Replacements));
                    return ref tmp;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }
        }
    }
}