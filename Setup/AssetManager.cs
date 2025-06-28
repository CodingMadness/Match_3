using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using DotNext.Buffers;
using ImGuiNET;
using Match_3.DataObjects;
using Match_3.Service;
using Raylib_cs;

namespace Match_3.Setup;

public unsafe class AssetManager : IDisposable
{
    private static readonly AssetManager _instance = new();
    private const int LargeEnough2FitAllResources = 1024 * 100; //100KB for now
    private MemoryOwner<byte> fileData = new(ArrayPool<byte>.Shared, LargeEnough2FitAllResources);
    private static readonly Assembly EmbeddedResources = Assembly.GetExecutingAssembly();

    private static readonly Lazy<IEnumerable<string>> AllFolders = new(() =>
    {
        var asmPath = EmbeddedResources.Location;
        var asmName = EmbeddedResources.FullName!;
        var slnName = asmName.AsSpan(0, asmName.IndexOf(','));
        var projPath = asmPath.AsSpan(0, asmPath.IndexOf(slnName) + slnName.Length);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
            IgnoreInaccessible = true,
            MatchType = MatchType.Simple,
            BufferSize = 32768 // Use the full 32KB Content (powers of two are better)
        };
        return Directory.EnumerateDirectories(Path.Join(projPath, "Assets"), "*", options);
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    private AssetManager()
    {
    }

    public static readonly AssetManager Instance = _instance;

    private Span<byte> GetEmbeddedResourceAsBytes(string relativePath)
    {
        var slnName = EmbeddedResources.GetName().Name;
        var fullPath = $"{slnName}.Assets.{relativePath}";

        using var stream = EmbeddedResources.GetManifestResourceStream(fullPath) ??
                           throw new FileNotFoundException("Cannot find resource file.", fullPath);

        var length = (int)stream.Length;
        var usableBuffer = fileData.Span[..length];
        stream.ReadExactly(usableBuffer);
        return usableBuffer;
    }

    private void Get2FileFormatAndData(string relativePath,
        out byte* fileFormat, out byte* data, out int size)
    {
        var buffer = GetEmbeddedResourceAsBytes(relativePath);

        fixed (byte* customPtr = buffer)
        {
            var format = relativePath[relativePath.LastIndexOf('.')..];
            fileFormat = (byte*)Marshal.StringToHGlobalAnsi(format);
            data = customPtr;
            size = buffer.Length;
        }
    }

    public IEnumerable<string> YieldManifestNames()
    {
        return EmbeddedResources.GetManifestResourceNames();
    }

    public IEnumerable<AssetFolderInfo> YieldSubFolderEntries()
    {
        return
            from fullAssetPath in AllFolders.Value
            let projName = EmbeddedResources.GetName().Name
            let startOfProj = fullAssetPath.IndexOf(projName, StringComparison.OrdinalIgnoreCase)
            let relativePath = fullAssetPath.AsSpan(startOfProj + projName.Length + 1).ToString()
            select new AssetFolderInfo(fullAssetPath, relativePath, null);
    }

    public AssetContainer LoadAssetFolder()
    {
        static ReadOnlySpan<char> GetNextSubFolderByNestLvl(ReadOnlySpan<char> currentFolder, int depth)
        {
            int i = 0;
            //we do not want to slice 1x more, the loop shall stop at the last found '/'
            int iterations = depth - 1;

            while (i++ < iterations)
            {
                //sprites/gui/button
                var found = currentFolder.IndexOf('\\');
                currentFolder = currentFolder[(found + 1)..];
            }

            return currentFolder;
        }

        static View<char> GetFolderName(Queue<View<char>> buffer, in AssetFolderInfo folder)
        {
            View<char> result;
            buffer.Enqueue(folder.PhysicalLocation);

            if (folder.Depth > 1)
            {
                var first = buffer.Dequeue();
                result = new(GetNextSubFolderByNestLvl(first, folder.Depth));
            }
            else
            {
                result = buffer.Dequeue();
            }

            return result;
        }

        static AssetContainer GetParentFolder(AssetContainer head, AssetFolderInfo folderInfo, View<char> childFolderName)
        {
            int parentLvl = folderInfo.Depth - 1;
            var foldersFromDepth = head.GetFoldersAtDepth(parentLvl);
            return foldersFromDepth.First(folder =>
                folderInfo.PhysicalLocation.AsSpan()
                    .EndsWith(Path.Join(folder.CurrentInfo.PhysicalLocation, childFolderName)));
        }

        static View<char> GetPhysicalProjectPath(Assembly loaded, ReadOnlySpan<char> folderName)
        {
            var projName = loaded.GetName().Name!;
            var fullAsmPath = loaded.Location;
            int startOfProjOccurence = fullAsmPath.IndexOf(projName, StringComparison.OrdinalIgnoreCase);
            int endOfProjOccurence = startOfProjOccurence + projName.Length;
            var result = fullAsmPath.AsSpan(..endOfProjOccurence);
            int x = 1;
            return Path.Join(result, folderName);
        }

        static View<char> GetEmbeddedAssetFolderPath(Assembly loaded, ReadOnlySpan<char> folderName)
        {
            var physicalPath = GetPhysicalProjectPath(loaded, folderName).AsSpan();
            var projName = EmbeddedResources.GetName().Name!;
            var startOfProj = physicalPath.IndexOf(projName, StringComparison.OrdinalIgnoreCase);
            var relativePath = physicalPath.Slice(startOfProj);
            return relativePath.Replace(['\\'], ['.']);
        }
        
        AssetContainer head = new()
        {
            CurrentInfo = new(
                GetPhysicalProjectPath(EmbeddedResources, "Assets").ToString(),
                GetEmbeddedAssetFolderPath(EmbeddedResources, "Assets").ToString(), null)
        };
        
        using var folderIterator = YieldSubFolderEntries().GetEnumerator();
        AssetContainer parent = head;
        Queue<View<char>> buffer = new(2);
        int currDepth = 1;

        while (folderIterator.MoveNext())
        {
            var folderInfo = folderIterator.Current;
            var childFolderName = GetFolderName(buffer, in folderInfo);

            if (folderInfo.Depth > currDepth)
            {
                parent = GetParentFolder(parent, folderInfo, childFolderName);
                currDepth++;
            }

            parent.AddSubFolder(folderInfo);
        }

        return head;
    }

    public void Dispose()
    {
        fileData.Dispose();
    }
}