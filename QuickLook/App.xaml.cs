using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public static bool RunningAsViewer;

        private static bool _duplicated;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                MessageBox.Show(((Exception) args.ExceptionObject).Message + Environment.NewLine +
                                ((Exception) args.ExceptionObject).StackTrace);

                Current.Shutdown();
            };

            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Any())
                if (Directory.Exists(e.Args.First()) || File.Exists(e.Args.First()))
                    RunAsViewer(e);
                else
                    RunAsListener(e);
            else
                RunAsListener(e);
        }

        private void RunAsViewer(StartupEventArgs e)
        {
            RunningAsViewer = true;

            var runningPid = PidHelper.GetRunningInstance();
            if (runningPid != -1)
            {
                Process.GetProcessById(runningPid).Kill();

                Current.Shutdown();
                return;
            }

            PidHelper.WritePid();

            ViewWindowManager.GetInstance().InvokeViewer(e.Args.First());
        }

        private void RunAsListener(StartupEventArgs e)
        {
            RunningAsViewer = false;

            if (PidHelper.GetRunningInstance() != -1)
            {
                _duplicated = true;

                MessageBox.Show("QuickLook is already running in the background.");

                Current.Shutdown();
                return;
            }

            PidHelper.WritePid();

            TrayIconManager.GetInstance();
            if (!e.Args.Contains("/autorun"))
                TrayIconManager.GetInstance().ShowNotification("", "QuickLook is running in the background.");

            PluginManager.GetInstance();

            BackgroundListener.GetInstance();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            TrayIconManager.GetInstance().Dispose();
            BackgroundListener.GetInstance().Dispose();

            if (!_duplicated)
                PidHelper.DeletePid();
        }
    }
}