using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string AppFullPath = Assembly.GetExecutingAssembly().Location;
        public static readonly string AppPath = Path.GetDirectoryName(AppFullPath);

        private Mutex isRunning;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => MessageBox.Show(((Exception) args.ExceptionObject).Message + Environment.NewLine +
                                                  ((Exception) args.ExceptionObject).StackTrace);

            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            EnsureSingleInstance();

            if (!e.Args.Contains("/autorun"))
                TrayIcon.GetInstance().ShowNotification("", "QuickLook is running in the background.");

            PluginManager.GetInstance();

            BackgroundListener.GetInstance();
        }

        private void EnsureSingleInstance()
        {
            bool isNew = false;
            isRunning = new Mutex(true, "QuickLook.App", out isNew);
            if (!isNew)
            {
                MessageBox.Show("QuickLook is already running in the background.");
                Current.Shutdown();
            }
        }
    }
}