namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents the optional header of a PE image file for x64 assemblies.
/// </summary>
public sealed class ImageOptionalHeader64 : ImageOptionalHeader
{
    /// <summary>
    /// Gets the preferred address of the first byte of image when loaded into memory; must be a multiple of 64 K. The default for DLLs is 0x10000000. The default for Windows CE EXEs is 0x00010000. The default for Windows NT, Windows 2000, Windows XP, Windows 95, Windows 98, and Windows Me is 0x00400000.
    /// </summary>
    public ulong ImageBase { get; internal set; }

    /// <summary>
    /// Gets the size of the stack to reserve. Only <see cref="SizeOfStackCommit" /> is committed; the rest is made available one page at a time until the reserve size is reached.
    /// </summary>
    public ulong SizeOfStackReserve { get; internal set; }

    /// <summary>
    /// Gets the size of the stack to commit.
    /// </summary>
    public ulong SizeOfStackCommit { get; internal set; }

    /// <summary>
    /// Gets the size of the local heap space to reserve. Only <see cref="SizeOfHeapCommit" /> is committed; the rest is made available one page at a time until the reserve size is reached.
    /// </summary>
    public ulong SizeOfHeapReserve { get; internal set; }

    /// <summary>
    /// Gets the size of the local heap space to commit.
    /// </summary>
    public ulong SizeOfHeapCommit { get; internal set; }

    internal ImageOptionalHeader64()
    {
    }
}
