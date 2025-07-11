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

public readonly record struct AssetFile(string FullFilePath, View<byte> Content) : IPublishable<AssetFileInfo>
{
    private readonly AssetFileInfo _fileInfo = new(FullFilePath);
    
    private unsafe Texture2D GetTexture()
    {
        // Convert extension to ANSI
        var extAnsi = Marshal.StringToHGlobalAnsi(_fileInfo.Extension.ToString());
        
        try
        {
            fixed (byte* dataPtr = Content)
            {
                // Load the image while the content is pinned
                var image = Raylib.LoadImageFromMemory((sbyte*)extAnsi, dataPtr, Content.Length);
                var texture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image); // Don't forget to unload the image
                return texture;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extAnsi);
        }
    }
    
    public AssetType Format
    {
        get
        { 
            return _fileInfo.Type switch
            {
                "Texture" => new(GetTexture()),
                "Font" => new(new Font()),
                "Sound" => new(new Sound()),
                "Shader" => new(new Shader()),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public ReadOnlySpan<char> Name => _fileInfo.Name;
    
    AssetFileInfo IPublishable<AssetFileInfo>.VirtualObject => _fileInfo;
}

 