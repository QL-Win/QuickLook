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
                        (sender, e) => Process.Start(@"http://pooi.moe/QuickLook/")),
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