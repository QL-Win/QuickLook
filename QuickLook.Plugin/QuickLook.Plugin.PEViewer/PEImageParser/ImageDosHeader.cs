namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents the DOS header of a PE image file.
/// </summary>
public sealed class ImageDosHeader
{
    /// <summary>
    /// Specifies the amount of bytes on last page of the file.
    /// </summary>
    public ushort LastPageSize { get; internal set; }

    /// <summary>
    /// Specifies the amount of pages in the file.
    /// </summary>
    public ushort PageCount { get; internal set; }

    /// <summary>
    /// Specifies the amount of relocations in the file.
    /// </summary>
    public ushort RelocationCount { get; internal set; }

    /// <summary>
    /// Specifies the size of header in paragraphs.
    /// </summary>
    public ushort HeaderSize { get; internal set; }

    /// <summary>
    /// Specifies the minimum extra paragraphs needed.
    /// </summary>
    public ushort MinAlloc { get; internal set; }

    /// <summary>
    /// Specifies the maximum extra paragraphs needed.
    /// </summary>
    public ushort MaxAlloc { get; internal set; }

    /// <summary>
    /// Specifies the initial (relative) SS value.
    /// </summary>
    public ushort InitialSS { get; internal set; }

    /// <summary>
    /// Specifies the initial SP value.
    /// </summary>
    public ushort InitialSP { get; internal set; }

    /// <summary>
    /// Specifies the file checksum.
    /// </summary>
    public ushort Checksum { get; internal set; }

    /// <summary>
    /// Specifies the initial IP value.
    /// </summary>
    public ushort InitialIP { get; internal set; }

    /// <summary>
    /// Specifies the initial (relative) CS value.
    /// </summary>
    public ushort InitialCS { get; internal set; }

    /// <summary>
    /// Specifies the file address of the relocation table.
    /// </summary>
    public ushort RelocationOffset { get; internal set; }

    /// <summary>
    /// Specifies the overlay number.
    /// </summary>
    public ushort OverlayNumber { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved1 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved2 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved3 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved4 { get; internal set; }

    /// <summary>
    /// Specifies the OEM Identifier.
    /// </summary>
    public ushort OemIdentifier { get; internal set; }

    /// <summary>
    /// Specifies the OEM identifier.
    /// </summary>
    public ushort OemInformation { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved5 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved6 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved7 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved8 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved9 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved10 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved11 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved12 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved13 { get; internal set; }

    /// <summary>
    /// Reserved.
    /// </summary>
    public ushort Reserved14 { get; internal set; }

    /// <summary>
    /// Specifies the file address of new EXE header.
    /// </summary>
    public uint PEHeaderOffset { get; internal set; }

    internal ImageDosHeader()
    {
    }
}
