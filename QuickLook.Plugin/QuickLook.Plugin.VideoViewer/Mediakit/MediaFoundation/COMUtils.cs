using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace WPFMediaKit.MediaFoundation;

public class COMUtil
{
    /// <summary>
    /// Check if a COM Object is available
    /// </summary>
    /// <param name="clsid">The CLSID of this object</param>
    /// <returns>true if the object is available, false if not</returns>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static bool IsObjectInstalled(Guid clsid)
    {
        object comobj = null;
        try
        {
            Type type = Type.GetTypeFromCLSID(clsid);
            comobj = Activator.CreateInstance(type);
            return comobj != null;
        }
        catch (Exception)
        {
            return false;
        }
        finally { SafeRelease(comobj); }
    }

    /// <summary>
    /// Release a ComObject
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>ref counter of the object</returns>
    public static int SafeRelease(object obj)
    {
        return (obj != null && Marshal.IsComObject(obj)) ? Marshal.ReleaseComObject(obj) : 0;
    }

    /// <summary>
    /// Try (final) release a Com-Object and set the obj to null
    /// </summary>
    /// <typeparam name="I"></typeparam>
    /// <param name="comobj"></param>
    /// <returns>true if object is released (not null and a com object)</returns>
    public static bool TryFinalRelease<I>(ref I comobj)
    {
        if (comobj != null)
        {
            if (Marshal.IsComObject(comobj))
                while (Marshal.ReleaseComObject(comobj) > 0) ;

            comobj = default(I);
            return true;
        }
        return false;
    }

    //*********
    //Beispiel
    //*********
    // Guid vom filter
    // Guid mpaguid = new Guid("3D446B6F-71DE-4437-BE15-8CE47174340F");
    // falls nötig, den pfad zur *.ax bzw. dll datei
    // IBaseFilter baseFilter = CreateFromDll<IBaseFilter>("MpaDecFilter.ax", mpaguid);
    // graphBuilder.AddFilter(baseFilter, "MPEG1 Audio Decoder");

    #region Create COM-Object from file

    /// <summary>Function to get a COM object from file (dll)</summary>
    /// <param name="dllName">a (unmanaged) dll-file where the COM object is implemented</param>
    /// <param name="mpaguid">objects Guid</param>
    /// <returns>a interface or null if not loaded</returns>
    /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if the method can't creat COM-object</exception>
    /// <exception cref="System.Runtime.DllNotFoundException">Thrown if the dll not found</exception>
    /// <exception cref="System.ArgumentNullException"/>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public static T CreateFromDll<T>(string dllName, Guid mpaguid)
    {
        if (String.IsNullOrEmpty(dllName))
            throw new ArgumentNullException("dllName");

        //Get a classFactory for our classID
        IClassFactory classFactory = ComHelper.GetClassFactory(dllName, mpaguid);
        if (classFactory == null)
            throw new COMException(String.Format("Can't create ClassFactory from '{0}", dllName));

        //And create an object-instance using that class factory
        Guid iGUID = typeof(T).GUID;
        Object obj;

        try
        {
            Marshal.ThrowExceptionForHR(classFactory.CreateInstance(null, ref iGUID, out obj));
            return (T)obj;
        }
        finally { Marshal.ReleaseComObject(classFactory); }
    }

    #endregion Create COM-Object from file

    #region IClassFactory Interface

