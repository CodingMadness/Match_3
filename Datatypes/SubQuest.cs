namespace Match_3.Datatypes;

public readonly record struct SubQuest(int Count, float Interval)
{
    public int CompareTo(EventState other)
    {
        int countCmp = Count.CompareTo(other.Count);
        return countCmp == 0 ? Interval.CompareTo(other.Interval) : countCmp;
    }
}