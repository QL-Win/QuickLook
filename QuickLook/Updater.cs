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

namespace QuickLook
{
    class Updater
    {
        public static void AutoUpdate()
        {
            var lversion = "";
            var dpath = "";
            try
            {
                //check remote server for json file containing the latest version info
                WebRequest WebRequest = WebRequest.Create("https://www.dropbox.com/s/9snksdc815ewlr7/quicklookstats.txt?dl=1");
                var response = WebRequest.GetResponse();
                string jsonrsp = new StreamReader(response.GetResponseStream()).ReadToEnd();
                dynamic results = JsonConvert.DeserializeObject<dynamic>(jsonrsp);
                lversion = results.latestVersion;
                dpath = results.downloadPath;
            }
            catch (Exception e)
            {
                string message = "An error occured while trying to check for updates.";
                string caption = "QuickLook - Update error";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;
                result = MessageBox.Show(message, caption, buttons);
            }
            if (Application.ProductVersion != lversion)
            {
                string message = "A new version (v" + lversion + ") of QuickLook is available.\nDo you want to download and install it?";
                string caption = "QuickLook - New Update Available";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;
                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    bool success = false;
                    string tmpFolderPath = Directory.GetCurrentDirectory() + @"\quicklook_updates";
                    string newUpdateFileLocation = tmpFolderPath + @"\quicklook_" + lversion + "_" + Guid.NewGuid().ToString() + ".msi";
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
                        string Warn_message = "The update is now being downloaded and the installer will be executed right after.\nThe app won't work until this process finishes.\nPlease wait.";
                        string Warn_caption = "QuickLook - Notice";
                        MessageBoxButtons Warn_buttons = MessageBoxButtons.OK;
                        DialogResult Warn_result;
                        Warn_result = MessageBox.Show(Warn_message, Warn_caption, Warn_buttons);

                        //download the new update msi package from the URL specified on the json file
                        var fileReader = new WebClient();
                        var fileAddress = dpath;
                        fileReader.DownloadFile(new Uri(fileAddress), newUpdateFileLocation);
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
                            //kills quicklook process and executes the msi installer through a cmd command chain (cmd will require UAC elevation)
                            string pId = Process.GetCurrentProcess().Id.ToString();
                            string command = @"taskkill /f /PID " + pId + " && timeout 3 && \"" + newUpdateFileLocation + "\" && exit";
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
                            string Err_message = "An error occured while downloading the new update.";
                            string Err_caption = "QuickLook - Update error";
                            MessageBoxButtons Err_buttons = MessageBoxButtons.OK;
                            DialogResult Err_result;
                            Err_result = MessageBox.Show(Err_message, Err_caption, Err_buttons);
                        }
                    }
                }
            }
            else
            {
                string message = "No new version available.";
                string caption = "QuickLook";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;
                result = MessageBox.Show(message, caption, buttons);
            }
        }
    }
}
