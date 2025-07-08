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

public readonly record struct AssetFileInfo(string FullPath)
{
    public ReadOnlySpan<char> ParentFolder => Path.GetDirectoryName(FullPath);
    public ReadOnlySpan<char> Name => Path.GetFileName(FullPath);
    public ReadOnlySpan<char> Extension => Path.GetExtension(FullPath);
    public ReadOnlySpan<char> Type
    {
        get
        {
            return Extension switch
            {
                ".otf" or ".ttf" => "Font",
                ".png" or "jpg" or "jpeg" => "Texture",
                ".mp3" or ".wav" or ".ogg" => "Sound",
                ".frag" or ".vert" => "Shader",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public override string ToString() => Name.ToString();
}

public readonly record struct AssetFile(in AssetFileInfo FileInfo, View<byte> Content) : IContainer<AssetFileInfo>
{
    private unsafe void GetPointers(out sbyte* ext, out byte* content)
    {
        fixed (byte* customPtr = Content)
        {
            ext = (sbyte*)Marshal.StringToHGlobalAnsi(FileInfo.Extension.ToString());
            content = customPtr;
        }
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
    public AssetType Format
    {
        get
        { 
            return FileInfo.Type switch
            {
                "Font" => new(new Font()),
                "Texture" => new(GetTextureFromContent()),
                "Sound" => new(GetSoundFromContent()),
                "Shader" => new(new Shader()),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    AssetFileInfo IContainer<AssetFileInfo>.VirtualObject => FileInfo;
}

 