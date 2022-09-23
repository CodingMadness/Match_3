using Raylib_CsLo;

namespace Match_3;

public static class AssetManager
{
    public static Texture SpriteSheet { get; private set; }
    public static Font DebugFont { get; private set; }
    
    public static string GetAssetfolderName(string? nextFolder)
    {
        var net6Path = Environment.CurrentDirectory.AsSpan();
       // Console.WriteLine(net6Path.ToString());

        const string projectName = "Match3";
        int lastProjectNameOccurence = net6Path.LastIndexOf(projectName, StringComparison.Ordinal) + projectName.Length;
        var projectPath = net6Path[..lastProjectNameOccurence];

        string assetFolderName;

        if (nextFolder is null)
            assetFolderName = Environment.OSVersion.Platform == PlatformID.Unix ? "/Assets/" : "\\Assets\\";

        else
            assetFolderName = Environment.OSVersion.Platform == PlatformID.Unix ? $"/Assets/{nextFolder}/" : $"\\Assets\\{nextFolder}\\";

        return $"{projectPath}{assetFolderName}"; 
    }
    
    public static void Init()
    { 
        DebugFont = Raylib.LoadFont(GetAssetfolderName("fonts") + "font5.otf");
        SpriteSheet = Raylib.LoadTexture(GetAssetfolderName("spritesheets") + "set1.png");
    }
}