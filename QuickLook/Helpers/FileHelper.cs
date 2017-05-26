using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers
{
    internal class FileHelper
    {
        private static string GetExecutable(string line)
        {
            var ret = line.StartsWith("\"")
                ? line.Split(new[] {"\" "}, 2, StringSplitOptions.None)
                : line.Split(new[] {' '}, 2, StringSplitOptions.None);

            ret[0] = ret[0].StartsWith("\"") ? ret[0].Substring(1) : ret[0];
            return ret[0];
        }

        public static bool? GetAssocApplication(string path, out string appFriendlyName)
        {
            appFriendlyName = string.Empty;

            if (string.IsNullOrEmpty(path))
                return null;

            if (Directory.Exists(path))
                return null;

            if (!File.Exists(path))
                return null;

            if (Path.GetExtension(path) == ".lnk")
            {
                var shell = (IWshShell) new WshShell();
                var link = shell.CreateShortcut(path);
                path = GetExecutable(link.TargetPath);
            }

            var ext = Path.GetExtension(path).ToLower();
            var isExe = new[] {".cmd", ".bat", ".pif", ".scf", ".exe", ".com", ".scr"}.Contains(ext.ToLower());

            // no assoc. app. found
            if (string.IsNullOrEmpty(GetAssocApplicationNative(ext, AssocStr.Command)))
                if (string.IsNullOrEmpty(GetAssocApplicationNative(ext, AssocStr.AppId))) // UWP
                    return null;

            appFriendlyName = isExe
                ? FileVersionInfo.GetVersionInfo(path).FileDescription
                : GetAssocApplicationNative(ext, AssocStr.FriendlyAppName);

            if (string.IsNullOrEmpty(appFriendlyName))
                appFriendlyName = Path.GetFileName(path);

            return isExe;
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra,
            [Out] StringBuilder sOut, [In] [Out] ref uint nOut);

        private static string GetAssocApplicationNative(string fileExtensionIncludingDot, AssocStr str)
        {
            uint cOut = 0;
            if (AssocQueryString(AssocF.Verify | AssocF.RemapRunDll | AssocF.InitIgnoreUnknown, str,
                    fileExtensionIncludingDot, null, null,
                    ref cOut) != 1)
                return null;

            var pOut = new StringBuilder((int) cOut);
            if (AssocQueryString(AssocF.Verify | AssocF.RemapRunDll | AssocF.InitIgnoreUnknown, str,
                    fileExtensionIncludingDot, null, pOut,
                    ref cOut) != 0)
                return null;

            return pOut.ToString();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Flags]
        private enum AssocF
        {
            InitNoRemapCLSID = 0x1,
            InitByExeName = 0x2,
            OpenByExeName = 0x2,
            InitDefaultToStar = 0x4,
            InitDefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            InitIgnoreUnknown = 0x400,
            InitFixedProgid = 0x800,
            IsProtocol = 0x1000,
            InitForFile = 0x2000
        }

        //[SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DdeCommand,
            DdeIfExec,
            DdeApplication,
            DdeTopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            SupportedUriProtocols,
            ProgId,
            AppId,
            AppPublisher,
            AppIconReference,
            Max
        }
    }
}