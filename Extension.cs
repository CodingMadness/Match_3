using Raylib_cs;

namespace Match_3;

public static class Extension
{
    public static string GetName(this Color c) => nameof(c);
    
    public static bool operator ==(Color c1, Color c2) =>
        c1.a == c2.a && c1.b == c2.b && c1.g == c2.g && c1.r == c2.r;

    public static bool operator !=(Color c1, Color c2) => !(c1 == c2);

    
}