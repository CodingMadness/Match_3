namespace Match_3.Variables;

public struct EventStats : IComparable<EventStats>
{
    private TimeOnly? _prev, _current;
    private int _count;

    public EventStats(int count) : this()
    {
        Count = count;
    }

    public float Interval { get; private set; }

    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            _current = TimeOnly.FromDateTime(DateTime.Now);
            _prev = _prev is null ? _current : TimeOnly.FromTimeSpan((_current - _prev).Value);
            Interval = _prev?.Second ?? 0f;
        }
    }

    public override string ToString()
    {
        return $"event: ({Count} was done in an Interval of: {Interval} seconds) {Environment.NewLine}";
    }

    public int CompareTo(EventStats other)
    {
        var countComparison = _count.CompareTo(other._count);
        return countComparison == 0 ? Interval.CompareTo(other.Interval) : countComparison;
    }
}