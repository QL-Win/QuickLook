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
using QuickLook.Helpers;
using QuickLook.Properties;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly bool IsUWP = ProcessHelper.IsRunningAsUWP();
        public static readonly bool Is64Bit = Environment.Is64BitProcess;
        public static readonly string AppFullPath = Assembly.GetExecutingAssembly().Location;
        public static readonly string AppPath = Path.GetDirectoryName(AppFullPath);

        private bool _isFirstInstance;
        private Mutex _isRunning;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                //MessageBox.Show(((Exception) args.ExceptionObject).ToString());

                const string source = "QuickLook Application Error";
                if (!EventLog.SourceExists(source))
                    EventLog.CreateEventSource(source, "Application");
                EventLog.WriteEntry(source, ((Exception) args.ExceptionObject).ToString(),
                    EventLogEntryType.Error);
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
                    MessageBox.Show(TranslationHelper.GetString("APP_SECOND"));

                Shutdown();
                return;
            }

            UpgradeSettings();
            CheckUpdate();
            RunListener(e);

            // first instance: run and preview this file
            if (e.Args.Any() && (Directory.Exists(e.Args.First()) || File.Exists(e.Args.First())))
                RemoteCallShowPreview(e);
        }

        private void CheckUpdate()
        {
            if (DateTime.Now - Settings.Default.LastUpdate < TimeSpan.FromDays(7))
                return;

            Task.Delay(120 * 1000).ContinueWith(_ => Updater.CheckForUpdates(true));
            Settings.Default.LastUpdate = DateTime.Now;
            Settings.Default.Save();
        }

        private void UpgradeSettings()
        {
            try
            {
                if (!Settings.Default.Upgraded)
                    return;

                Updater.CollectAndShowReleaseNotes();

                Settings.Default.Upgrade();
            }
            catch (ConfigurationErrorsException e)
            {
                if (e.Filename != null)
                    File.Delete(e.Filename);
                else if (((ConfigurationErrorsException) e.InnerException)?.Filename != null)
                    File.Delete(((ConfigurationErrorsException) e.InnerException).Filename);

                MessageBox.Show("Configuration file is currupted and has been reseted. Please restart QuickLook.",
                    "QuickLook", MessageBoxButton.OK, MessageBoxImage.Error);

                Process.GetCurrentProcess().Kill(); // just kill current process to avoid subsequence executions
                //return;
            }
            Settings.Default.Upgraded = false;
            Settings.Default.Save();
        }

        private void RemoteCallShowPreview(StartupEventArgs e)
        {
            PipeServerManager.SendMessage(PipeMessages.Toggle, e.Args.First());
        }

        private void RunListener(StartupEventArgs e)
        {
            TrayIconManager.GetInstance();
            if (!e.Args.Contains("/autorun") && !IsUWP)
                TrayIconManager.ShowNotification("", TranslationHelper.GetString("APP_START"));
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
            if (_isFirstInstance)
            {
                _isRunning.ReleaseMutex();

                PipeServerManager.GetInstance().Dispose();
                TrayIconManager.GetInstance().Dispose();
                BackgroundListener.GetInstance().Dispose();
                ViewWindowManager.GetInstance().Dispose();
            }
        }

        private void EnsureFirstInstance()
        {
            _isRunning = new Mutex(true, "QuickLook.App.Mutex", out _isFirstInstance);
        }
    }
}