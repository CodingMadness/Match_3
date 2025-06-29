using System.Buffers;
using System.Reflection;
using DotNext.Buffers;
using Match_3.DataObjects;

namespace Match_3.Setup;

public class AssetManager : IDisposable
{
    private static readonly AssetManager _instance = new();
    private const int LargeEnough2FitAllResources = 1024 * 100; //100KB for now
    private MemoryOwner<byte> fileData = new(ArrayPool<byte>.Shared, LargeEnough2FitAllResources);
    private static readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();

    static string GetProjectDirectory()
    {
        var fullPath = EmbeddedResources.GetManifestResourceNames()[0];
        return fullPath.AsSpan(0, fullPath.IndexOf('\\')).ToString();
    }

    private static readonly Lazy<IEnumerable<string>> AllFolderNames =
        new(() => EmbeddedResources.GetManifestResourceNames(), LazyThreadSafetyMode.ExecutionAndPublication);

    private AssetManager()
    {
    }

    public static readonly AssetManager Instance = _instance;

    public IEnumerable<AssetFolderInfo> YieldSubFolderEntries()
    {
        return
            from fullAssetPath in AllFolderNames.Value
            select new AssetFolderInfo(fullAssetPath);
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

        AssetContainer head = new()
        {
            CurrentInfo = new(GetProjectDirectory())
        };

        using var folderIterator = YieldSubFolderEntries().GetEnumerator();
        AssetContainer parent = head;
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

            parent.AddSubFolder(folderInfo);
            parent.AddFiles();
        }

        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}