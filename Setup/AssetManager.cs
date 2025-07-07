using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using DotNext.Buffers;
using DotNext.Runtime;
using Match_3.DataObjects;
using NetFabric.Hyperlinq;

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

    private Span<byte> GetEmbeddedResourceAsBytes(string fullPath)
    {
        using var stream = EmbeddedResources.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);
        
        var length = (int)stream.Length;
        var content = fileData.Span[..length];
        stream.ReadExactly(content);
        return content;
    }

    private AssetManager()
    {
     
    }

    public static readonly AssetManager Instance = _instance;

    private IEnumerable<string> YieldFileNames(AssetFolderInfo folderInfo)
    {
        return AllFilePaths.Value
            .Select(path => path.Contains(folderInfo.Name, StringComparison.OrdinalIgnoreCase) ? path : "")
            .Where(path => !string.IsNullOrWhiteSpace(path));
        
        // foreach (var fileName in res)
        // {
        //     yield return (fileName, new (GetEmbeddedResourceAsBytes(Path.Join(folderInfo.FullPath, fileName))));
        // }
    }

    public AssetContainer LoadAssetFolder(string resourceFolderName)
    {
        AssetContainer head = new(new(resourceFolderName));
        
        foreach (var filePath in AllFilePaths.Value)
        {
            AssetFolderInfo folderInfo = new(Path.GetDirectoryName(filePath)!);
            var current = head.AddSubFolder(folderInfo);
            current.AddFiles(YieldFileNames(folderInfo), GetEmbeddedResourceAsBytes);
        }
        // var tileAtlas = head["Dangerous"];
        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}