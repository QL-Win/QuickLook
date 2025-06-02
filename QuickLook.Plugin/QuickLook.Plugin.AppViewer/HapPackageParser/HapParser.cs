// Copyright © 2017-2025 QL-Win Contributors
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

using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.HapPackageParser;

internal static class HapParser
{
    public static HapInfo Parse(string path)
    {
        using var zip = new ZipFile(path);
        ZipEntry entry = zip.GetEntry("module.json");

        if (entry != null)
        {
            using var stream = zip.GetInputStream(entry);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            if (dict == null) return null;

            var info = new HapInfo();

            if (dict.ContainsKey("app"))
            {
                dynamic app = dict["app"];

                if (app.ContainsKey("label")) info.Label = app["label"].ToString();
                if (app.ContainsKey("icon")) info.Icon = app["icon"].ToString();
                if (app.ContainsKey("versionName")) info.VersionName = app["versionName"].ToString();
                if (app.ContainsKey("versionCode")) info.VersionCode = app["versionCode"].ToString();
                if (app.ContainsKey("compileSdkType")) info.CompileSdkType = app["compileSdkType"].ToString();
                if (app.ContainsKey("compileSdkVersion")) info.CompileSdkVersion = app["compileSdkVersion"].ToString();
                if (app.ContainsKey("minAPIVersion")) info.MinAPIVersion = app["minAPIVersion"].ToString();
                if (app.ContainsKey("targetAPIVersion")) info.TargetAPIVersion = app["targetAPIVersion"].ToString();
                if (app.ContainsKey("bundleName")) info.BundleName = app["bundleName"].ToString();
                if (app.ContainsKey("debug")) info.Debug = (bool)app["debug"];
            }

            {
                if (info.Icon == "$media:layered_image")
                {
                    ZipEntry foreground = zip.GetEntry("resources/base/media/foreground.png");
                    ZipEntry background = zip.GetEntry("resources/base/media/background.png");

                    if (foreground != null && background != null)
                    {
                        {
                            using var s = new BinaryReader(zip.GetInputStream(foreground));
                            info.AppIconForeground = s.ReadBytes((int)foreground.Size);
                        }
                        {
                            using var s = new BinaryReader(zip.GetInputStream(background));
                            info.AppIconBackground = s.ReadBytes((int)background.Size);
                        }

                        info.HasLayeredIcon = true;
                        info.HasIcon = true;
                    }
                    else
                    {
                        info.HasLayeredIcon = false;
                        info.HasIcon = false;
                    }
                }

                {
                    ZipEntry appIcon = zip.GetEntry("resources/base/media/app_icon.png");

                    if (appIcon != null)
                    {
                        using var s = new BinaryReader(zip.GetInputStream(appIcon));
                        info.Logo = s.ReadBytes((int)appIcon.Size);

                        info.HasLayeredIcon = false;
                        info.HasIcon = true;
                    }
                }

                if (!info.HasIcon)
                {
                    ZipEntry icon = zip.GetEntry("resources/base/media/icon.png");

                    if (icon != null)
                    {
                        using var s = new BinaryReader(zip.GetInputStream(icon));
                        info.Logo = s.ReadBytes((int)icon.Size);

                        info.HasLayeredIcon = false;
                        info.HasIcon = true;
                    }
                }
            }

            if (dict.ContainsKey("module"))
            {
                dynamic module = dict["module"];

                {
                    List<string> requestPermissions = [];

                    if (module.ContainsKey("requestPermissions"))
                    {
                        foreach (dynamic requestPermission in module["requestPermissions"])
                        {
                            if (requestPermission.ContainsKey("name"))
                            {
                                requestPermissions.Add(requestPermission["name"].ToString());
                            }
                        }
                    }
                    info.RequestPermissions = [.. requestPermissions];
                }
                {
                    List<string> deviceTypes = [];

                    if (module.ContainsKey("deviceTypes"))
                    {
                        foreach (dynamic deviceType in module["deviceTypes"])
                        {
                            deviceTypes.Add(deviceType.ToString());
                        }
                    }
                    info.DeviceTypes = [.. deviceTypes];
                }
            }
            return info;
        }

        return null;
    }
}
