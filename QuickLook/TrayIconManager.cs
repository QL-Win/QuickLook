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
using System.Windows.Forms;
using QuickLook.Helpers;
using QuickLook.Properties;

namespace QuickLook
{
    public class TrayIconManager : IDisposable
    {
        private static TrayIconManager _instance;

        private readonly NotifyIcon _icon;

        private readonly MenuItem _itemAutorun =
            new MenuItem("Run at &Startup", (sender, e) =>
            {
                if (AutoStartupHelper.IsAutorun())
                    AutoStartupHelper.RemoveAutorunShortcut();
                else
                    AutoStartupHelper.CreateAutorunShortcut();
            });

        private TrayIconManager()
        {
            _icon = new NotifyIcon
            {
                Text = $"QuickLook v{Application.ProductVersion}",
                Icon = Resources.app,
                Visible = true,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem($"v{Application.ProductVersion}") {Enabled = false},
                    new MenuItem("-"),
                    new MenuItem("Check for &Updates...",
                        (sender, e) => Updater.CheckForUpdates()),
                    _itemAutorun,
                    new MenuItem("&Quit", (sender, e) => System.Windows.Application.Current.Shutdown())
                })
            };

            _icon.ContextMenu.Popup += (sender, e) => { _itemAutorun.Checked = AutoStartupHelper.IsAutorun(); };
        }

        public void Dispose()
        {
            _icon.Visible = false;
        }

        public void ShowNotification(string title, string content, bool isError = false, Action clickEvent = null,
            Action closeEvent = null)
        {
            _icon.ShowBalloonTip(5000, title, content, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
            _icon.BalloonTipClicked += OnIconOnBalloonTipClicked;
            _icon.BalloonTipClosed += OnIconOnBalloonTipClosed;

            void OnIconOnBalloonTipClicked(object sender, EventArgs e)
            {
                clickEvent?.Invoke();
                _icon.BalloonTipClicked -= OnIconOnBalloonTipClicked;
            }


            void OnIconOnBalloonTipClosed(object sender, EventArgs e)
            {
                closeEvent?.Invoke();
                _icon.BalloonTipClosed -= OnIconOnBalloonTipClosed;
            }
        }

        internal static TrayIconManager GetInstance()
        {
            return _instance ?? (_instance = new TrayIconManager());
        }
    }
}