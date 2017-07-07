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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using QuickLook.Controls;

namespace QuickLook.Helpers
{
    internal class Updater
    {
        public static void CheckForUpdates(bool silent = false)
        {
            if (App.IsUWP)
                return;

            Task.Run(() =>
            {
                try
                {
                    var web = new WebClientEx(15 * 1000);
                    web.Headers.Add(HttpRequestHeader.UserAgent, "Wget/1.9.1");

                    var response = web.DownloadDataStream("https://api.github.com/repos/xupefei/QuickLook/releases");

                    var json = JsonConvert.DeserializeObject<dynamic>(new StreamReader(response).ReadToEnd());

                    var nVersion = (string) json[0]["tag_name"];
                    //nVersion = "0.2.1";

                    if (new Version(nVersion) <= Assembly.GetExecutingAssembly().GetName().Version)
                    {
                        if (!silent)
                            Application.Current.Dispatcher.Invoke(
                                () => TrayIconManager.GetInstance().ShowNotification("",
                                    "You are now on the latest version."));
                        return;
                    }

                    string notes = CollectReleaseNotes(json);

                    var changeLogPath = Path.GetTempFileName() + ".md";
                    File.WriteAllText(changeLogPath, notes);

                    Application.Current.Dispatcher.Invoke(
                        () =>
                        {
                            ViewWindowManager.GetInstance().InvokeViewer(changeLogPath);
                            TrayIconManager.GetInstance().ShowNotification("",
                                $"New version {nVersion} is released. Click here to open the download page.",
                                clickEvent: () => Process.Start(
                                    @"https://github.com/xupefei/QuickLook/releases/latest"));
                        });
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Application.Current.Dispatcher.Invoke(
                        () => TrayIconManager.GetInstance().ShowNotification("",
                            $"Error occured when checking for updates: {e.Message}"));
                }
            });
        }

        private static string CollectReleaseNotes(dynamic json)
        {
            var notes = string.Empty;

            foreach (var item in json)
            {
                notes += $"# {item["name"]}\r\n\r\n";
                notes += item["body"] + "\r\n\r\n";
            }

            return notes;
        }
    }
}