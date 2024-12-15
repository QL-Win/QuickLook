using System.Diagnostics;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents a data directory of a PE image file.
/// </summary>
[DebuggerDisplay($"{nameof(ImageDataDirectory)}: Name = {{Name}}, VirtualAddress = {{VirtualAddress}}, Size = {{Size}}")]
public sealed class ImageDataDirectory
{
    /// <summary>
    /// Gets the name of the data directory. This may not be a valid enum value of <see cref="ImageDataDirectoryName" />, if the image has more than 14 data directories.
    /// </summary>
    public ImageDataDirectoryName Name { get; private set; }

    /// <summary>
    /// Gets the address of the first byte of a table or string that Windows uses.
    /// </summary>
    public uint VirtualAddress { get; private set; }

    /// <summary>
    /// Gets size of a table or string that Windows uses.
    /// </summary>
    public uint Size { get; private set; }

    internal ImageDataDirectory(ImageDataDirectoryName name, uint virtualAddress, uint size)
    {
        Name = name;
        VirtualAddress = virtualAddress;
        Size = size;
    }
}
