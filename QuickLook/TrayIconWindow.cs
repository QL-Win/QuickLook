using System;
using System.Windows;

namespace QuickLook;

/// <summary>
/// A hidden window used internally for system tray functionality.
/// HotCorners is a background application with no visible user interface.
/// This window is created temporarily to initialize the tray icon context and is then hidden.
/// </summary>
public partial class TrayIconWindow : Window, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the TrayIconWindow class.
    /// Creates an invisible, transparent window with no taskbar presence.
    /// </summary>
    public TrayIconWindow()
    {
        Title = "TrayIconWindow";
        Width = 0;
        Height = 0;
        AllowsTransparency = true;
        Opacity = 0;
        ShowInTaskbar = false;
        WindowStyle = WindowStyle.None;
    }

    /// <summary>
    /// Disposes the window and releases its resources.
    /// </summary>
    public void Dispose()
    {
        Close();
    }
}
