using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using static Raylib_cs.Raylib;

namespace Match_3.Setup;

public static class AssetManager
{
    public static Texture2D WelcomeTexture;
    public static Texture2D GameOverTexture;
    public static Texture2D DefaultTileAtlas;
    public static Texture2D EnemySprite;
    public static Texture2D BgIngameTexture;
    public static Shader WobbleEffect;
    public static (int secondsLoc, int gridSizeLoc, int shouldWobbleLoc) ShaderData;
    public static Texture2D FeatureBtn;
    public static Sound SplashSound;
    public static ImFontPtr CustomFont;

    private static Sound LoadSound(string relativePath)
    {
        var buffer = GetEmbeddedResource($"Sounds.{relativePath}");
        var wave = LoadWaveFromMemory(".mp3", buffer);
        return LoadSoundFromWave(wave);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="relativePath">the path looks like: Root.Sub or Fonts.font3.oft</param>
    /// <exception cref="FileNotFoundException"></exception>
    private static byte[] GetEmbeddedResource(string relativePath)
    {
        var fileName = $"Match_3.Assets.{relativePath}";
        var assembly = Assembly.GetEntryAssembly();     
        using var stream = (assembly?.GetManifestResourceStream(fileName)) ?? throw new FileNotFoundException("Cannot find mappings file.", nameof(fileName) + ": " + fileName);
        byte[] data = ArrayPool<byte>.Shared.Rent((int)stream.Length);
        stream.ReadExactly(data, 0, (int)stream.Length);
        return data;
    }

    private static unsafe ImFontPtr LoadCustomFont(string relativePath, float fontSize)
    {       
        var fontBytes = GetEmbeddedResource($"Fonts.{relativePath}");
        var io = ImGui.GetIO();
        ImFontPtr customFont;

        fixed (byte* customPtr = fontBytes)
        {
            customFont = io.Fonts.AddFontFromMemoryTTF((IntPtr)customPtr, fontBytes.Length, fontSize);
        }

        return customFont;
    }

    // private static ImFontPtr LoadCustomFontFromFile(string onlyFileName, float fontSize)
    // {
    //     var io = ImGui.GetIO();
    //     var fullPath = $"Match_3.Assets.{onlyFileName}";
    //     var assembly = Assembly.GetEntryAssembly();
    //     var paths = assembly.GetManifestResourceNames();
    //     var containsFile = paths.Any(x => x.EndsWith(onlyFileName));
    //
    //     if (!containsFile)
    //         return null;
    //
    //     var font = io.Fonts.AddFontFromFileTTF(fullPath, fontSize);
    //     return font;
    // }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullPath">For instance: Background.bg1.png OR Button.FeatureBtn.png</param>
    /// <returns></returns>
    private static Texture2D LoadTexture(string fullPath)
    {
        var buffer = GetEmbeddedResource(fullPath);
        Image bg = LoadImageFromMemory(".png", buffer);
        return LoadTextureFromImage(bg);
    }

    private static Texture2D LoadGuiTexture(string relativePath) => LoadTexture($"Sprites.GUI.{relativePath}");

    private static Texture2D LoadInGameTexture(string relativePath) => LoadTexture($"Sprites.Tiles.{relativePath}");

    public static void LoadAssets(float fontSize)
    {
        InitAudioDevice();

        SplashSound = LoadSound("splash.mp3");
        WelcomeTexture = LoadGuiTexture("Background.bgWelcome1.png");
        FeatureBtn = LoadGuiTexture("Button.btn1.png");
        BgIngameTexture = LoadGuiTexture("Background.bgIngame1.png");
        GameOverTexture = LoadGuiTexture("Background.bgGameOver.png");
        DefaultTileAtlas = LoadInGameTexture("set3_1.png");
        EnemySprite = LoadInGameTexture("set2.png");

        //TODO: this function causes memory leaks!
        // CustomFont = LoadCustomFontFromFile("font6.ttf", fontSize);
    } 
}