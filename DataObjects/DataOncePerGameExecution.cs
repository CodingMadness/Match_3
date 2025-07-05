global using Font = ImGuiNET.ImFontPtr;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Match_3.Service;
using NoAlloq;
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
public readonly record struct AssetFolderInfo(View<char> FullPath)
{
    public AssetFolderInfo(string fullPath) :  this(fullPath.AsSpan())
    {
        
    }
    public ReadOnlySpan<char> Name
    {
        get
        {
            var lastDirPos = FullPath.AsSpan().LastIndexOf('\\') + 1;
            var name = FullPath.AsSpan()[(lastDirPos..)];
            return name;
        }
    }
    public int Depth => FullPath.AsSpan().Count('\\');
    public bool IsRoot => Depth is 0;
}

/// <summary>
/// Maps an embedded file (asset) from the project-hierarchy
/// </summary>
/// <param name="Parent">The full path to the file</param>
/// <param name="Content"></param>
public readonly record struct AssetFile(in AssetFolderInfo Parent, View<char> FileName, View<byte> Content)
{
    private unsafe void GetPointers(out sbyte* ext, out byte* data)
    {
        data = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in Content.First));
        View<char> tmp = Extension.ToAnsiString();
        ext = (sbyte*)Unsafe.AsPointer(ref Unsafe.AsRef(in tmp.First));
    }
    
    private unsafe Texture2D GetTextureFromContent()
    {
        GetPointers(out sbyte* ext, out byte* data);
        var image = Raylib.LoadImageFromMemory(ext, data, Content.Length);
        var res = Raylib.LoadTextureFromImage(image);
        return res;
    }
    private unsafe Sound GetSoundFromContent()
    {
        GetPointers(out sbyte* ext, out byte* data);
        var image = Raylib.LoadWaveFromMemory(ext, data, Content.Length);
        var res = Raylib.LoadSoundFromWave(image);
        return res;
    }
     
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
                ".png" or "jpg" or "jpeg" => new(GetTextureFromContent()),
                ".mp3" or ".wav" or ".ogg" => new(GetSoundFromContent()),
                ".frag" or ".vert" => new(new Shader()),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public override string ToString() => FileName.AsSpan().ToString();
}

public record AssetContainer(in AssetFolderInfo CurrentInfo) : IContainer<List<AssetFile>>
{
    private readonly List<AssetContainer> _subAssetFolders = [];
    private readonly List<AssetFile> _allFiles = [];

    List<AssetFile> IContainer<List<AssetFile>>.VirtualObject => _allFiles;

    public AssetContainer AddSubFolder(in AssetFolderInfo subFolderInfo)
    {
        var folder = new AssetContainer(subFolderInfo);
        _subAssetFolders.Add(folder);
        return folder;
    }
    
    public AssetFile this[string fileNameWithExt]
    {
        get
        {
            var span = CollectionsMarshal.AsSpan(_allFiles);
            return span.First(file => file.Name == fileNameWithExt);
            /* return the specified index here */ 
        }
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
}
