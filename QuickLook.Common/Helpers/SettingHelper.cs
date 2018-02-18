// Copyright © 2018 Paddy Xu
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
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

namespace QuickLook.Common.Helpers
{
    public class SettingHelper
    {
        public static readonly string LocalDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"pooi.moe\QuickLook\");

        private static readonly Dictionary<string, XmlDocument> FileCache = new Dictionary<string, XmlDocument>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Get<T>(string id, T failsafe = default(T), Assembly calling = null)
        {
            if (!typeof(T).IsSerializable && !typeof(ISerializable).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("A serializable Type is required");

            var file = Path.Combine(LocalDataPath,
                (calling ?? Assembly.GetCallingAssembly()).GetName().Name + ".config");

            var doc = GetConfigFile(file);

            // try to get setting
            var s = GetSettingFromXml(doc, id, failsafe);

            return s != null ? s : failsafe;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Set(string id, object value, Assembly calling = null)
        {
            if (!value.GetType().IsSerializable)
                throw new NotSupportedException("New value if not serializable.");

            var file = Path.Combine(LocalDataPath,
                (calling ?? Assembly.GetCallingAssembly()).GetName().Name + ".config");

            WriteSettingToXml(GetConfigFile(file), id, value);
        }

        private static T GetSettingFromXml<T>(XmlDocument doc, string id, T failsafe)
        {
            var v = doc.SelectSingleNode($@"/Settings/{id}");

            try
            {
                var result = v == null ? failsafe : (T) Convert.ChangeType(v.InnerText, typeof(T));
                return result;
            }
            catch (Exception)
            {
                return failsafe;
            }
        }

        private static void WriteSettingToXml(XmlDocument doc, string id, object value)
        {
            var v = doc.SelectSingleNode($@"/Settings/{id}");

            if (v != null)
            {
                v.InnerText = value.ToString();
            }
            else
            {
                var node = doc.CreateNode(XmlNodeType.Element, id, doc.NamespaceURI);
                node.InnerText = value.ToString();
                doc.SelectSingleNode(@"/Settings")?.AppendChild(node);
            }

            doc.Save(new Uri(doc.BaseURI).LocalPath);
        }

        private static XmlDocument GetConfigFile(string file)
        {
            if (FileCache.ContainsKey(file))
                return FileCache[file];

            Directory.CreateDirectory(Path.GetDirectoryName(file));
            if (!File.Exists(file))
                CreateNewConfig(file);

            var doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch (XmlException)
            {
                CreateNewConfig(file);
                doc.Load(file);
            }

            if (doc.SelectSingleNode(@"/Settings") == null)
            {
                CreateNewConfig(file);
                doc.Load(file);
            }

            FileCache.Add(file, doc);
            return doc;
        }

        private static void CreateNewConfig(string file)
        {
            using (var writer = XmlWriter.Create(file))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Settings");
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Flush();
            }
        }
    }
}