// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickLook.Common.Helpers
{
    public class FileHelper
    {
        public static bool IsExecutable(string path, out string appFriendlyName)
        {
            appFriendlyName = string.Empty;
            var ext = Path.GetExtension(path).ToLower();
            var isExe = new[] {".cmd", ".bat", ".pif", ".scf", ".exe", ".com", ".scr"}.Contains(ext.ToLower());

            if (!isExe)
                return false;

            appFriendlyName = FileVersionInfo.GetVersionInfo(path).FileDescription;
            if (string.IsNullOrEmpty(appFriendlyName))
                appFriendlyName = Path.GetFileName(path);

            return true;
        }

        public static bool GetAssocApplication(string path, out string appFriendlyName)
        {
            appFriendlyName = string.Empty;
            var ext = Path.GetExtension(path).ToLower();

            // no assoc. app. found
            if (string.IsNullOrEmpty(GetAssocApplicationNative(ext, AssocStr.Command)))
                if (string.IsNullOrEmpty(GetAssocApplicationNative(ext, AssocStr.AppId))) // UWP
                    return false;

            appFriendlyName = GetAssocApplicationNative(ext, AssocStr.FriendlyAppName);
            if (string.IsNullOrEmpty(appFriendlyName))
                appFriendlyName = Path.GetFileName(path);

            return true;
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