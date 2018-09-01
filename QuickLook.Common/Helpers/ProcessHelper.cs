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
using System.Threading.Tasks;
using QuickLook.Common.NativeMethods;

namespace QuickLook.Common.Helpers
{
    public class ProcessHelper
    {
        private const int ErrorInsufficientBuffer = 0x7A;

        // ReSharper disable once InconsistentNaming
        public static void PerformAggressiveGC()
        {
            // delay some time to make sure that all windows are closed
            Task.Delay(2000).ContinueWith(t => GC.Collect(GC.MaxGeneration));
        }

        public static bool IsRunningAsUWP()
        {
            try
            {
                uint len = 0;
                var r = Kernel32.GetCurrentPackageFullName(ref len, null);

                return r == ErrorInsufficientBuffer;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static bool IsOnWindows10S()
        {
            const uint PRODUCT_CLOUD = 0x000000B2; // Windows 10 S
            const uint PRODUCT_CLOUDN = 0x000000B3; // Windows 10 S N

            Kernel32.GetProductInfo(Environment.OSVersion.Version.Major,
                Environment.OSVersion.Version.Minor, 0, 0, out var osType);

            return osType == PRODUCT_CLOUD || osType == PRODUCT_CLOUDN;
        }

        public static void WriteLog(string msg)
        {
            Debug.WriteLine(msg);

            var logFilePath = Path.Combine(SettingHelper.LocalDataPath, @"QuickLook.Exception.log");

            using (var writer = new StreamWriter(new FileStream(logFilePath, FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.Read)))
            {
                writer.BaseStream.Seek(0, SeekOrigin.End);

                writer.WriteLine($"========{DateTime.Now}========");
                writer.WriteLine(msg);
                writer.WriteLine();
            }
        }
    }
}