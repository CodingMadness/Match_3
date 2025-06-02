using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public static nint copyOfFontBytesPtr;
    public static GCHandle Handle2CustomFont;

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
        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        return data;
    }

    private static unsafe ImFontPtr LoadCustomFont(string relativePath, float fontSize)
    {       
        var fontConfig = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
        var fontBytes = GetEmbeddedResource($"Fonts.{relativePath}");
        var customFontHandle = GCHandle.Alloc(fontBytes, GCHandleType.Pinned);
        var io = ImGui.GetIO();
        ImFontPtr customFont;
        copyOfFontBytesPtr = customFontHandle.AddrOfPinnedObject();

        try
        {
            copyOfFontBytesPtr = customFontHandle.AddrOfPinnedObject();
            customFont = io.Fonts.AddFontFromMemoryTTF(
                copyOfFontBytesPtr,
                fontBytes.Length,
                fontSize,
                fontConfig,
                io.Fonts.GetGlyphRangesDefault()
            );
            io.Fonts.Build();
            rlImGui.ReloadFonts();
        }
        finally
        {
            customFontHandle.Free();
            fontConfig.Destroy();
            GC.Collect();
        }

        return customFont;
    }

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
        CustomFont = LoadCustomFont("font6.ttf", fontSize);
    } 
}