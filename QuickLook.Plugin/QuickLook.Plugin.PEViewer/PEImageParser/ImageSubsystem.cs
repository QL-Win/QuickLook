namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Specifies the values for the <see cref="ImageOptionalHeader.Subsystem" /> property of the optional header of a PE image file.
/// </summary>
public enum ImageSubsystem : ushort
{
    /// <summary>
    /// Specifies an unknown subsystem.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Specifies device drivers and native Windows processes.
    /// </summary>
    Native = 1,

    /// <summary>
    /// Specifies the Windows graphical user interface (GUI) subsystem.
    /// </summary>
    WindowsGui = 2,

    /// <summary>
    /// Specifies the Windows character subsystem.
    /// </summary>
    WindowsCui = 3,

    /// <summary>
    /// Specifies the OS/2 character subsystem.
    /// </summary>
    OS2Cui = 5,

    /// <summary>
    /// Specifies the Posix character subsystem.
    /// </summary>
    PosixCui = 7,

    /// <summary>
    /// Specifies a native Win9x driver.
    /// </summary>
    NativeWindows = 8,

    /// <summary>
    /// Specifies Windows CE.
    /// </summary>
    WindowsCEGui = 9,

    /// <summary>
    /// Specifies an Extensible Firmware Interface (EFI) application.
    /// </summary>
    EfiApplication = 10,

    /// <summary>
    /// Specifies an EFI driver with boot services.
    /// </summary>
    EfiBootServiceDriver = 11,

    /// <summary>
    /// Specifies an EFI driver with run-time services.
    /// </summary>
    EfiRuntimeDriver = 12,

    /// <summary>
    /// Specifies an EFI ROM image.
    /// </summary>
    EfiRom = 13,

    /// <summary>
    /// Specifies XBOX.
    /// </summary>
    XBox = 14,

    /// <summary>
    /// Specifies a Windows boot application.
    /// </summary>
    WindowsBootApplication = 16,
}
