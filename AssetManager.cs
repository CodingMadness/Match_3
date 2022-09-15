using Raylib_cs;

namespace Match_3;

public static class AssetManager
{
    public static Texture2D SpriteSheet { get; private set; }
    public static Font DebugFont { get; private set; }

    public static Optimized.Font WelcomeFont;

    static AssetManager()
    {
        
    }
    
    private static string GetAssetfolderName()
    {
        var net6Path = Environment.CurrentDirectory.AsSpan();
        const string projectName = "Match3_Backup";
        int lastProjectNameOccurence = net6Path.LastIndexOf(projectName, StringComparison.Ordinal) + projectName.Length;
        var projectPath = net6Path.Slice(0, lastProjectNameOccurence);
        var assetFolderName = Environment.OSVersion.Platform == PlatformID.Unix ? "/Assets/" : "\\Assets\\";
        return $"{projectPath}{assetFolderName}"; 
    }

    public static void Init()
    {
        var assetFolder = GetAssetfolderName();
        DebugFont = Raylib.LoadFont(assetFolder + "font3.ttf");
        WelcomeFont = Optimized.Raylib.LoadFont(assetFolder + "font4.otf");
        SpriteSheet = Raylib.LoadTexture(assetFolder + "shapes.png");
    }
}