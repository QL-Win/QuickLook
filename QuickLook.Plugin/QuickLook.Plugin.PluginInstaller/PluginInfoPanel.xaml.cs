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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace QuickLook.Plugin.PluginInstaller;

public partial class PluginInfoPanel : UserControl
{
    private readonly ContextObject _context;
    private readonly string _path;
    private string _namespace;

    public PluginInfoPanel(string path, ContextObject context)
    {
        InitializeComponent();

        // apply global theme
        Resources.MergedDictionaries[0].Clear();

        _path = path;
        _context = context;
        ReadInfo();

        btnInstall.Click += BtnInstall_Click;
    }

    private void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        btnInstall.Content = "Installing ...";
        btnInstall.IsEnabled = false;

        var t = DoInstall();
        t.ContinueWith(_ =>
            Dispatcher.BeginInvoke(new Action(() => btnInstall.Content = "Done! Please restart QuickLook.")));
        t.Start();
    }

    private Task DoInstall()
    {
        var targetFolder = Path.Combine(App.UserPluginPath, _namespace);
        return new Task(() =>
        {
            CleanUp();

            try
            {
                ZipFile.ExtractToDirectory(_path, targetFolder);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => description.Text = ex.Message));
                Dispatcher.BeginInvoke(new Action(() => btnInstall.Content = "Installation failed."));
                CleanUp();
            }
        });

        void CleanUp()
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                return;
            }

            try
            {
                Directory.GetFiles(targetFolder, "*", SearchOption.AllDirectories)
                    .ForEach(file => File.Move(file,
                        Path.Combine(Path.GetDirectoryName(file), Guid.NewGuid() + ".to_be_deleted")));
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => description.Text = ex.Message));
                Dispatcher.BeginInvoke(new Action(() => btnInstall.Content = "Installation failed."));
            }
        }
    }

    private void ReadInfo()
    {
        filename.Text = Path.GetFileNameWithoutExtension(_path);

        var xml = LoadXml(GetFileFromZip(_path, "QuickLook.Plugin.Metadata.config"));

        _namespace = GetString(xml, @"/Metadata/Namespace");

        var okay = _namespace != null && _namespace.StartsWith("QuickLook.Plugin.");

        filename.Text = okay ? _namespace : "Invalid plugin.";
        version.Text = "Version " + GetString(xml, @"/Metadata/Version", "not defined");
        description.Text = GetString(xml, @"/Metadata/Description", string.Empty);

        btnInstall.Visibility = okay ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetString(XmlNode xml, string xpath, string def = null)
    {
        var n = xml?.SelectSingleNode(xpath);

        return n?.InnerText ?? def;
    }

    private static XmlDocument LoadXml(Stream data)
    {
        var doc = new XmlDocument();
        try
        {
            doc.Load(data);
            return doc;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static MemoryStream GetFileFromZip(string archive, string entry)
    {
        var ms = new MemoryStream();

        try
        {
            using (var zip = ZipFile.Open(archive, ZipArchiveMode.Read))
            {
                using (var s = zip?.GetEntry(entry)?.Open())
                {
                    s?.CopyTo(ms);
                }
            }
        }
        catch (Exception)
        {
            return ms;
        }

        ms.Position = 0;
        return ms;
    }
}
