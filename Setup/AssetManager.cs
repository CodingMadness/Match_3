using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNext.Buffers;
using DotNext.Runtime;
using Match_3.DataObjects;

namespace Match_3.Setup;

public class AssetManager : IDisposable
{
    private static readonly AssetManager _instance = new();
    private const int LargeEnough2FitAllResources = 1024 * 1000; //1MB for now
    private MemoryOwner<byte> fileData = new(ArrayPool<byte>.Shared, LargeEnough2FitAllResources);
    private static readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();
    
    private static string GetResourceDir(out string projDir)
    {
        projDir = EmbeddedResources.GetName().Name!;
        var resFolder = EmbeddedResources.GetManifestResourceNames()[0].AsSpan();
        resFolder = resFolder.Slice(0, resFolder.IndexOf('\\'));
        return resFolder.ToString();
    }

    private static readonly Lazy<IEnumerable<string>> AllFilePaths =
        new(() => EmbeddedResources.GetManifestResourceNames(), LazyThreadSafetyMode.ExecutionAndPublication);

    private Span<byte> GetEmbeddedResourceAsBytes(string relativePath)
    {
        var resourceDir = GetResourceDir(out _);
        var fullPath = Path.Join(resourceDir, relativePath);
        
        // using var stream = EmbeddedResources.GetManifestResourceStream(fullPath) ??
        //                    throw new FileNotFoundException("Cannot find resource file.", fullPath);
        //
        // var length = (int)stream.Length;
        // var content = fileData.Span[..length];
        // stream.ReadExactly(content);
        Span<byte> content = new([1, 2, 3]);
        return content;
    }

    private AssetManager()
    {
     
    }

    public static readonly AssetManager Instance = _instance;

    private IEnumerable<(string fileName, View<byte> fileData)> YieldFiles(AssetFolderInfo folderInfo)
    {
        var res = AllFilePaths.Value.Select(path =>
            path.AsSpan().Contains(folderInfo.Name, StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(path) : "")
            .Where(path => !string.IsNullOrWhiteSpace(path));
        
        foreach (var fileName in res)
        {
            yield return (fileName, new(GetEmbeddedResourceAsBytes(Path.Join(folderInfo.Name, fileName))));
        }
    }

    private IEnumerable<AssetFolderInfo> YieldSubFolders()
    {
        return AllFilePaths.Value
            .Select(Path.GetDirectoryName)
            .Distinct()  // This ensures we only process each folder once
            .Select(onlyDir => new AssetFolderInfo(onlyDir!))
            .OrderBy(folder =>  folder.Depth);
    }
    
    public AssetContainer LoadAssetFolder()
    {
        static AssetContainer LinkParentWithChild(AssetContainer current, in AssetFolderInfo folderInfo)
        {
            var fullPath = folderInfo.FullPath;
            int folderNameAppearance = fullPath.AsSpan().IndexOf(folderInfo.Name) - 1;
            var parentPath = fullPath.AsSpan().Slice(0, folderNameAppearance);            
            AssetFolderInfo parent = new(parentPath);
            return current.AddSubFolder(parent);
        }
        
        var resDir = GetResourceDir(out _);
        AssetContainer head = new(new(resDir));
        using var folderIterator = YieldSubFolders().GetEnumerator();
        var parent = head;
        int currDepth = 1;

        // var test = YieldSubFolders().ToArray();
        
        while (folderIterator.MoveNext())
        {
            var childFolderInfo = folderIterator.Current;

            if (childFolderInfo.Depth > currDepth)
            {
                parent = LinkParentWithChild(parent, childFolderInfo);
                currDepth++;
            }
            var current = parent.AddSubFolder(childFolderInfo);
            current.AddFiles(YieldFiles (childFolderInfo));
        }

        var tileAtlas = head["set1.png"];

        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}