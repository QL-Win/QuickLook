using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace QuickLook.Plugin.CameraRawViewer
{
    internal class DCraw
    {
        private static readonly string DCrawPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Environment.Is64BitProcess ? "dcraw64.exe" : "dcraw32.exe");

        public static string ConvertToTiff(string input)
        {
            var output = Path.GetTempFileName();

            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = DCrawPath;
                p.StartInfo.Arguments = $"-w -W -h -T -O \"{output}\" \"{input}\"";
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.Start();

                p.WaitForExit(10000);
            }

            return new FileInfo(output).Length > 0 ? output : string.Empty;
        }
    }
}