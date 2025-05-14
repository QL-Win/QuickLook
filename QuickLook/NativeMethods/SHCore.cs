using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods;

internal static class SHCore
{
    public enum PROCESS_DPI_AWARENESS
    {
        PROCESS_DPI_UNAWARE,
        PROCESS_SYSTEM_DPI_AWARE,
        PROCESS_PER_MONITOR_DPI_AWARE
    }

    [DllImport("shcore.dll")]
    public static extern uint SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
}
