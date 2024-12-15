using System;

namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Specifies the values for the <see cref="ImageOptionalHeader.DllCharacteristics" /> property of the optional header of a PE image file.
/// </summary>
[Flags]
public enum ImageDllCharacteristics : ushort
{
    /// <summary>
    /// Specifies that the image can handle a high entropy 64-bit virtual address space.
    /// </summary>
    HighEntropyVA = 0x20,

    /// <summary>
    /// Specifies that the DLL can be relocated at load time.
    /// </summary>
    DynamicBase = 0x40,

    /// <summary>
    /// Specifies that Code Integrity checks are enforced.
    /// </summary>
    ForceIntegrity = 0x80,

    /// <summary>
    /// Specifies that the image is NX compatible.
    /// </summary>
    NxCompatible = 0x100,

    /// <summary>
    /// Specifies that the image is isolation aware, but the image is not isolated.
    /// </summary>
    IsolationAware = 0x200,

    /// <summary>
    /// Specifies that the image does not use structured exception (SE) handling. No SE handler may be called in the image.
    /// </summary>
    NoSEH = 0x400,

    /// <summary>
    /// Specifies that the image is not bound.
    /// </summary>
    DoNotBind = 0x800,

    /// <summary>
    /// Specifies that the image must execute in an AppContainer.
    /// </summary>
    AppContainer = 0x1000,

    /// <summary>
    /// Specifies that the image is a WDM driver.
    /// </summary>
    WdmDriver = 0x2000,

    /// <summary>
    /// Specifies that the image supports Control Flow Guard.
    /// </summary>
    ControlFlowGuard = 0x4000,

    /// <summary>
    /// Specifies that the image is terminal Server aware.
    /// </summary>
    TerminalServerAware = 0x8000,
}
