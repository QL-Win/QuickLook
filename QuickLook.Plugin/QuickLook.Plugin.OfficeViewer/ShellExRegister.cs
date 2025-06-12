using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Principal;

namespace QuickLook.Plugin.OfficeViewer;

internal static class ShellExRegister
{
    /// <summary>
    /// Returns the GUID of the preview handler associated with the specified file extension.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static Guid GetPreviewHandlerGUID(string fileExtension)
    {
        // open the registry key corresponding to the file extension
        var ext = Registry.ClassesRoot.OpenSubKey(fileExtension);
        if (ext != null)
        {
            // open the key that indicates the GUID of the preview handler type
            // Such as `Computer\HKEY_CLASSES_ROOT\.docx\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}`
            // Such as `Computer\HKEY_CLASSES_ROOT\.xlsx\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}`
            var test = ext.OpenSubKey(@"shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
            if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

            // sometimes preview handlers are declared on key for the class
            var className = Convert.ToString(ext.GetValue(null));
            if (className != null)
            {
                // Such as `Computer\HKEY_CLASSES_ROOT\{CLASS_NAME}\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}`
                test = Registry.ClassesRoot.OpenSubKey(
                    className + @"\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
            }
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Set the GUID of the preview handler associated with the specified file extension.
    /// </summary>
    /// <param name="fileExtension"></param>
    /// <param name="guid"></param>
    public static void SetPreviewHandlerGUID(string fileExtension, Guid guid)
    {
        // open the registry key corresponding to the file extension
        var ext = Registry.ClassesRoot.OpenSubKey(fileExtension);
        if (ext != null)
        {
            // open the key that indicates the GUID of the preview handler type
            // Such as `Computer\HKEY_CLASSES_ROOT\.docx\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}`
            // Such as `Computer\HKEY_CLASSES_ROOT\.xlsx\shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}`
            var test = ext.OpenSubKey(@"shellex\{8895b1c6-b41f-4c1c-a562-0d564250836f}", true);
            test?.SetValue(null, guid.ToString("B"));
        }
    }

    public static bool IsRunAsAdmin()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
