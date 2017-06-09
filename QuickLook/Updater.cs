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
      
        public static bool CheckForUpdates()
        {
            var lversion = "";
            var dpath = "";
            bool success = false;
            try
            {
                //check github api for json file containing the latest version info
                HttpWebRequest QLWebRequest = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/xupefei/QuickLook/releases/latest");
                QLWebRequest.UserAgent = "QuickLook Auto Updater";
                var response = QLWebRequest.GetResponse();
                string jsonrsp = new StreamReader(response.GetResponseStream()).ReadToEnd();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(jsonrsp);
                lversion = results["name"];
                dpath = results["assets"][0]["browser_download_url"];
                success = true;
            }
            catch (Exception e)
            {
                TrayIconManager.GetInstance().ShowNotification("QuickLook - Update error", "An error occured while trying to check for updates.", true);
                success = false;
            }

            lversion = lversion + ".0"; //update-version.cmd adds an aditional 0 to the version, github api doesnt.  
            int cleanNewVersion = Convert.ToInt32(lversion.Replace(".", ""));
            int cleanCurrentVersion = Convert.ToInt32(Application.ProductVersion.Replace(".", ""));

            if ((cleanCurrentVersion < cleanNewVersion) && success)
            {
                TrayIconManager.GetInstance().ShowNotification("QuickLook Update", "A new version of QuickLook is being downloaded.", false);
                TriggerUpdate(dpath); //this function will be called when the user accepts the update
                return true;
            }
            else
            {
                return false;
            }

        }

        public static void TriggerUpdate(string path)
        {
            BackgroundWorker QuickLookUpdateDownloader = new BackgroundWorker();
            QuickLookUpdateDownloader.DoWork += QuickLookUpdateDownloader_DoWork;
            QuickLookUpdateDownloader.RunWorkerCompleted += QuickLookUpdateDownloader_RunWorkerCompleted;
            QuickLookUpdateDownloader.RunWorkerAsync(path);

        }

        private static void QuickLookUpdateDownloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var r = e.Result;
            if (r is string)
            {
                //executes the msi installer through a cmd command chain (cmd will require UAC elevation)
                string command = @"""" + r + "\" && exit";
                var commandDispatcherSettings = new ProcessStartInfo();
                var commandDispatcherProcess = new Process();
                commandDispatcherSettings.FileName = "cmd";
                commandDispatcherSettings.Verb = "runas";
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
                if (!Directory.Exists(tmpFolderPath))
                {
                    Directory.CreateDirectory(tmpFolderPath);
                }
                else
                {
                    //wipe the temporary download folder
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
               
                //download the new update msi package from the URL specified on the json file
                var fileReader = new WebClient();
                fileReader.DownloadFile(new Uri(dpath), newUpdateFileLocation);
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
