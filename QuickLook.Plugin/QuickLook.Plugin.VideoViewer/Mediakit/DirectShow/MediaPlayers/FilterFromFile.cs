using DirectShowLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

// Code of MediaPortal (www.team-mediaportal.com)

namespace WPFMediaKit.DirectShow.MediaPlayers;

[ComVisible(false)]
[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
internal interface IClassFactory
{
    void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid refiid, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

    void LockServer(bool fLock);
}

/// <summary>
/// Helper class to load <see cref="IBaseFilter"/>s from a file. It's not needed that the filter is registered.
/// </summary>
public static class FilterFromFile
{
    #region LoadLibraryEx Flags

    private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
    private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

    #endregion LoadLibraryEx Flags

    #region Native API wrapper methods

    /// <summary>
    /// The GetProcAddress function retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
    /// </summary>
    /// <param name="hModule">Handle to the DLL module that contains the function or variable. The LoadLibrary or GetModuleHandle function returns this handle.</param>
    /// <param name="lpProcName">Pointer to a null-terminated string containing the function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
    /// <returns>If the function succeeds, the return value is the address of the exported function or variable.<br></br><br>If the function fails, the return value is NULL. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    /// <summary>
    /// Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.
    /// </summary>
    /// <param name="lpFileName">Pointer to a null-terminated string that names the executable module (either a .dll or .exe file). The name specified is the file name of the module and is not related to the name stored in the library module itself, as specified by the LIBRARY keyword in the module-definition (.def) file.</param>
    /// <param name="hFile">This parameter is reserved for future use. It must be IntPtr.Zero.</param>
    /// <param name="dwFlags">The action to be taken when loading the module. If no flags are specified, the behavior of this function is identical to that of the <see cref="LoadLibrary"/> function.</param>
    /// <returns>If the function succeeds, the return value is a handle to the module.<br/>If the function fails, the return value is NULL. To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    /// <summary>
    /// The FreeLibrary function decrements the reference count of the loaded dynamic-link library (DLL). When the reference count reaches zero, the module is unmapped from the address space of the calling process and the handle is no longer valid.
    /// </summary>
    /// <param name="hLibModule">Handle to the loaded DLL module. The LoadLibrary or GetModuleHandle function returns this handle.</param>
    /// <returns>If the function succeeds, the return value is nonzero.<br></br><br>If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
    [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", CharSet = CharSet.Ansi)]
    static extern int FreeLibrary(IntPtr hLibModule);

    #endregion Native API wrapper methods

    #region Helper class DllList

    /// <summary>
    /// Holds a list of dll handles and unloads the dlls in the destructor.
    /// </summary>
    private class DllList
    {
        private readonly List<IntPtr> _handleList = new List<IntPtr>();

        public void AddDllHandle(IntPtr dllHandle)
        {
            lock (_handleList)
            {
                _handleList.Add(dllHandle);
            }
        }

        ~DllList()
        {
            foreach (IntPtr dllHandle in _handleList)
            {
                try
                {
                    FreeLibrary(dllHandle);
                }
                catch { }
            }
        }
    }

    #endregion Helper class DllList

    delegate int DllGetClassObject(ref Guid classId, ref Guid interfaceId, [Out, MarshalAs(UnmanagedType.Interface)] out object ppunk);

    private static readonly DllList DLL_LIST = new DllList();

    /// <summary>
    /// Gets a class factory for a specific COM Class ID.
    /// </summary>
    /// <param name="dllName">The dll where the COM class is implemented.</param>
    /// <param name="filterPersistClass">The requested Class ID.</param>
    /// <returns>IClassFactory instance used to create instances of that class.</returns>
    internal static IClassFactory GetClassFactory(string dllName, Guid filterPersistClass)
    {
        // Load the class factory from the dll.
        // By specifying the flags we allow to search for dependencies in the same folder as the file to be loaded
        // as well as default dirs like System32 and the Application folder.
        IntPtr dllHandle = LoadLibraryEx(dllName, IntPtr.Zero,
          LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        if (dllHandle == IntPtr.Zero)
            return null;

        // Keep a reference to the dll until the process\AppDomain dies.
        DLL_LIST.AddDllHandle(dllHandle);

        //Get a pointer to the DllGetClassObject function
        IntPtr dllGetClassObjectPtr = GetProcAddress(dllHandle, "DllGetClassObject");
        if (dllGetClassObjectPtr == IntPtr.Zero)
            return null;

        // Convert the function pointer to a .net delegate.
        DllGetClassObject dllGetClassObject = (DllGetClassObject)Marshal.GetDelegateForFunctionPointer(dllGetClassObjectPtr, typeof(DllGetClassObject));

        // Call the DllGetClassObject to retreive a class factory for out Filter class.
        Guid baseFilterGuid = filterPersistClass;
        Guid classFactoryGuid = typeof(IClassFactory).GUID;
        Object unk;
        if (dllGetClassObject(ref baseFilterGuid, ref classFactoryGuid, out unk) != 0)
            return null;

        return (unk as IClassFactory);
    }

    /// <summary>
    /// Loads an COM .dll or .ax and creates an instance of the given Interface with IID <paramref name="interfaceId"/>.
    /// </summary>
    /// <param name="dllName">Filename of a .dll or .ax component</param>
    /// <param name="interfaceId">Interface to create an object instance for</param>
    /// <param name="useAssemblyRelativeLocation">Combine the given file name to a full path</param>
    /// <returns>Instance or <c>null</c></returns>
    public static IBaseFilter LoadFilterFromDll(string dllName, Guid interfaceId, bool useAssemblyRelativeLocation)
    {
        // Get a ClassFactory for our classID
        string dllPath = useAssemblyRelativeLocation ? BuildAssemblyRelativePath(dllName) : dllName;
        IClassFactory classFactory = GetClassFactory(dllPath, interfaceId);
        if (classFactory == null)
            return null;

        // And create an IFilter instance using that class factory
        Guid baseFilterGuid = typeof(IBaseFilter).GUID;
        object obj;
        classFactory.CreateInstance(null, ref baseFilterGuid, out obj);
        return (obj as IBaseFilter);
    }

    /// <summary>
    /// Builds a full path for a given <paramref name="fileName"/> that is located in the same folder as the <see cref="Assembly.GetCallingAssembly"/>.
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Combined path</returns>
    public static string BuildAssemblyRelativePath(string fileName)
    {
        string executingPath = Assembly.GetCallingAssembly().Location;
        return Path.Combine(Path.GetDirectoryName(executingPath), fileName);
    }
}
