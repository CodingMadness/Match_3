global using Font = ImGuiNET.ImFontPtr;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNext;
using Match_3.Service;
using NetFabric.Hyperlinq;
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

public readonly record struct AssetFile(string FullPath, View<byte> Content)
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
    
    public ReadOnlySpan<char> ParentFolder => Path.GetDirectoryName(FullPath);
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
    public override string ToString() => Name.ToString();
}

public record AssetContainer : IContainer<List<AssetFile>>
{
    private readonly List<AssetFile> _allFiles = [];
    
    List<AssetFile> IContainer<List<AssetFile>>.VirtualObject => _allFiles;
    public AssetFile this[string fileName] => _allFiles.Find(file => file.Name.BitwiseEquals(fileName));

    public void AddFiles(IEnumerable<string> filePathsOfFiles, Func<string,Span<byte>> GetByteData)
    {
        foreach (var fullFilePath in filePathsOfFiles)
        {
            AssetFile file = new(Path.GetFileName(fullFilePath), GetByteData(fullFilePath));
            _allFiles.Add(file);
        }
    }
}
