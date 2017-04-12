using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using QuickLook.Plugin;
using QuickLook.Utilities;

namespace QuickLook
{
    /// <summary>
    /// Interaction logic for App.xaml
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
