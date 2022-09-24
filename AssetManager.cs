using JetBrains.Annotations;
using Raylib_CsLo;

namespace Match_3;

public static class AssetManager
{
    public static Texture SpriteSheet { get; private set; }
    public static Font DebugFont { get; private set; }
    
    public static string GetAssetfolderName(string? nextFolder)
    {
        //this line has to be because when calling the .exe from the bin folder, 
        //rider apperently sets the currentDirectory to /usr/shpend and not as I expect to the
        //full location of the 
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        var net6Path = Environment.CurrentDirectory.AsSpan();
        Console.WriteLine(net6Path.ToString());

        const string projectName = "Match_3";
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
        DebugFont = Raylib.GetFontDefault();//Raylib.LoadFont(GetAssetfolderName("fonts") + "font2.otf");
        SpriteSheet = Raylib.LoadTexture(GetAssetfolderName("spritesheets") + "set1.png");
    }
}