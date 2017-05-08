using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuickLook
{
    public class TrayIcon
    {
        private static TrayIcon _instance;

        private NotifyIcon _icon;

        internal TrayIcon()
        {
            _icon = new NotifyIcon
            {
                Icon = Properties.Resources.app_white,
                Visible = true
            };
        }

        public void ShowNotification(string title, string content, bool isError = false)
        {
            _icon.ShowBalloonTip(5000, title, content, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
        }

        internal static TrayIcon GetInstance()
        {
            return _instance ?? (_instance = new TrayIcon());
        }
    }
}
