// Copyright © 2017-2025 QL-Win Contributors
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

using QuickLook.Common.Commands;
using QuickLook.Common.Helpers;
using QuickLook.Helpers;
using QuickLook.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Wpf.Ui.Violeta.Win32;
using ToolTipIcon = Wpf.Ui.Violeta.Win32.ToolTipIcon;

namespace QuickLook;

internal partial class TrayIconManager : IDisposable
{
    private static TrayIconManager _instance;

    private readonly TrayIconHost _icon;

    private readonly TrayMenuItem _itemAutorun = null!;

    private TrayIconManager()
    {
        _icon = new TrayIconHost
        {
            ToolTipText = string.Format(TranslationHelper.Get("Icon_ToolTip"),
                Application.ProductVersion),
            Icon = GetTrayIconByDPI(),
            Menu =
            [
                new TrayMenuItem()
                {
                    Header = $"v{Application.ProductVersion}{(App.IsUWP ? " (UWP)" : string.Empty)}",
                    IsEnabled = false,
                },
                new TraySeparator(),
                new TrayMenuItem()
                {
                   Header = TranslationHelper.Get("Icon_CheckUpdate"),
                   Command = new RelayCommand(() => Updater.CheckForUpdates()),
                },
                new TrayMenuItem()
                {
                    Header = TranslationHelper.Get("Icon_GetPlugin"),
                    Command = new RelayCommand(() => Process.Start("https://github.com/QL-Win/QuickLook/wiki/Available-Plugins")),
                },
                new TrayMenuItem()
                {
                    Header = TranslationHelper.Get("Icon_OpenDataFolder"),
                    Command = new RelayCommand(() => Process.Start("explorer.exe", SettingHelper.LocalDataPath)),
                },
                _itemAutorun = new TrayMenuItem()
                {
                    Header = TranslationHelper.Get("Icon_RunAtStartup"),
                    Command = new RelayCommand(() =>
                    {
                        if (AutoStartupHelper.IsAutorun())
                            AutoStartupHelper.RemoveAutorunShortcut();
                        else
                            AutoStartupHelper.CreateAutorunShortcut();
                    }),
                    IsEnabled = !App.IsUWP,
                },
                new TrayMenuItem()
                {
                    Header = TranslationHelper.Get("Icon_Restart"),
                    Command = new RelayCommand(() => Restart(forced: true)),
                },
                new TrayMenuItem()
                {
                    Header = TranslationHelper.Get("Icon_Quit"),
                    Command = new RelayCommand(System.Windows.Application.Current.Shutdown),
                }
            ],
            IsVisible = SettingHelper.Get("ShowTrayIcon", true)
        };

        _icon.RightDown += (_, _) =>
        {
            _itemAutorun.IsChecked = AutoStartupHelper.IsAutorun();
        };
    }

    public void Dispose()
    {
        _icon.IsVisible = false;
    }

    public void Restart(string fileName = null, string dir = null, string args = null, int? exitCode = null, bool forced = false)
    {
        _ = args; // Currently there is no cli supported by QL

        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = fileName ?? Path.Combine(dir ?? AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName),
                    WorkingDirectory = dir ?? Environment.CurrentDirectory,
                    UseShellExecute = true,
                },
            };
            process.Start();
        }
        catch (Win32Exception)
        {
            return;
        }
        if (forced)
        {
            Process.GetCurrentProcess().Kill();
        }
        Environment.Exit(exitCode ?? 'r' + 'e' + 's' + 't' + 'a' + 'r' + 't');
    }

    private nint GetTrayIconByDPI()
    {
        var scale = DisplayDeviceHelper.GetCurrentScaleFactor().Vertical;

        if (!App.IsWin10)
            return scale > 1 ? Resources.app.Handle : Resources.app_16.Handle;

        return OSThemeHelper.SystemUsesDarkTheme()
            ? (scale > 1 ? Resources.app_white.Handle : Resources.app_white_16.Handle)
            : (scale > 1 ? Resources.app_black.Handle : Resources.app_black_16.Handle);
    }

    public static void ShowNotification(string title, string content, bool isError = false, int timeout = 5000,
        Action clickEvent = null,
        Action closeEvent = null)
    {
        var icon = GetInstance()._icon;
        
        try
        {
            icon.ShowBalloonTip(timeout, title, content, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
            icon.BalloonTipClicked += OnIconOnBalloonTipClicked;
            icon.BalloonTipClosed += OnIconOnBalloonTipClosed;
        }
        catch (MissingMethodException)
        {
            // Fallback: ShowBalloonTip method signature may have changed in the library
            // Try alternative approach or silently fail to prevent crash
            System.Diagnostics.Debug.WriteLine($"ShowBalloonTip failed: {title} - {content}");
        }

        void OnIconOnBalloonTipClicked(object sender, EventArgs e)
        {
            clickEvent?.Invoke();
            icon.BalloonTipClicked -= OnIconOnBalloonTipClicked;
        }

        void OnIconOnBalloonTipClosed(object sender, EventArgs e)
        {
            closeEvent?.Invoke();
            icon.BalloonTipClosed -= OnIconOnBalloonTipClosed;
        }
    }

    public static TrayIconManager GetInstance()
    {
        return _instance ??= new TrayIconManager();
    }
}
