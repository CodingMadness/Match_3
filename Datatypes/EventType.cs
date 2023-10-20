namespace Match_3.Datatypes;

public enum EventType : byte
{
    Clicked,
    Swapped,
    Matched,
    RePainted,
    Destroyed,
    COUNT = Destroyed + 1
}