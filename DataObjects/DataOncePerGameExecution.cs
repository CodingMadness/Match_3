global using Font = ImGuiNET.ImFontPtr;
using System.Runtime.InteropServices;
using OneOf;
using Raylib_cs;

namespace Match_3.DataObjects;

/// <summary>
/// Discriminated Union to represent one of the listed types based on custom-conditions
/// </summary>
public class AssetType : OneOfBase<Font, Texture2D, Sound, Shader>
{
    public AssetType(OneOf<Font, Texture2D, Sound, Shader> input) : base(input)
    {
    }
}

/// <summary>
/// Wraps all necessary information about an AssetType-folder 
/// </summary>
/// <param name="FullPath">The full raw path to the Folder-entry</param>
public readonly record struct AssetFolderInfo(string FullPath, IEnumerable<AssetFileInfo> AssetFiles)
{
    public ReadOnlySpan<char> Name
    {
        get
        {
            var lastDirPos = FullPath.LastIndexOf('\\') + 1;
            var name = FullPath.AsSpan(lastDirPos..);
            return name;
        }
    }
    public int Depth => FullPath.Count('\\');
    public bool IsRoot => Depth is 0;
}

/// <summary>
/// Maps an embedded file (asset) from the project-hierarchy
/// </summary>
/// <param name="Parent">The full path to the file</param>
/// <param name="Content"></param>
public readonly record struct AssetFileInfo(in AssetFolderInfo Parent, View<char> FileName, View<byte> Content)
{
    public ReadOnlySpan<char> FullPath => Path.Join(Parent.FullPath, FileName);
    public ReadOnlySpan<char> Name => Path.GetFileName(FullPath);
    public ReadOnlySpan<char> Extension => Path.GetExtension(FullPath);
    public AssetType Format
    {
        get
        {
            return Extension switch
            {
                ".otf" or ".ttf" => new(new Font()),
                ".png" or "jpg" or "jpeg" => new(new Texture2D()),
                ".mp3" or ".wav" or ".ogg" => new(new Sound()),
                ".frag" or ".vert" => new(new Shader()),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

public class AssetContainer : IContainer<List<AssetFileInfo>>
{
    private readonly List<AssetContainer> _subAssetFolders = [];
    private readonly List<AssetFileInfo> _allFiles = [];
    
    List<AssetFileInfo> IContainer<List<AssetFileInfo>>.VirtualObject => _allFiles;
    public required AssetFolderInfo CurrentInfo { get; init; }
    public void AddSubFolder(in AssetFolderInfo subFolderInfo)
    {
        var folder = new AssetContainer { CurrentInfo = subFolderInfo };
        _subAssetFolders.Add(folder);
    }
    public void AddFiles(IEnumerable<(string fileName, View<byte> fileData)> allFileInfos)
    {
        foreach (var fileInfo in allFileInfos)
        {
            _allFiles.Add(new(CurrentInfo, fileInfo.fileName, fileInfo.fileData));
        }
    }
    public IEnumerable<AssetContainer> GetFoldersAtDepth(int targetDepth)
    {
        var queue = new Queue<AssetContainer>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
        
            if (current.CurrentInfo.Depth == targetDepth)
            {
                yield return current;
            }
            else if (current.CurrentInfo.Depth < targetDepth)
            {
                foreach (ref var subfolder in CollectionsMarshal.AsSpan(current._subAssetFolders))
                {
                    queue.Enqueue(subfolder);
                }
            }
        }
    }
    public override string ToString() => CurrentInfo.ToString();
}