using System;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Specifies the values for the <see cref="ImageCoffHeader.Characteristics" /> property of the COFF header of a PE image file.
/// </summary>
[Flags]
public enum ImageCharacteristics : ushort
{
    /// <summary>
    /// Specifies that the file does not contain base relocations and must therefore be loaded at its preferred base address. If the base address is not available, the loader reports an error. The default behavior of the linker is to strip base relocations from executable (EXE) files.
    /// </summary>
    RelocationStripped = 0x1,

    /// <summary>
    /// Specifies that the image file is valid and can be run. If this flag is not set, it indicates a linker error.
    /// </summary>
    Executable = 0x2,

    /// <summary>
    /// Specifies that COFF line numbers have been removed. This flag is deprecated and should be zero.
    /// </summary>
    LineNumbersStripped = 0x4,

    /// <summary>
    /// Specifies that COFF symbol table entries for local symbols have been removed. This flag is deprecated and should be zero.
    /// </summary>
    SymbolsStripped = 0x8,

    /// <summary>
    /// Specifies to aggressively trim working set. This flag is deprecated for Windows 2000 and later and must be zero.
    /// </summary>
    AggressivelyTrimWorkingSet = 0x10,

    /// <summary>
    /// Specifies that the application can handle > 2 GB addresses.
    /// </summary>
    LargeAddressAware = 0x20,

    /// <summary>
    /// Specifies little endian: The least significant bit (LSB) precedes the most significant bit (MSB) in memory. This flag is deprecated and should be zero.
    /// </summary>
    BytesReversedLo = 0x80,

    /// <summary>
    /// Specifies that the machine is based on a 32-bit-word architecture.
    /// </summary>
    Machine32 = 0x100,

    /// <summary>
    /// Specifies that debugging information is removed from the image file.
    /// </summary>
    DebugStripped = 0x200,

    /// <summary>
    /// Specifies that if the image is on removable media, to fully load it and copy it to the swap file.
    /// </summary>
    RemovableRunFromSwap = 0x400,

    /// <summary>
    /// Specifies that if the image is on network media, to fully load it and copy it to the swap file.
    /// </summary>
    NetRunFromSwap = 0x800,

    /// <summary>
    /// Specifies that the image file is a system file, not a user program.
    /// </summary>
    System = 0x1000,

    /// <summary>
    /// Specifies that the image file is a dynamic-link library (DLL). Such files are considered executable files for almost all purposes, although they cannot be directly run.
    /// </summary>
    Dll = 0x2000,

    /// <summary>
    /// Specifies that the file should be run only on a uniprocessor machine.
    /// </summary>
    UpSystem = 0x4000,

    /// <summary>
    /// Specifies big endian: the MSB precedes the LSB in memory. This flag is deprecated and should be zero.
    /// </summary>
    BytesReversedHi = 0x8000
}
