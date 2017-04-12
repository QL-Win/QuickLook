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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            PluginManager.GetInstance();

            BackgroundListener.GetInstance();
        }
    }
}