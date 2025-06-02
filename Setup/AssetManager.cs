using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
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

    private static Span<byte> GetEmbeddedResource(string relativePath)
    {
        var fileName = $"Match_3.Assets.{relativePath}";
        var assembly = Assembly.GetEntryAssembly();
        MemoryOwner<byte> pool;
        using (var stream = assembly?.GetManifestResourceStream(fileName) ??
                            throw new FileNotFoundException("Cannot find mappings file.",
                                nameof(fileName) + ": " + fileName))
        {
            pool = MemoryOwner<byte>.Allocate((int)stream.Length);

            stream.ReadAtLeast(pool.Span, (int)stream.Length);
        }

        return pool.Span;
    }

    private static unsafe void Get2FileFormatAndData(string relativePath,
        out sbyte* fileFormat, out byte* data, out int size)
    {
        var buffer = GetEmbeddedResource($"Sounds.{relativePath}");

        fixed (byte* customPtr = buffer)
        {
            var format = relativePath.AsSpan(relativePath.LastIndexOf('.'));

            fixed (char* cPtr = format)
            {
                sbyte* conversion = (sbyte*)cPtr;
                fileFormat = conversion;
                data = customPtr;
                size = buffer.Length;
            }
        }
    }

    private static unsafe Sound LoadSound(string relativePath)
    {
        Get2FileFormatAndData(relativePath, out sbyte* fileFormat, out byte* data, out int size);
        Wave file = LoadWaveFromMemory(fileFormat, data, size);
        return LoadSoundFromWave(file);
    }

    private static unsafe Texture2D LoadTexture(string relativePath)
    {
        Get2FileFormatAndData(relativePath, out sbyte* fileFormat, out byte* data, out int size);
        var file = LoadImageFromMemory(fileFormat, data, size);
        return LoadTextureFromImage(file);
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