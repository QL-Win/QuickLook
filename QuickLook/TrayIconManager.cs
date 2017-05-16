using System;
using System.Diagnostics;
using System.Windows.Forms;
using QuickLook.Helpers;
using QuickLook.Properties;
using Application = System.Windows.Application;

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
                Icon = Resources.app,
                Visible = true,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Check for &Updates...",
                        (sender, e) => Process.Start(@"http://pooi.moe/QuickLook/")),
                    _itemAutorun,
                    new MenuItem("&Quit", (sender, e) => Application.Current.Shutdown())
                })
            };

            _icon.ContextMenu.Popup += (sender, e) => { _itemAutorun.Checked = AutoStartupHelper.IsAutorun(); };
        }

        public void Dispose()
        {
            _icon.Visible = false;
        }

        public void ShowNotification(string title, string content, bool isError = false)
        {
            _icon.ShowBalloonTip(5000, title, content, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
        }

        internal static TrayIconManager GetInstance()
        {
            return _instance ?? (_instance = new TrayIconManager());
        }
    }
}