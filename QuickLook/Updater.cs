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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace QuickLook
{
    class Updater
    {
        private static BackgroundWorker QuickLookUpdateDownloader = new BackgroundWorker();

        public static bool CheckForUpdates(bool silent = false)
        {
            if (QuickLookUpdateDownloader.IsBusy)
            {
                TrayIconManager.GetInstance().ShowNotification("", "A new version is already being downloaded.", false);
                return false;
            }
            var lversion = "";
            var dpath = "";
            var mdchangelog = "";
            bool success = false;
            Version vNew = new Version();
            Version vCurrent = new Version();
            string changeLogPath = Directory.GetCurrentDirectory() + @"\quicklook_updates\changelog.md";
            try
            {
                HttpWebRequest QLWebRequest = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/xupefei/QuickLook/releases/latest");
                QLWebRequest.UserAgent = "QuickLook Auto Updater";
                var response = QLWebRequest.GetResponse();
                string jsonrsp = new StreamReader(response.GetResponseStream()).ReadToEnd();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(jsonrsp);
                lversion = results["name"];
                dpath = results["assets"][0]["browser_download_url"];
                mdchangelog = results["body"];
                vNew = new Version(lversion);
                vCurrent = new Version(Application.ProductVersion);

                string tmpFolderPath = Directory.GetCurrentDirectory() + @"\quicklook_updates";
                if (!Directory.Exists(tmpFolderPath))
                {
                    Directory.CreateDirectory(tmpFolderPath);
                }
                else
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(tmpFolderPath);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                if (File.Exists(changeLogPath))
                {
                    File.Delete(changeLogPath);
                }

                using (FileStream fs = File.Create(changeLogPath))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(mdchangelog);
                    fs.Write(info, 0, info.Length);
                }

                success = true;
            }
            catch (Exception e)
            {
                success = false;
            }

            if ((vNew.CompareTo(vCurrent) > 0) && success)
            {
                Action acpt = new Action(() => UpdateConfirmation(changeLogPath, dpath));
                Action dcln = new Action(() => CancelUpdate());

                TrayIconManager.GetInstance().ShowNotification("QuickLook", "A new version of QuickLook is available. Click here to learn more.", false, acpt, dcln);
                return true;
            }
            else if (!success)
            {
                if (!silent)
                {
                    TrayIconManager.GetInstance().ShowNotification("QuickLook - Update error", "An error occured while trying to check for updates.", true);
                }
                return false;
            }
            else
            {
                if (!silent)
                {
                    TrayIconManager.GetInstance().ShowNotification("", "You have the latest version installed.", false);
                }
                return false;
            }

        }

        public static void UpdateConfirmation(string changelogPath, string dpath)
        {
            ViewWindowManager.GetInstance().InvokeViewer(changelogPath, false);
            string message = "Do you want to download and install this new update?";
            string caption = "QuickLook - New Update Available";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;
            result = MessageBox.Show(message, caption, buttons);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                TriggerUpdate(dpath);
                ViewWindowManager.GetInstance().ClosePreview();
                TrayIconManager.GetInstance().ShowNotification("QuickLook", "QuickLook is downloading a new update.", false);
            }
        }

        public static void CancelUpdate()
        {
            //code to skip or postpone the update
        }

        public static void TriggerUpdate(string path)
        {
            QuickLookUpdateDownloader.DoWork += QuickLookUpdateDownloader_DoWork;
            QuickLookUpdateDownloader.RunWorkerCompleted += QuickLookUpdateDownloader_RunWorkerCompleted;
            QuickLookUpdateDownloader.RunWorkerAsync(path);

        }

        private static void QuickLookUpdateDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var r = e.Result;
            if (r is string)
            {
                string command = @"""" + r + "\" && exit";
                var commandDispatcherSettings = new ProcessStartInfo();
                var commandDispatcherProcess = new Process();
                commandDispatcherSettings.FileName = "cmd";
                commandDispatcherSettings.WindowStyle = ProcessWindowStyle.Hidden;
                commandDispatcherSettings.Arguments = "cmd /C " + command;
                commandDispatcherProcess.StartInfo = commandDispatcherSettings;
                commandDispatcherProcess.Start();
                commandDispatcherProcess.WaitForExit();
            }
            else
            {
                TrayIconManager.GetInstance().ShowNotification("QuickLook - Update error", "An error occured while downloading the new version.", true);
            }
            
        }

        private static void QuickLookUpdateDownloader_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var dpath = e.Argument.ToString();
            string tmpFolderPath = Directory.GetCurrentDirectory() + @"\quicklook_updates";
            string newUpdateFileLocation = tmpFolderPath + @"\quicklook_update_" + Guid.NewGuid().ToString() + ".msi";
            bool success = false;
            try
            {              
                WebClientEx client = new WebClientEx(300000);
                var downloadedStream = client.DownloadDataStream(dpath);
                var fileStream = File.Create(newUpdateFileLocation);
                downloadedStream.WriteTo(fileStream);
                fileStream.Close();
                client.Dispose();
                fileStream.Dispose();
                downloadedStream.Dispose();
                success = true;
            }
            catch (Exception ex)
            {
                success = false;

            }
            finally
            {
                if (success)
                {
                    e.Result = newUpdateFileLocation;
                }
                else
                {
                    e.Result = false;
                }
            }
        }
    }
}
