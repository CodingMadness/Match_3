using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using DotNext;
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
    private readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();
    private Lazy<IEnumerable<string>> AllFilePaths => new(() => EmbeddedResources.GetManifestResourceNames(), LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly List<AssetFile> _allFiles = [];
    
    private Span<byte> GetEmbeddedResourceAsBytes(string fullPath)
    {
        using var stream = EmbeddedResources.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);
        
        var length = (int)stream.Length;
        var content = fileData.Span[..length];
        stream.ReadExactly(content);
        return content;
    }
    // private static IEnumerable<string> YieldFileNames(string parentFolderName)
    // {
    //     return AllFilePaths.Value
    //         .Select(path => path.Contains(parentFolderName, StringComparison.OrdinalIgnoreCase) ? path : "")
    //         .Where(path => !string.IsNullOrWhiteSpace(path));
    // }
    
    private AssetManager()
    {
     
    }

    public static readonly AssetManager Instance = _instance;

    public AssetFile GetFile(string fileName) => _allFiles.Find(file => file.FileInfo.Name.BitwiseEquals(fileName));
    
    public void LoadAssetFolder()
    {
        foreach (var filePath in AllFilePaths.Value)
        {
            var file = new AssetFile(new(filePath), GetEmbeddedResourceAsBytes(filePath));
            _allFiles.Add(file);
        }
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}