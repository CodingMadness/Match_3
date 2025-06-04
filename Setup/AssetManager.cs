using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using ImGuiNET;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Match_3.Setup;

public class AssetManager : IDisposable
{
    private static readonly AssetManager _instance = new();

    private const int LargeEnough2FitAllResources = 1024 * 100; //100KB for now
    private readonly MemoryOwner<byte> fileData = MemoryOwner<byte>.Allocate(LargeEnough2FitAllResources);
    private readonly Assembly? ResourceFile = Assembly.GetEntryAssembly();

    private AssetManager()
    {
    }

    public static readonly AssetManager Instance = _instance;
    public Texture2D DefaultTileAtlas { get; private set; }
    public ImFontPtr CustomFont { get; private set; }

    private Span<byte> GetEmbeddedResourceBytes(in string relativePath)
    {
        var fullPath = $"Match_3.Assets.{relativePath}";

        using var stream = ResourceFile?.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);

        var length = (int)stream.Length;
        var usableBuffer = fileData.Span[..length];
        stream.ReadExactly(usableBuffer);
        return usableBuffer;
    }

    private unsafe void Get2FileFormatAndData(in string relativePath,
        out sbyte* fileFormat, out byte* data, out int size)
    {
        var buffer = GetEmbeddedResourceBytes(relativePath);

        fixed (byte* customPtr = buffer)
        {
            var format = relativePath[relativePath.LastIndexOf('.')..];
            fileFormat = (sbyte*)Marshal.StringToHGlobalAnsi(format);
            data = customPtr;
            size = buffer.Length;
        }
    }

    private unsafe Texture2D LoadTexture(in string relativePath)
    {
        Get2FileFormatAndData(in relativePath, out sbyte* fileFormat, out byte* data, out int size);
        var file = LoadImageFromMemory(fileFormat, data, size);
        return LoadTextureFromImage(file);
    }

    private Texture2D LoadInGameTexture(in string relativePath)
    {
        return LoadTexture($"Sprites.Tiles.{relativePath}");
    }

    private unsafe ImFontPtr LoadCustomFont(in string relativePath, float fontSize)
    {
        var fullPath = $"Fonts.{relativePath}";
        Get2FileFormatAndData(in fullPath, out _, out byte* data, out int size);
        var io = ImGui.GetIO();
        var customFont = io.Fonts.AddFontFromMemoryTTF((nint)data, size, fontSize);
        return customFont;
    }

    public void LoadAssets(float fontSize)
    {
        DefaultTileAtlas = LoadInGameTexture("set3_1.png");
        CustomFont = LoadCustomFont("font6.ttf", fontSize);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            fileData.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~AssetManager()
    {
        Dispose(false);
    }
}