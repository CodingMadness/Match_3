using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Raylib_cs;

namespace Match_3.Setup;

public class AssetFolder
{
    private List<AssetFolder> SubAssetFolders { get; }

    private List<string> Files { get; init; }

    public View<char> Name { get; init; }

    public static readonly Assembly Root = Assembly.GetExecutingAssembly();

    private static readonly Lazy<IEnumerable<string>> _folders = new(() =>
    {
        var asmLocation = Root.Location;
        var fullName = Root.FullName!;
        var slnName = fullName.AsSpan(0, fullName.IndexOf(','));
        var projPath = asmLocation.AsSpan(0, asmLocation.IndexOf(slnName) + slnName.Length);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
            IgnoreInaccessible = true,
            MatchType = MatchType.Simple,
            BufferSize = 32768 // Use the full 32KB buffer (powers of two are better)
        };
        return Directory.EnumerateDirectories(Path.Join(projPath, "Assets"), "*", options);
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    private IEnumerable<string> YieldSubFolders()
    {
        return
            from folderName in _folders.Value
            orderby '\\'
            let beginOfAssetFolder = folderName.AsSpan().IndexOf(Name, StringComparison.Ordinal)
            let endOfAssetFolder = beginOfAssetFolder + Name.Length + 1
            select folderName[endOfAssetFolder..].FirstLetter2Upper();
    }

    public AssetFolder(View<char> name)
    {
        Name = name;
        SubAssetFolders = new();
        Files = new();
    }

    private AssetFolder AddSubFolder(ReadOnlySpan<char> name)
    {
        var folder = new AssetFolder(name)
        {
            Files = new(0),
            Name = new(name),
        };

        SubAssetFolders.Add(folder);
        return folder;
    }

    public static AssetFolder LoadAssetFolder(int maxFolders)
    {
        static bool Contains(string src, string toCheck)
        {
            return toCheck is not [] && src.Contains(toCheck);
        }

        static ReadOnlySpan<char> TrySlice(string src)
        {
            int idx = src.LastIndexOf('\\');
            int? maybeFound = idx == -1 ? null : idx;
            return maybeFound is not null ? src.AsSpan(idx + 1) : src;
        }

        //Match_3.Assets.Sprites.GUI.BackGround.Welcome.<file>.<format>
        AssetFolder head = new("Assets");
        using var folderIterator = head.YieldSubFolders().GetEnumerator();
        bool isNested = false;
        string prevFolderName = string.Empty;
        List<AssetFolder> remainingFolders = new(maxFolders);

        int debugIndex = 0;
        bool debugCanMove;

        AssetFolder IterateRecursively(in AssetFolder subFolder, IEnumerator<char>? iterator=null)
        {
            AssetFolder last = subFolder;
            string current = string.Empty;
            var rightIterator = iterator is null ? folderIterator :remainingFolders.GetEnumerator();
            
            while (debugCanMove = rightIterator.MoveNext())
            {
                var next = !isNested ? folderIterator.Current : prevFolderName;
                Console.WriteLine(next);

                if (Contains(next, current))
                {
                    debugIndex++;
                    isNested = true;
                    prevFolderName = next;
                    IterateRecursively(in last);
                }
                if (isNested)
                    remainingFolders.Add(folderIterator.Current);
                
                //root can add subfolder directly because no files are in them
                var nestedFolderName = TrySlice(next);
                last = subFolder.AddSubFolder(nestedFolderName);
                current = next;
                isNested = false;
                debugIndex++;
                
                if(!debugCanMove)
                    break;
            }

            return IterateRecursively(remainingFolders[0]);
        }

        return IterateRecursively(head);
    }

    public override string ToString() => Name.ToString();
}

public unsafe class AssetManager : IDisposable
{
    private static readonly AssetManager _instance = new();
    private const int LargeEnough2FitAllResources = 1024 * 100; //100KB for now
    private MemoryOwner<byte> fileData = new(ArrayPool<byte>.Shared, LargeEnough2FitAllResources);
    private readonly Assembly ResourceFile = Assembly.GetEntryAssembly()!;

    private AssetManager()
    {
    }

    public static readonly AssetManager Instance = _instance;
    public Texture2D DefaultTileAtlas { get; private set; }
    public ImFontPtr CustomFont { get; private set; }

    private Span<byte> GetEmbeddedResourceBytes(in string relativePath)
    {
        var fullAsmName = ResourceFile.FullName!;
        var slnName = fullAsmName.AsSpan(0, fullAsmName.IndexOf(','));
        var fullPath = $"{slnName}.Assets.{relativePath}";

        using var stream = ResourceFile?.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);

        var length = (int)stream.Length;
        var usableBuffer = fileData.Span[..length];
        stream.ReadExactly(usableBuffer);
        return usableBuffer;
    }

    private void Get2FileFormatAndData(in string relativePath,
        out byte* fileFormat, out byte* data, out int size)
    {
        var buffer = GetEmbeddedResourceBytes(relativePath);

        fixed (byte* customPtr = buffer)
        {
            var format = relativePath[relativePath.LastIndexOf('.')..];
            fileFormat = (byte*)Marshal.StringToHGlobalAnsi(format);
            data = customPtr;
            size = buffer.Length;
        }
    }

    private Texture2D LoadTexture(in string relativePath)
    {
        Get2FileFormatAndData(in relativePath, out byte* fileFormat, out byte* data, out int size);
        var file = Raylib.LoadImageFromMemory((sbyte*)fileFormat, data, size);
        return Raylib.LoadTextureFromImage(file);
    }

    private Texture2D LoadInGameTexture(in string relativePath)
    {
        return LoadTexture($"Sprites.Tiles.{relativePath}");
    }

    private ImFontPtr LoadCustomFont(in string relativePath, float fontSize)
    {
        var fullPath = $"Fonts.{relativePath}";
        Get2FileFormatAndData(in fullPath, out _, out byte* data, out int size);
        var io = ImGui.GetIO();
        var customFont = io.Fonts.AddFontFromMemoryTTF((nint)data, size, fontSize);
        return customFont;
    }

    public void LoadAssets()
    {
        var assets = AssetFolder.LoadAssetFolder(11);
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}