    [ComVisible(true), ComImport, SuppressUnmanagedCodeSecurity,
    Guid("00000001-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClassFactory
    {
        [PreserveSig]
        int CreateInstance([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
                                 [In] ref Guid riid,
                                 [Out, MarshalAs(UnmanagedType.Interface)] out object obj);

        [PreserveSig]
        int LockServer([In] bool fLock);
    }

    #endregion IClassFactory Interface

    #region ComHelper class Load Com-Objects from file

    /// <summary>
    /// Utility class to get a Class Factory for a certain Class ID
    /// by loading the dll that implements that class
    /// </summary>
    public static class ComHelper
    {
        private static DLLLoader loader = new DLLLoader();

        /// <summary>
        /// Gets a class factory for a specific COM Class ID.
        /// </summary>
        /// <param name="dllName">The dll where the COM class is implemented</param>
        /// <param name="filterPersistClass">The requested Class ID</param>
        /// <returns>IClassFactory instance used to create instances of that class</returns>
        /// <exception cref="System.Runtime.InteropServices.COMException">Thrown if the method can't creat COM-object</exception>
        /// <exception cref="System.Runtime.DllNotFoundException">Thrown if the dll not found</exception>
        public static IClassFactory GetClassFactory(string dllName, Guid filtersGuiid)
        {
            IntPtr dllHandle = loader.GetDLLHandle(dllName);
            Object unk;

            //Get a pointer to the DllGetClassObject function
            IntPtr dllGetClassObjectPtr = GetProcAddress(dllHandle, "DllGetClassObject");
            if (dllGetClassObjectPtr == IntPtr.Zero)
                return null;

            //Call the DllGetClassObject to retreive a class factory for out Filter class
            Guid IClassFactory_GUID = typeof(IClassFactory).GUID; //IClassFactory class id

            //Convert the function pointer to a .net delegate
            DllGetClassObject dllGetClassObject = (DllGetClassObject)Marshal.GetDelegateForFunctionPointer(dllGetClassObjectPtr, typeof(DllGetClassObject));

            return (dllGetClassObject(ref filtersGuiid, ref IClassFactory_GUID, out unk) != 0) ? null : (unk as IClassFactory);
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

        //DllGetClassObject fuction pointer signature
        private delegate int DllGetClassObject(ref Guid ClassId, ref Guid InterfaceId, [Out, MarshalAs(UnmanagedType.Interface)] out object ppunk);

        /// <summary>
        /// The GetProcAddress function retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="hModule">Handle to the DLL module that contains the function or variable.
        /// The LoadLibrary or GetModuleHandle function returns this handle.</param>
        /// <param name="lpProcName">Pointer to a null-terminated string containing the function or variable name,
        /// or the function's ordinal value. If this parameter is an ordinal value,
        /// it must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>If the function succeeds, the return value is the address of the exported function or variable.<br></br>
        /// <br>If the function fails, the return value is NULL. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>
        /// LoadLibrary function maps the specified executable module into the address space of the calling process.
        /// </summary>
        /// <param name="lpLibFileName">Pointer to a null-terminated string that names the executable module.
        /// The name specified is the file name of the module and is not related to the name stored in the library module itself,
        /// as specified by the LIBRARY keyword in the module-definition (.def) file.</param>
        /// <returns>If the function succeeds, the return value is a handle to the module.<br></br>
        /// <br>If the function fails, the return value is NULL. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryA", SetLastError = true, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr LoadLibrary(string lpLibFileName);

        /// <summary>
        /// The FreeLibrary function decrements the reference count of the loaded dynamic-link library (DLL).
        /// When the reference count reaches zero, the module is unmapped from the address space of the calling process and the handle is no longer valid.
        /// </summary>
        /// <param name="hLibModule">Handle to the loaded DLL module. The LoadLibrary or GetModuleHandle function returns this handle.</param>
        /// <returns>If the function succeeds, the return value is nonzero.<br></br><br>
        /// If the function fails, the return value is zero. To get extended error information, call Marshal.GetLastWin32Error.</br></returns>
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", SetLastError = true, CharSet = CharSet.Ansi), SuppressUnmanagedCodeSecurity]
        internal static extern int FreeLibrary(IntPtr hLibModule);

        #region private DLLLoader class

        private class DLLLoader
        {
            private Dictionary<string, IntPtr> loadedDlls = new Dictionary<string, IntPtr>();

            private delegate int PointerToMethodInvoker();

            ~DLLLoader()
            {
                lock (loadedDlls)
                {
                    foreach (var dllHandle in loadedDlls.Values)
                    {
                        try { FreeLibrary(dllHandle); }
                        catch { }
                    }
                }
            }

            /// <summary>
            /// GetDLLHandle
            /// </summary>
            /// <param name="dllName"></param>
            /// <returns>the handle from registered dll</returns>
            public IntPtr GetDLLHandle(string dllName)
            {
                IntPtr handle;

                lock (loadedDlls)
                {
                    if (!loadedDlls.TryGetValue(dllName, out handle))
                    {
                        handle = LoadLibrary(dllName);
                        if (handle == IntPtr.Zero)
                            throw new Win32Exception(string.Format("Can't load library '{0}'.", dllName));

                        // Keep a reference to the dll until the process\AppDomain dies.
                        loadedDlls.Add(dllName, handle);
                    }
                    return handle;
                }
            }

            public void RegisterComDLL(IntPtr dllHandle)
            {
                CallPointerMethod(dllHandle, "DllRegisterServer");
            }

            public void UnRegisterComDLL(IntPtr dllHandle)
            {
                CallPointerMethod(dllHandle, "DllUnregisterServer");
            }

            private void CallPointerMethod(IntPtr dllHandle, string methodName)
            {
                IntPtr dllEntryPoint = GetProcAddress(dllHandle, methodName);
                if (IntPtr.Zero == dllEntryPoint)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var method = (PointerToMethodInvoker)Marshal.GetDelegateForFunctionPointer(dllEntryPoint, typeof(PointerToMethodInvoker));
                method();
            }
        }

        #endregion private DLLLoader class
    }

    #endregion ComHelper class Load Com-Objects from file
}
