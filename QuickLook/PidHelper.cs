using System.Diagnostics;
using System.IO;

namespace QuickLook
{
    internal static class PidHelper
    {
        private static FileStream _pidLocker;

        private static readonly string PidListener =
            Path.Combine(Path.GetTempPath(), "QuickLook.App.Listener.D6EC3F8DDF6B.pid");

        private static readonly string PidViewer =
            Path.Combine(Path.GetTempPath(), "QuickLook.App.Viewer.A6FA53E93515.pid");

        internal static int GetRunningInstance()
        {
            var pid = App.RunningAsViewer ? PidViewer : PidListener;

            if (!File.Exists(pid))
                return -1;

            var ppid = -1;
            using (var file = File.Open(pid, FileMode.Open, FileAccess.Read,FileShare.ReadWrite))
            using (var sr = new StreamReader(file))
                int.TryParse(sr.ReadToEnd(), out ppid);

            try
            {
                Process.GetProcessById(ppid);
            }
            catch
            {
                return -1;
            }

            return ppid;
        }

        internal static void WritePid()
        {
            var pidFile = App.RunningAsViewer ? PidViewer : PidListener;

            File.WriteAllText(pidFile, Process.GetCurrentProcess().Id.ToString());

            _pidLocker = File.Open(pidFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        internal static void DeletePid()
        {
            _pidLocker?.Close();
            _pidLocker = null;

            File.Delete(App.RunningAsViewer ? PidViewer : PidListener);
        }
    }
}