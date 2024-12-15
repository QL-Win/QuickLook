using System;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Specifies the values for the <see cref="ImageSectionHeader.Characteristics" /> property of a section header of a PE image file.
/// </summary>
[Flags]
public enum ImageSectionFlags : uint
{
    /// <summary>
    /// Specifies that the section should not be padded to the next boundary. This flag is obsolete and is replaced by <see cref="Align1" />. This is valid only for object files.
    /// </summary>
    NoPadding = 0x8,

    /// <summary>
    /// Specifies that the section contains executable code.
    /// </summary>
    ContainsCode = 0x20,

    /// <summary>
    /// Specifies that the section contains initialized data.
    /// </summary>
    ContainsInitializedData = 0x40,

    /// <summary>
    /// Specifies that the section contains uninitialized data.
    /// </summary>
    ContainsUninitializedData = 0x80,

    /// <summary>
    /// Specifies that the section contains comments or other information. The .drectve section has this type. This is valid for object files only.
    /// </summary>
    ContainsInformation = 0x200,

    /// <summary>
    /// Specifies that the section will not become part of the image. This is valid only for object files.
    /// </summary>
    Remove = 0x800,

    /// <summary>
    /// Specifies that the section contains COMDAT data. This is valid only for object files.
    /// </summary>
    ContainsComdat = 0x1000,

    /// <summary>
    /// Specifies that the section contains data referenced through the global pointer (GP).
    /// </summary>
    ContainsGlobalPointerData = 0x8000,

    /// <summary>
    /// Specifies to align data on a 1-byte boundary. Valid only for object files.
    /// </summary>
    Align1 = 0x100000,

    /// <summary>
    /// Specifies to align data on a 2-byte boundary. Valid only for object files.
    /// </summary>
    Align2 = 0x200000,

    /// <summary>
    /// Specifies to align data on a 4-byte boundary. Valid only for object files.
    /// </summary>
    Align4 = 0x300000,

    /// <summary>
    /// Specifies to align data on a 8-byte boundary. Valid only for object files.
    /// </summary>
    Align8 = 0x400000,

    /// <summary>
    /// Specifies to align data on a 16-byte boundary. Valid only for object files.
    /// </summary>
    Align16 = 0x500000,

    /// <summary>
    /// Specifies to align data on a 32-byte boundary. Valid only for object files.
    /// </summary>
    Align32 = 0x600000,

    /// <summary>
    /// Specifies to align data on a 64-byte boundary. Valid only for object files.
    /// </summary>
    Align64 = 0x700000,

    /// <summary>
    /// Specifies to align data on a 128-byte boundary. Valid only for object files.
    /// </summary>
    Align128 = 0x800000,

    /// <summary>
    /// Specifies to align data on a 256-byte boundary. Valid only for object files.
    /// </summary>
    Align256 = 0x900000,

    /// <summary>
    /// Specifies to align data on a 512-byte boundary. Valid only for object files.
    /// </summary>
    Align512 = 0xa00000,

    /// <summary>
    /// Specifies to align data on a 1024-byte boundary. Valid only for object files.
    /// </summary>
    Align1024 = 0xb00000,

    /// <summary>
    /// Specifies to align data on a 2048-byte boundary. Valid only for object files.
    /// </summary>
    Align2048 = 0xc00000,

    /// <summary>
    /// Specifies to align data on a 4096-byte boundary. Valid only for object files.
    /// </summary>
    Align4096 = 0xd00000,

    /// <summary>
    /// Specifies to align data on a 8192-byte boundary. Valid only for object files.
    /// </summary>
    Align8192 = 0xe00000,

    /// <summary>
    /// Specifies that the section contains extended relocations.
    /// </summary>
    ContainsExtendedRelocations = 0x1000000,

    /// <summary>
    /// Specifies that the section can be discarded as needed.
    /// </summary>
    Discardable = 0x2000000,

    /// <summary>
    /// Specifies that the section cannot be cached.
    /// </summary>
    NotCached = 0x4000000,

    /// <summary>
    /// Specifies that the section is not pageable.
    /// </summary>
    NotPaged = 0x8000000,

    /// <summary>
    /// Specifies that the section can be shared in memory.
    /// </summary>
    Shared = 0x10000000,

    /// <summary>
    /// Specifies that the section can be executed as code.
    /// </summary>
    Execute = 0x20000000,

    /// <summary>
    /// Specifies that the section can be read.
    /// </summary>
    Read = 0x40000000,

    /// <summary>
    /// Specifies that the section can be written to.
    /// </summary>
    Write = 0x80000000,
}
