using System.Runtime.InteropServices;
using ImGuiNET;
using Match_3.Service;
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
/// <param name="PhysicalLocation">The full raw path to the Folder-entry</param> 
/// <param name="FileFormat">The format for ALL files in this specific folder</param>
public readonly record struct AssetFolderInfo(string PhysicalLocation, string RelativeLocation, AssetType? FileFormat)
{
    public bool IsRoot => Depth is 0;

    public ReadOnlySpan<char> Root => RelativeLocation.AsSpan(0,RelativeLocation.IndexOf('\\'));

    public ReadOnlySpan<char> Name
    {
        get
        {
            var result = RelativeLocation.AsSpan(RelativeLocation.LastIndexOf('\\') + 1);
            result.Mutable()[0] = char.ToUpper(result.Mutable()[0]);
            return result;
        }
    }

    public int Depth { get; } = RelativeLocation.AsSpan().Count('\\'); 
}

/// <summary>
/// Maps an embedded file (asset) from the project-hierarchy
/// </summary>
/// <param name="Parent">The full path to the file</param>
/// <param name="Content"></param>
public readonly record struct AssetFile(AssetContainer Parent, string FileName, View<byte> Content)
{
    public ReadOnlySpan<char> FullPath => Path.Join(Parent.CurrentInfo.PhysicalLocation, FileName);
    public ReadOnlySpan<char> Name => Path.GetFileName(FullPath);
    public ReadOnlySpan<char> Extension => Path.GetExtension(FullPath);
    public AssetType Format => Parent.CurrentInfo.FileFormat!;
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

    public void AddFiles(Span<byte> fileContent)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
            IgnoreInaccessible = true,
            MatchType = MatchType.Simple,
            BufferSize = 32768 // Use the full 32KB Content (powers of two are better)
        };
        var fileNames = Directory.EnumerateFiles(CurrentInfo.PhysicalLocation.ToString(), "*", options);

        foreach (var fileName in fileNames)
        {
            AssetFile fileData = new(this, fileName, fileContent);
            _allFiles.Add(fileData);
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