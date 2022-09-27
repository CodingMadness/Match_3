using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace Match_3;

public static unsafe class AssetManager
{
    public static Texture Default;
    public static Texture MatchBlockAtlas;
    public static Font WelcomeFont;

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

    public static void Init()
    {
        var buffer = GetEmbeddedResource("Fonts.font3.otf");
        byte* first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        WelcomeFont = LoadFontFromMemory(".otf", first, buffer.Length, 20, null, 0);

        buffer = GetEmbeddedResource("Atlas.set1.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        Image ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        Default = LoadTextureFromImage(ballImg);
        
        buffer = GetEmbeddedResource("Atlas.set2.png");
        first = (byte*)Unsafe.AsPointer(ref buffer[0]);
        ballImg = LoadImageFromMemory(".png", first, buffer.Length);
        MatchBlockAtlas = LoadTextureFromImage(ballImg);
    }
}