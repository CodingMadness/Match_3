using System.Buffers;
using System.Reflection;
using DotNext.Buffers;
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
            .Select(onlyDir => new AssetFolderInfo(onlyDir!));
    }
    
    public AssetContainer LoadAssetFolder()
    {
        static ReadOnlySpan<char> GetNextSubFolderByNestLvl(ReadOnlySpan<char> currentFolder, int depth)
        {
            int i = 0;
            //we do not want to slice 1x more, the loop shall stop at the last found '/'
            int iterations = depth - 1;

            while (i++ < iterations)
            {
                //sprites/gui/button
                var found = currentFolder.IndexOf('\\');
                currentFolder = currentFolder[(found + 1)..];
            }

            return currentFolder;
        }

        static View<char> GetFolderName(Queue<View<char>> buffer, in AssetFolderInfo folder)
        {
            View<char> result;
            buffer.Enqueue(folder.FullPath);

            if (folder.Depth > 1)
            {
                var first = buffer.Dequeue();
                result = new(GetNextSubFolderByNestLvl(first, folder.Depth));
            }
            else
            {
                result = buffer.Dequeue();
            }

            return result;
        }

        static AssetContainer GetParentFolder(AssetContainer head, AssetFolderInfo folderInfo,
            View<char> childFolderName)
        {
            int parentLvl = folderInfo.Depth - 1;
            var foldersFromDepth = head.GetFoldersAtDepth(parentLvl);
            return foldersFromDepth.First(folder =>
                folderInfo.FullPath.AsSpan()
                    .EndsWith(Path.Join(folder.CurrentInfo.FullPath, childFolderName)));
        }

        var resDir = GetResourceDir(out var projDir);
        AssetContainer head = new(new(Path.Join(projDir, resDir)));
         
        var x = YieldSubFolders().ToArray();
        using var folderIterator = YieldSubFolders().GetEnumerator();
        var parent = head;
        Queue<View<char>> buffer = new(2);
        int currDepth = 1;
        
        while (folderIterator.MoveNext())
        {
            var folderInfo = folderIterator.Current;
            var childFolderName = GetFolderName(buffer, in folderInfo);

            if (folderInfo.Depth > currDepth)
            {
                parent = GetParentFolder(parent, folderInfo, childFolderName);
                currDepth++;
            }
            var current = parent.AddSubFolder(folderInfo);
            current.AddFiles(YieldFiles (folderInfo));
            int a = 1;
        }

        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}