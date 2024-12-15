namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents the COFF header of a PE image file.
/// </summary>
public sealed class ImageCoffHeader
{
    /// <summary>
    /// Gets the number that identifies the type of target machine.
    /// </summary>
    public ImageMachineType Machine { get; internal set; }

    /// <summary>
    /// Gets the number of sections. This indicates the size of the section table, which immediately follows the headers.
    /// </summary>
    public ushort NumberOfSections { get; internal set; }

    /// <summary>
    /// Gets the low 32 bits of the number of seconds since 01.01.1970 00:00:00, that indicates when the file was created.
    /// </summary>
    public uint TimeDateStamp { get; internal set; }

    /// <summary>
    /// Gets the file offset of the COFF symbol table, or zero if no COFF symbol table is present. This value should be zero for an image because COFF debugging information is deprecated.
    /// </summary>
    public uint PointerToSymbolTable { get; internal set; }

    /// <summary>
    /// Gets the number of entries in the symbol table. This data can be used to locate the string table, which immediately follows the symbol table. This value should be zero for an image because COFF debugging information is deprecated.
    /// </summary>
    public uint NumberOfSymbols { get; internal set; }

    /// <summary>
    /// Gets the size of the optional header, which is required for executable files but not for object files. This value should be zero for an object file.
    /// </summary>
    public ushort SizeOfOptionalHeader { get; internal set; }

    /// <summary>
    /// Gets the flags that indicate the attributes of the file.
    /// </summary>
    public ImageCharacteristics Characteristics { get; internal set; }

    internal ImageCoffHeader()
    {
    }
}
