using System.Runtime.InteropServices;
using ImGuiNET;
using OneOf;
using Raylib_cs;

namespace Match_3.DataObjects;

/// <summary>
/// Discriminated Union to represent one of the listed types based on custom-conditions
/// </summary>
public class AssetType : OneOfBase<ImFontPtr, Texture2D, Sound, Shader>
{
    public AssetType(OneOf<ImFontPtr, Texture2D, Sound, Shader> input) : base(input)
    {
    }
}

/// <summary>
/// Wraps all necessary information about an AssetType-folder 
/// </summary>
/// <param name="FullPath">The full raw path to the Folder-entry</param>
public readonly record struct AssetFolderInfo(string FullPath)
{
    public bool IsRoot => Depth is 0;
    public ReadOnlySpan<char> RelativeLocation => IsRoot ? [] : FullPath.AsSpan(FullPath.IndexOf('\\'));
    public ReadOnlySpan<char> Name => IsRoot ? FullPath : RelativeLocation.Slice(0, RelativeLocation.IndexOf('\\'));
    public int Depth => FullPath.Contains('\\') ?
                        FullPath.AsSpan(0, FullPath.IndexOf(Name)).Count('\\') : 0; 
}

/// <summary>
/// Maps an embedded file (asset) from the project-hierarchy
/// </summary>
/// <param name="Parent">The full path to the file</param>
/// <param name="Content"></param>
public readonly record struct AssetFile(AssetContainer Parent, string FileName, View<byte> Content)
{
    public ReadOnlySpan<char> FullPath => Path.Join(Parent.CurrentInfo.FullPath, FileName);
    public ReadOnlySpan<char> Name => Path.GetFileName(FullPath);
    public ReadOnlySpan<char> Extension => Path.GetExtension(FullPath);
    // public AssetType Format => Parent.CurrentInfo.FileFormat!;
}

public class AssetContainer : IContainer<List<AssetFile>>
{
    private readonly List<AssetContainer> _subAssetFolders = [];
    private readonly List<AssetFile> _allFiles = [];
    
    List<AssetFile> IContainer<List<AssetFile>>.VirtualObject => _allFiles;
    public required AssetFolderInfo CurrentInfo { get; init; }
    
    public void AddSubFolder(AssetFolderInfo subFolderInfo)
    {
        var folder = new AssetContainer { CurrentInfo = subFolderInfo };
         
        _subAssetFolders.Add(folder);
    }

    public void AddFiles(Span<byte> content)
    {
         
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