namespace Match_3.StateHolder;

public enum EventType : byte
{
    Clicked,
    Swapped,
    Matched,
    RePainted,
    Destroyed,
    COUNT = Destroyed + 1
}