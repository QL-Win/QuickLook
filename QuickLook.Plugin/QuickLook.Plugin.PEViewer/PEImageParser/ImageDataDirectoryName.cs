namespace QuickLook.Plugin.PEViewer.PEImageParser;

/// <summary>
/// Specifies the name of a data directory entry of a PE image file.
/// </summary>
public enum ImageDataDirectoryName
{
    /// <summary>
    /// Specifies the export table address and size.
    /// </summary>
    ExportTable = 0,

    /// <summary>
    /// Specifies the import table address and size.
    /// </summary>
    ImportTable = 1,

    /// <summary>
    /// Specifies the resource table address and size.
    /// </summary>
    ResourceTable = 2,

    /// <summary>
    /// Specifies the exception table address and size.
    /// </summary>
    ExceptionTable = 3,

    /// <summary>
    /// Specifies the attribute certificate table address and size.
    /// </summary>
    CertificateTable = 4,

    /// <summary>
    /// Specifies the base relocation table address and size.
    /// </summary>
    BaseRelocationTable = 5,

    /// <summary>
    /// Specifies the debug data starting address and size.
    /// </summary>
    DebugDirectory = 6,

    /// <summary>
    /// Reserved, must be zero.
    /// </summary>
    Architecture = 7,

    /// <summary>
    /// Specifies the RVA of the value to be stored in the global pointer register. The size member of this structure must be set to zero.
    /// </summary>
    GlobalPointer = 8,

    /// <summary>
    /// Specifies the thread local storage (TLS) table address and size.
    /// </summary>
    TlsTable = 9,

    /// <summary>
    /// Specifies the load configuration table address and size.
    /// </summary>
    LoadConfigurationTable = 10,

    /// <summary>
    /// Specifies the bound import table address and size.
    /// </summary>
    BoundImportTable = 11,

    /// <summary>
    /// Specifies the import address table address and size.
    /// </summary>
    ImportAddressTable = 12,

    /// <summary>
    /// Specifies the delay import descriptor address and size.
    /// </summary>
    DelayImportDescriptors = 13,

    /// <summary>
    /// Specifies the CLR runtime header address and size.
    /// </summary>
    ClrRuntimeHeader = 14,
}
