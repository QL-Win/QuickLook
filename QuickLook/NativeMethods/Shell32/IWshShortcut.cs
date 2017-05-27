using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods.Shell32
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("F935DC23-1CF0-11D0-ADB9-00C04FD58A0B")]
    internal interface IWshShortcut
    {
        string FullName { get; }
        string Arguments { get; set; }
        string Description { get; set; }
        string Hotkey { get; set; }
        string IconLocation { get; set; }
        string RelativePath { set; }
        string TargetPath { get; set; }
        int WindowStyle { get; set; }
        string WorkingDirectory { get; set; }
        void Load([In] string pathLink);
        void Save();
    }
}