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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows.Controls;
using System.Xml;

namespace QuickLook.Plugin.PluginInstaller
{
    public partial class PluginInfoPanel : UserControl
    {
        private readonly string _path;

        public PluginInfoPanel(string path)
        {
            InitializeComponent();

            // apply global theme
            Resources.MergedDictionaries[0].Clear();

            _path = path;
            ReadInfo();
        }

        private void ReadInfo()
        {
            filename.Text = Path.GetFileNameWithoutExtension(_path);

            var xml = LoadXml(GetFileFromZip(_path, "Metadata.config"));
        }

        private XmlDocument LoadXml(Stream data)
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

        private Stream GetFileFromZip(string archive, string entry)
        {
            var ms = new MemoryStream();

            using (var zip = ZipFile.Open(archive, ZipArchiveMode.Read))
            {
                using (var s = zip.GetEntry(entry)?.Open())
                {
                    s?.CopyTo(ms);
                }
            }

            return ms;
        }
    }
}