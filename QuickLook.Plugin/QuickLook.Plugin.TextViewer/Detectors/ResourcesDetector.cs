// Copyright © 2017-2026 QL-Win Contributors
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
using System.Collections;
using System.IO;
using System.Resources;
using System.Text;

namespace QuickLook.Plugin.TextViewer.Detectors;

public sealed class ResourcesDetector : ITransferFormatDetector
{
    public string Name => "Resources";

    public string Extension => ".properties";

    public string OriginalExtension => ".resources";

    public bool Detect(string path, string text)
    {
        _ = text;
        if (string.IsNullOrEmpty(path)) return false;
        if (!Path.GetExtension(path).Equals(OriginalExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        // .resources header: MagicNumber (0xBEEFCACE) + HeaderVersionNumber
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < sizeof(int) * 2) return false;

            using var br = new BinaryReader(fs);
            return br.ReadInt32() == ResourceManager.MagicNumber
                && br.ReadInt32() >= 1;
        }
        catch
        {
            return false;
        }
    }

    public string Transfer(string path)
    {
        if (!Detect(path, null)) return null;

        try
        {
            using var reader = new ResourceReader(path);
            var sb = new StringBuilder();

            sb.AppendLine("# .resources dump (via ResourceReader)");
            sb.AppendLine($"# Source: {path}");
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            foreach (DictionaryEntry entry in reader)
            {
                var key = EscapeKey(entry.Key?.ToString() ?? string.Empty);
                var value = entry.Value is null ? string.Empty : EscapeValue(entry.Value.ToString());
                sb.Append(key).Append(' ').Append('=').Append(' ').AppendLine(value);
            }

            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeKey(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var c in key)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;

                case '=':
                    sb.Append(@"\=");
                    break;

                case ':':
                    sb.Append(@"\:");
                    break;

                case ' ':
                    sb.Append(@"\ ");
                    break;

                case '\t':
                    sb.Append(@"\t");
                    break;

                case '\n':
                    sb.Append(@"\n");
                    break;

                case '\r':
                    sb.Append(@"\r");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;

                case '\t':
                    sb.Append(@"\t");
                    break;

                case '\n':
                    sb.Append(@"\n");
                    break;

                case '\r':
                    sb.Append(@"\r");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
