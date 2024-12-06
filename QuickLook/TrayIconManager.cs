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
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QuickLook.Common.Helpers;
using QuickLook.Helpers;
using QuickLook.Properties;

namespace QuickLook
{
    internal class TrayIconManager : IDisposable
    {
        [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetPreferredAppMode(int preferredAppMode);

        [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern void FlushMenuThemes();

        private static TrayIconManager _instance;

        private readonly NotifyIcon _icon;

        private readonly MenuItem _itemAutorun =
            new MenuItem(TranslationHelper.Get("Icon_RunAtStartup"),
                (sender, e) =>
                {
                    if (AutoStartupHelper.IsAutorun())
                        AutoStartupHelper.RemoveAutorunShortcut();
                    else
                        AutoStartupHelper.CreateAutorunShortcut();
                })
            { Enabled = !App.IsUWP };

        private TrayIconManager()
        {
            // Enable dark mode for context menus if using dark theme
            if (OSThemeHelper.AppsUseDarkTheme())
            {
                SetPreferredAppMode(2); // ForceDark
                FlushMenuThemes();
            }

            _icon = new NotifyIcon
            {
                Text = string.Format(TranslationHelper.Get("Icon_ToolTip"),
                    Application.ProductVersion),
                Icon = GetTrayIconByDPI(),
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem($"v{Application.ProductVersion}{(App.IsUWP ? " (UWP)" : "")}") {Enabled = false},
                    new MenuItem("-"),
                    new MenuItem(TranslationHelper.Get("Icon_CheckUpdate"), (sender, e) => Updater.CheckForUpdates()),
                    new MenuItem(TranslationHelper.Get("Icon_GetPlugin"),
                        (sender, e) => Process.Start("https://github.com/QL-Win/QuickLook/wiki/Available-Plugins")),
                    new MenuItem(TranslationHelper.Get("Icon_OpenDataFolder"), (sender, e) => Process.Start("explorer.exe", SettingHelper.LocalDataPath)),
                    _itemAutorun,
                    new MenuItem(TranslationHelper.Get("Icon_Quit"),
                        (sender, e) => System.Windows.Application.Current.Shutdown())
                }),
                Visible = SettingHelper.Get("ShowTrayIcon", true)
            };

            _icon.ContextMenu.Popup += (sender, e) => { _itemAutorun.Checked = AutoStartupHelper.IsAutorun(); };
        }

        public void Dispose()
        {
            _icon.Visible = false;
        }

        private Icon GetTrayIconByDPI()
        {
            var scale = DisplayDeviceHelper.GetCurrentScaleFactor().Vertical;

            if (!App.IsWin10)
                return scale > 1 ? Resources.app : Resources.app_16;

            return OSThemeHelper.SystemUsesDarkTheme()
                ? (scale > 1 ? Resources.app_white : Resources.app_white_16)
                : (scale > 1 ? Resources.app_black : Resources.app_black_16);
        }

        public static void ShowNotification(string title, string content, bool isError = false, int timeout = 5000,
            Action clickEvent = null,
            Action closeEvent = null)
        {
            var icon = GetInstance()._icon;
            icon.ShowBalloonTip(timeout, title, content, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
            icon.BalloonTipClicked += OnIconOnBalloonTipClicked;
            icon.BalloonTipClosed += OnIconOnBalloonTipClosed;

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
            return _instance ?? (_instance = new TrayIconManager());
        }
    }
}