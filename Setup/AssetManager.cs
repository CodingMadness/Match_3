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
    private List<AssetFolder> SubAssetFolders { get; } = [];
    public required View<char> Name { get; init; }
    public required int Depth { get; init; }
    
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
            let depth = folderName.AsSpan().Count('\\') + 1
            select (folderName, depth);
    }
    private void AddSubFolder(ReadOnlySpan<char> name, int depth)
    {
        var folder = new AssetFolder
        {
            Name = new(name),
            Depth = depth
        };

        SubAssetFolders.Add(folder);
    }

    public static IEnumerable<AssetFolder> GetFoldersAtDepthBFS(AssetFolder root, int targetDepth)
    {
        var queue = new Queue<(AssetFolder folder, int depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
        
            if (depth == targetDepth)
            {
                yield return current;
            }
            else if (depth < targetDepth)
            {
                foreach (ref var subfolder in CollectionsMarshal.AsSpan(current.SubAssetFolders))
                {
                    queue.Enqueue((subfolder, depth + 1));
                }
            }
        }
    }
    public static AssetFolder LoadAssetFolder()
    {
        static ReadOnlySpan<char> GetNextSubFolderByNestLvl(ReadOnlySpan<char> currentFolder, int depth)
        {
            int i = 0;
            //we do not want to slice 1x more, the loop shall stop at the last found '/'
            int iterations = depth-1;

            while (i++ < iterations)
            {
                //sprites/gui/button
                var found = currentFolder.IndexOf('\\');
                currentFolder = currentFolder[(found + 1)..];
            }

            return currentFolder;
        }

        static View<char> GetFolderName(Queue<View<char>> buffer, int folderDepth)
        {
            View<char> result;

            if (folderDepth > 1)
            {
                var first = buffer.Dequeue();
                result = new(GetNextSubFolderByNestLvl(first, folderDepth));
            }
            else
            {
                result = buffer.Dequeue();
            }

            return result;
        }

        static AssetFolder GetParentFolder(AssetFolder head, int depth, View<char> fullFolderPath, View<char> childFolderName)
        {
            int parentLvl = depth - 1;
            var foldersFromDepth = GetFoldersAtDepthBFS(head, parentLvl);
            return foldersFromDepth.First(folder => fullFolderPath.AsSpan().EndsWith(Path.Join(folder.Name, childFolderName)));
        }
        
        //Match_3.Assets.Sprites.GUI.BackGround.Welcome.<file>.<format>
        AssetFolder head = new()
        {
            Name = new("Assets"),
            Depth = 0
        };
        
        using var folderIterator = head.YieldSubFolders().GetEnumerator();
        AssetFolder next = head;
        Queue<View<char>> buffer = new(2);
        int currDepth = 1;
        
        while (folderIterator.MoveNext())
        {
            var (view, depth) = folderIterator.Current;
            buffer.Enqueue(view);
            var folderName = GetFolderName(buffer, depth);
                
            if (depth > currDepth)
            {
                next = GetParentFolder(head, depth, view, folderName);
                currDepth++;
            }

            next.AddSubFolder(folderName,  depth);
        }

        return head;
    }

    public IEnumerator<AssetFolder> GetEnumerator() => SubAssetFolders.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
        var assets = AssetFolder.LoadAssetFolder();
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}