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
using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.InfoPanel;

public static class FileHelper
{
    public static void GetDriveSpace(string path, out long totalSpace, out long totalFreeSpace)
    {
        totalSpace = totalFreeSpace = 0L;

        try
        {
            var root = new DriveInfo(Path.GetPathRoot(path));

            totalSpace = root.TotalSize;
            totalFreeSpace = root.AvailableFreeSpace;
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void CountFolder(string root, ref bool stop, out long totalDirs, out long totalFiles,
        out long totalSize)
    {
        totalDirs = totalFiles = totalSize = 0L;

        var stack = new Stack<DirectoryInfo>();
        stack.Push(new DirectoryInfo(root));

        //totalDirs++; // self

        do
        {
            if (stop)
                break;

            var pos = stack.Pop();

            try
            {
                // process files in current directory
                foreach (var file in pos.EnumerateFiles())
                {
                    totalFiles++;
                    totalSize += file.Length;
                }

                // then push all sub-directories
                foreach (var dir in pos.EnumerateDirectories())
                {
                    totalDirs++;
                    stack.Push(dir);
                }
            }
            catch (Exception)
            {
                totalDirs++;
                //pos = stack.Pop();
            }
        } while (stack.Count != 0);
    }
}
