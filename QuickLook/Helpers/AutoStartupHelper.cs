using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using QuickLook.NativeMethods.Shell32;

namespace QuickLook.Helpers
{
    internal static class AutoStartupHelper
    {
        private static readonly string _startupFullPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Path.ChangeExtension(Path.GetFileName(App.AppFullPath), ".lnk"));

        internal static void CreateAutorunShortcut()
        {
            try
            {
                var link = (IShellLink) new ShellLink();

                link.SetPath(App.AppFullPath);
                link.SetWorkingDirectory(App.AppPath);
                link.SetIconLocation(App.AppFullPath, 0);

                link.SetArguments($"/autorun"); // silent

                var file = (IPersistFile) link;
                file.Save(_startupFullPath, false);
            }
            catch (Exception)
            {
                TrayIconManager.GetInstance().ShowNotification("", "Failed to add QuickLook to Startup folder.");
            }
        }

        internal static void RemoveAutorunShortcut()
        {
            File.Delete(_startupFullPath);
        }

        internal static bool IsAutorun()
        {
            return File.Exists(_startupFullPath);
        }
    }
}