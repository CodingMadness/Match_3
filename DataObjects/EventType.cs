namespace Match_3.DataObjects;

public enum EventType : byte
{
    Clicked,
    Swapped,
    Matched,
    RePainted,
    Destroyed,
    COUNT = Destroyed + 1
}