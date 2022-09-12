using Raylib_cs;

namespace Match_3;

public static class AssetManager
{
    public static Texture2D SpriteSheet { get; private set; }
    public static Font Font { get; private set; }

    static AssetManager()
    {
        
    }

    public static void Init()
    {
        string net6Path = Environment.CurrentDirectory;
        const string projectName = "Match3";
        int lastProjectNameOccurence = net6Path.LastIndexOf(projectName, StringComparison.Ordinal) + projectName.Length;
        var fontPath = $"{net6Path.AsSpan(0, lastProjectNameOccurence)}/Assets/font3.ttf";
        Font = Raylib.LoadFont(fontPath);
        var tilePath = $"{net6Path.AsSpan(0, lastProjectNameOccurence)}/Assets/shapes.png";
        SpriteSheet = Raylib.LoadTexture(tilePath);
    }
}