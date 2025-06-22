using System.Buffers;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Raylib_cs;

namespace Match_3.Setup;

public class AssetFolder : IEnumerable<AssetFolder>
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

    private IEnumerable<(View<char> folderName, int nestLvl)> YieldSubFolders()
    {
        return
            from fullAssetPath in _folders.Value
            orderby '\\'
            let beginOfAssetFolder = fullAssetPath.AsSpan().IndexOf(Name, StringComparison.Ordinal)
            let endOfAssetFolder = beginOfAssetFolder + Name.Length + 1
            let folderName = new View<char>(fullAssetPath.AsSpan(endOfAssetFolder..).FirstLetter2Upper())
            let subLvlCount = folderName.AsSpan().Count('\\')
            select (folderName, subLvlCount);
    }

    public AssetFolder(View<char> name)
    {
        Name = name;
        SubAssetFolders = new();
        Files = new();
    }

    private void AddSubFolder(ReadOnlySpan<char> name)
    {
        var folder = new AssetFolder(new(name))
        {
            Files = new(0),
            Name = new(name),
        };

        SubAssetFolders.Add(folder);
    }

    public static AssetFolder LoadAssetFolder()
    {
        static ReadOnlySpan<char> TrySlice(ReadOnlySpan<char> src)
        {
            int idx = src.LastIndexOf('\\');
            int? maybeFound = idx == -1 ? null : idx;
            return maybeFound is not null ? src.Slice(idx + 1) : src;
        }

        static ReadOnlySpan<char> GetNextSubFolderByNestLvl(ReadOnlySpan<char> currentFolder, int nestingLvl)
        {
            int i = 0;
            ReadOnlySpan<char> result = currentFolder;
            //we do not want to slice 1x more, the loop shall stop at the last found '/'
            int maxNesting = nestingLvl;
            
            while (i++ < maxNesting)
            {
                //sprites/gui/button
                var found = currentFolder.IndexOf('\\');
                result =  currentFolder[(found+1)..];
            }

            return result;
        }
        
        static View<char> Dequeue(Queue<View<char>> buffer, int nestingLvl, bool shallPrepare)
        {
            // return nestingLvl > 0 ? GetNextSubFolderByNestLvl(buffer.Dequeue(), nestingLvl).ToString() : buffer.Dequeue();
            View<char> result;
            
            if (shallPrepare)
            {
                var first = buffer.Dequeue() ;
                result = new(GetNextSubFolderByNestLvl(first, nestingLvl));
            }
            else
            {
                result = buffer.Dequeue();
            }

            return result;
        }

        //Match_3.Assets.Sprites.GUI.BackGround.Welcome.<file>.<format>
        AssetFolder head = new(new("Assets"));
        using var folderIterator = head.YieldSubFolders().GetEnumerator();
        AssetFolder next = head;
        Queue<View<char>> buffer = new(2);
        bool shallPrepare;
        int currNestLvl = -1;
        
        AssetFolder IterateRecursively()
        {
            while (folderIterator.MoveNext())
            {
                var (folderName, nestingLvl) = folderIterator.Current;
                buffer.Enqueue(folderName);
                var currFolderName = Dequeue(buffer, nestingLvl, currNestLvl < nestingLvl);
                shallPrepare = currNestLvl == nestingLvl;
                
                //next level nesting was found
                if (shallPrepare)
                {
                    next = head.First(x => currFolderName.AsSpan().StartsWith(x.Name, StringComparison.OrdinalIgnoreCase));
                    buffer.Enqueue(folderName);
                    return IterateRecursively();
                }
                next.AddSubFolder(currFolderName);
            }
            return head;
        }

        return IterateRecursively();
    }

    public IEnumerator<AssetFolder> GetEnumerator()
    {
        return ((IEnumerable<AssetFolder>)SubAssetFolders).GetEnumerator();
    }

    public override string ToString() => Name.ToString();
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
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
        var assets = AssetFolder.LoadAssetFolder();
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}