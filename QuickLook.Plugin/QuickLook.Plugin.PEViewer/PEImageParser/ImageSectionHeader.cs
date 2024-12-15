namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Represents a section header of a PE image file.
/// </summary>
public sealed class ImageSectionHeader
{
    /// <summary>
    /// Gets an 8-byte, null-padded UTF-8 encoded string. If the string is exactly 8 characters long, there is no terminating null. For longer names, this property contains a slash (/) that is followed by an ASCII representation of a decimal number that is an offset into the string table. Executable images do not use a string table and do not support section names longer than 8 characters. Long names in object files are truncated if they are emitted to an executable file.
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Gets the total size of the section when loaded into memory. If this value is greater than <see cref="SizeOfRawData" />, the section is zero-padded. This property is valid only for executable images and should be set to zero for object files.
    /// </summary>
    public uint VirtualSize { get; internal set; }

    /// <summary>
    /// For executable images, Gets the address of the first byte of the section relative to the image base when the section is loaded into memory. For object files, this property is the address of the first byte before relocation is applied; for simplicity, compilers should set this to zero. Otherwise, it is an arbitrary value that is subtracted from offsets during relocation.
    /// </summary>
    public uint VirtualAddress { get; internal set; }

    /// <summary>
    /// Gets the size of the section (for object files) or the size of the initialized data on disk (for image files). For executable images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment" /> from the optional header. If this is less than <see cref="VirtualSize" />, the remainder of the section is zero-filled. Because the <see cref="SizeOfRawData" /> property is rounded but the <see cref="VirtualSize" /> property is not, it is possible for <see cref="SizeOfRawData" /> to be greater than <see cref="VirtualSize" /> as well. When a section contains only uninitialized data, this property should be zero.
    /// </summary>
    public uint SizeOfRawData { get; internal set; }

    /// <summary>
    /// Gets the file pointer to the first page of the section within the COFF file. For executable images, this must be a multiple of <see cref="ImageOptionalHeader.FileAlignment" /> from the optional header. For object files, the value should be aligned on a 4-byte boundary for best performance. When a section contains only uninitialized data, this property should be zero.
    /// </summary>
    public uint PointerToRawData { get; internal set; }

    /// <summary>
    /// Gets the file pointer to the beginning of relocation entries for the section. This is set to zero for executable images or if there are no relocations.
    /// </summary>
    public uint PointerToRelocations { get; internal set; }

    /// <summary>
    /// Gets the file pointer to the beginning of line-number entries for the section. This is set to zero if there are no COFF line numbers. This value should be zero for an image because COFF debugging information is deprecated.
    /// </summary>
    public uint PointerToLineNumbers { get; internal set; }

    /// <summary>
    /// Gets the number of relocation entries for the section. This is set to zero for executable images.
    /// </summary>
    public ushort NumberOfRelocations { get; internal set; }

    /// <summary>
    /// Gets the number of line-number entries for the section. This value should be zero for an image because COFF debugging information is deprecated.
    /// </summary>
    public ushort NumberOfLineNumbers { get; internal set; }

    /// <summary>
    /// Gets the flags that describe the characteristics of the section.
    /// </summary>
    public ImageSectionFlags Characteristics { get; internal set; }

    internal ImageSectionHeader()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Returns the name of this <see cref="ImageSectionHeader" />.
    /// </summary>
    /// <returns>
    /// The name of this <see cref="ImageSectionHeader" />.
    /// </returns>
    public override string ToString()
    {
        return Name;
    }
}
