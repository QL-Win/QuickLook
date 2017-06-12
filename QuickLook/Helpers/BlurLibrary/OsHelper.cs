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

namespace QuickLook.Helpers.BlurLibrary
{
    internal static class OsHelper
    {
        public static OsType GetOsType()
        {
            if (Environment.OSVersion.Version.Major != 6 && Environment.OSVersion.Version.Major != 10)
                return OsType.Other;

            if (Environment.OSVersion.Version.Major != 6)
                return Environment.OSVersion.Version.Major == 10
                    ? OsType.Windows10
                    : OsType.Other;

            switch (Environment.OSVersion.Version.Minor)
            {
                case 0:
                    return OsType.WindowsVista;
                case 1:
                    return OsType.Windows7;
                case 2:
                    return OsType.Windows8;
                case 3:
                    return OsType.Windows81;
                default:
                    return OsType.Other;
            }
        }
    }
}