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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QuickLook.Common.Helpers;
using QuickLook.Helpers;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string AppFullPath = Assembly.GetExecutingAssembly().Location;
        public static readonly string AppPath = Path.GetDirectoryName(AppFullPath);
        public static readonly bool Is64Bit = Environment.Is64BitProcess;
        public static readonly bool IsUWP = ProcessHelper.IsRunningAsUWP();
        public static readonly bool IsWin10 = Environment.OSVersion.Version >= new Version(10, 0);

        private bool _isFirstInstance;
        private Mutex _isRunning;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                ProcessHelper.WriteLog(((Exception) args.ExceptionObject).ToString());
            };

            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (ProcessHelper.IsOnWindows10S())
            {
                MessageBox.Show("This application does not run on Windows 10 S.");

                Shutdown();
                return;
            }

            EnsureFirstInstance();

            if (!_isFirstInstance)
            {
                // second instance: preview this file
                if (e.Args.Any() && (Directory.Exists(e.Args.First()) || File.Exists(e.Args.First())))
                    RemoteCallShowPreview(e);
                // second instance: duplicate
                else
                    MessageBox.Show(TranslationHelper.Get("APP_SECOND"));

                Shutdown();
                return;
            }
            
            CheckUpdate();
            RunListener(e);

            // first instance: run and preview this file
            if (e.Args.Any() && (Directory.Exists(e.Args.First()) || File.Exists(e.Args.First())))
                RemoteCallShowPreview(e);
        }

        private void CheckUpdate()
        {
            if (DateTime.Now - SettingHelper.Get<DateTime>("LastUpdate") < TimeSpan.FromDays(7))
                return;

            Task.Delay(120 * 1000).ContinueWith(_ => Updater.CheckForUpdates(true));
            SettingHelper.Set("LastUpdate", DateTime.Now);
        }

        private void RemoteCallShowPreview(StartupEventArgs e)
        {
            PipeServerManager.SendMessage(PipeMessages.Invoke, e.Args.First());
        }

        private void RunListener(StartupEventArgs e)
        {
            TrayIconManager.GetInstance();
            if (!e.Args.Contains("/autorun") && !IsUWP)
                TrayIconManager.ShowNotification("", TranslationHelper.Get("APP_START"));
            if (e.Args.Contains("/first"))
                AutoStartupHelper.CreateAutorunShortcut();

            NativeMethods.QuickLook.Init();

            PluginManager.GetInstance();
            ViewWindowManager.GetInstance();
            BackgroundListener.GetInstance();
            PipeServerManager.GetInstance();
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            if (!_isFirstInstance)
                return;

            _isRunning.ReleaseMutex();

            PipeServerManager.GetInstance().Dispose();
            TrayIconManager.GetInstance().Dispose();
            BackgroundListener.GetInstance().Dispose();
            ViewWindowManager.GetInstance().Dispose();
        }

        private void EnsureFirstInstance()
        {
            _isRunning = new Mutex(true, "QuickLook.App.Mutex", out _isFirstInstance);
        }
    }
}