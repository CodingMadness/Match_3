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
    private const int LargeEnough2FitAllResources = 1024 * 10000; //10MB for now
    private MemoryOwner<byte> fileData = new(ArrayPool<byte>.Shared, LargeEnough2FitAllResources);
    private static readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();
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
    private IEnumerable<string> YieldFileNames(string parentFolderName)
    {
        return AllFilePaths.Value
            .Select(path => path.Contains(parentFolderName, StringComparison.OrdinalIgnoreCase) ? path : "")
            .Where(path => !string.IsNullOrWhiteSpace(path));
    }
    
    private AssetManager()
    {
     
    }

    public static readonly AssetManager Instance = _instance;

    public AssetContainer LoadAssetFolder()
    {
        AssetContainer head = new();
        
        foreach (var filePath in AllFilePaths.Value)
        {
            head.AddFiles(YieldFileNames(filePath), GetEmbeddedResourceAsBytes);
        }
        var tileAtlas = head["set1.png"];
        var f = tileAtlas.Format;
     
        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}