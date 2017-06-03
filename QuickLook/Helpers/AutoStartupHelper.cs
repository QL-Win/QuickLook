using System;
using System.IO;
using Shell32;

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
                File.Create(_startupFullPath).Close();

                var shl = new Shell();
                var dir = shl.NameSpace(Path.GetDirectoryName(_startupFullPath));
                var itm = dir.Items().Item(Path.GetFileName(_startupFullPath));
                var lnk = (ShellLinkObject) itm.GetLink;

                lnk.Path = App.AppFullPath;
                lnk.Arguments = "/autorun"; // silent
                lnk.SetIconLocation(App.AppFullPath, 0);
                lnk.WorkingDirectory = App.AppPath;

                lnk.Save(_startupFullPath);
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