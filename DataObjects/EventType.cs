namespace Match_3.DataObjects;

public enum EventType : byte
{
    Clicked,
    Swapped,
    Matched,
    RePainted,
    Destroyed,
    Count = Destroyed + 1
}