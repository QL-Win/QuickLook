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
using QuickLook.Common.Helpers;
using QuickLook.Controls;

namespace QuickLook.Helpers;

internal class Updater
{
    public static void CheckForUpdates(bool silent = false)
    {
        if (App.IsUWP)
        {
            if (!silent)
                Process.Start("ms-windows-store://pdp/?productid=9NV4BS3L1H4S");

            return;
        }

        Task.Run(() =>
        {
            try
            {
                var json = DownloadJson("https://api.github.com/repos/QL-Win/QuickLook/releases/latest");

                var nVersion = (string)json["tag_name"];
                //nVersion = "9.2.1";

                if (new Version(nVersion) <= Assembly.GetExecutingAssembly().GetName().Version)
                {
                    if (!silent)
                        Application.Current.Dispatcher.Invoke(
                            () => TrayIconManager.ShowNotification("",
                                TranslationHelper.Get("Update_NoUpdate")));
                    return;
                }

                CollectAndShowReleaseNotes();

                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        TrayIconManager.ShowNotification("",
                            string.Format(TranslationHelper.Get("Update_Found"), nVersion),
                            timeout: 20000,
                            clickEvent:
                            () => Process.Start("https://github.com/QL-Win/QuickLook/releases/latest"));
                    });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Application.Current.Dispatcher.Invoke(
                    () => TrayIconManager.ShowNotification("",
                        string.Format(TranslationHelper.Get("Update_Error"), e.Message)));
            }
        });
    }

    private static void CollectAndShowReleaseNotes()
    {
        Task.Run(() =>
        {
            try
            {
                var json = DownloadJson("https://api.github.com/repos/QL-Win/QuickLook/releases");

                var notes = "# QuickLook has been updated!\r\n";

                var count = 0;
                foreach (var item in json)
                {
                    notes += $"## {item["name"]}\r\n\r\n";
                    notes += item["body"] + "\r\n\r\n";

                    if (count++ > 10)
                        break;
                }

                var changeLogPath = Path.GetTempFileName() + ".md";
                File.WriteAllText(changeLogPath, notes);

                PipeServerManager.SendMessage(PipeMessages.Invoke, changeLogPath);
                PipeServerManager.SendMessage(PipeMessages.Forget);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Application.Current.Dispatcher.Invoke(
                    () => TrayIconManager.ShowNotification("",
                        string.Format(TranslationHelper.Get("Update_Error"), e.Message)));
            }
        });
    }

    private static dynamic DownloadJson(string url)
    {
        var web = new WebClientEx(15 * 1000)
        {
            Proxy = WebRequest.DefaultWebProxy,
            Credentials = CredentialCache.DefaultCredentials
        };
        web.Headers.Add(HttpRequestHeader.UserAgent, "Wget/1.9.1");

        var response =
            web.DownloadDataStream(url);

        var json = JsonConvert.DeserializeObject<dynamic>(new StreamReader(response).ReadToEnd());
        return json;
    }
}
