using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Match_3.GameTypes;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static unsafe class AssetManager
{
    public static Texture BGAtlas;
    public static Texture DefaultTileAtlas;
    public static Texture EnemyAtlas;
    
    private static Font welcomeFont = GetFontDefault();
    public static readonly GameText WelcomeText = new(welcomeFont, "Welcome young man!!", 7f);
    public static readonly GameText GameOverText = new( welcomeFont, "!!", 7f);
    public static readonly GameText TimerText = new( welcomeFont with { baseSize = 512 * 2 }, "Welcome young man!!", 7f);
    public static readonly GameText LogText = new(welcomeFont, "", 20f); 

    public static string GetAssetFolderName(string? nextFolder)
    {
        //this line has to be because when calling the .exe from the bin folder, 
        //rider apparently sets the currentDirectory to /usr/shpend and not as I expect to the
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="relativePath">the path looks like: Root.Sub or Fonts.font3.oft</param>
    /// <exception cref="FileNotFoundException"></exception>
    public static byte[] GetEmbeddedResource(string relativePath)
    {
        var fileName = $"Match_3.Assets.{relativePath}";
        var assembly = Assembly.GetEntryAssembly();
        var stream = assembly?.GetManifestResourceStream(fileName);
        MemoryStream ms = new MemoryStream();

        if (stream == null)
        {
            throw new FileNotFoundException("Cannot find mappings file.", nameof(fileName) + ": " + fileName);
        }
        stream.CopyTo(ms);
        var buffer = ms.GetBuffer();
        return buffer;
    }

    public static void LoadAssets()
    {
        var buffer = GetEmbeddedResource("Fonts.font4.otf");
        byte* first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        welcomeFont = LoadFontFromMemory(".otf", first, buffer.Length, 20, null, 0);

        buffer = GetEmbeddedResource("Atlas.bg.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        Image bg = LoadImageFromMemory(".png", first, buffer.Length);
        BGAtlas = LoadTextureFromImage(bg);
        
        buffer = GetEmbeddedResource("Atlas.set1.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        Image ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        DefaultTileAtlas = LoadTextureFromImage(ballImg);
        
        buffer = GetEmbeddedResource("Atlas.set2.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        EnemyAtlas = LoadTextureFromImage(ballImg);
    }
}