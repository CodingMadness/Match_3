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

public class AssetManager 
{
    private static readonly AssetManager _instance = new();
    private readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();
    private Lazy<IEnumerable<string>> AllFilePaths => new(() => EmbeddedResources.GetManifestResourceNames(),
        LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly List<AssetFile> _allFiles = [];

    private MemoryOwner<byte> GetEmbeddedResourceAsBytes(string fullPath)
    {
        using var stream = EmbeddedResources.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);
        var owner = new MemoryOwner<byte>(ArrayPool<byte>.Shared, (int)stream.Length);
        stream.ReadExactly(owner.Span);
        return owner; // Caller must dispose
    }

    private AssetManager()
    {
    }

    public static readonly AssetManager Instance = _instance;

    public void GetFile(string fileName, out AssetFile assetFile)
    {
        var span = CollectionsMarshal.AsSpan(_allFiles);
        var result = span.FirstOrNone(file => file.FileInfo.Name.BitwiseEquals(fileName));
        assetFile = result.ValueRef;
    }

    public void LoadAssetFolder()
    {
        foreach (var filePath in AllFilePaths.Value)
        { 
            using var buffer = GetEmbeddedResourceAsBytes(filePath);
            var file = new AssetFile(new(filePath), buffer.Span);
            _allFiles.Add(file);
        }

        // var fileData2 = GetFile("set1.png");
        //File.WriteAllBytes(@"C:\users\maho3\desktop\mySet.png", fileData2.Content);
    }
}