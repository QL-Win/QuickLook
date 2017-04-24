using System;
using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.LastResort
{
    public static class FileHelper
    {
        public static void CountFolder(string root, ref bool stop, out long totalDirs, out long totalFiles,
            out long totalSize)
        {
            totalDirs = totalFiles = totalSize = 0L;

            var stack = new Stack<DirectoryInfo>();
            stack.Push(new DirectoryInfo(root));

            totalDirs++; // self

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

        public static string ToPrettySize(this long value, int decimalPlaces = 0)
        {
            const long OneKb = 1024;
            const long OneMb = OneKb * 1024;
            const long OneGb = OneMb * 1024;
            const long OneTb = OneGb * 1024;

            var asTb = Math.Round((double) value / OneTb, decimalPlaces);
            var asGb = Math.Round((double) value / OneGb, decimalPlaces);
            var asMb = Math.Round((double) value / OneMb, decimalPlaces);
            var asKb = Math.Round((double) value / OneKb, decimalPlaces);
            var chosenValue = asTb > 1
                ? $"{asTb} TB"
                : asGb > 1
                    ? $"{asGb} GB"
                    : asMb > 1
                        ? $"{asMb} MB"
                        : asKb > 1
                            ? $"{asKb} KB"
                            : $"{Math.Round((double) value, decimalPlaces)} bytes";

            return chosenValue;
        }
    }
}