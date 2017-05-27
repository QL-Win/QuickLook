using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods.Shell32
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [Guid("F935DC21-1CF0-11D0-ADB9-00C04FD58A0B")]
    internal interface IWshShell
    {
        IWshShortcut CreateShortcut(string pathLink);
    }
}