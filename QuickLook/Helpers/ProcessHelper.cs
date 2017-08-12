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
using System.Threading.Tasks;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers
{
    internal class ProcessHelper
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
    }
}