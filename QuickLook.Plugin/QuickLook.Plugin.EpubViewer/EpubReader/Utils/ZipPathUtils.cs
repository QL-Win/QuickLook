// Copyright © 2018 Marco Gavelli and Paddy Xu
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

namespace VersOne.Epub.Internal
{
    internal static class ZipPathUtils
    {
        public static string GetDirectoryPath(string filePath)
        {
            var lastSlashIndex = filePath.LastIndexOf('/');
            if (lastSlashIndex == -1)
                return string.Empty;
            return filePath.Substring(0, lastSlashIndex);
        }

        public static string Combine(string directory, string fileName)
        {
            if (string.IsNullOrEmpty(directory))
                return fileName;
            return string.Concat(directory, "/", fileName);
        }
    }
}