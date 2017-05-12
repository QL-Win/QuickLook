using System.Diagnostics;
using System.IO;

namespace QuickLook
{
    internal static class PidHelper
    {
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
            int.TryParse(File.ReadAllText(pid), out ppid);

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
            File.WriteAllText(App.RunningAsViewer ? PidViewer : PidListener, Process.GetCurrentProcess().Id.ToString());
        }

        internal static void DeletePid()
        {
            File.Delete(App.RunningAsViewer ? PidViewer : PidListener);
        }
    }
}