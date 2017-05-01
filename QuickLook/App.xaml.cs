using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string AppPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => MessageBox.Show(((Exception) args.ExceptionObject).Message + Environment.NewLine +
                                                  ((Exception) args.ExceptionObject).StackTrace);

            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            PluginManager.GetInstance();

            BackgroundListener.GetInstance();
        }
    }
}