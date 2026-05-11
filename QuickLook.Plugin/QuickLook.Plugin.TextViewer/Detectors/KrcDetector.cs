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

using KRCLib;
using System;
using System.IO;

namespace QuickLook.Plugin.TextViewer.Detectors;

public sealed class KrcDetector : ITransferFormatDetector
{
    public string Name => "INI";

    public string Extension => ".ini"; // .krc is more like .ini than .lrc

    public string RealExtension => ".krc";

    public bool Detect(string path, string text)
    {
        _ = text;
        if (string.IsNullOrEmpty(path)) return false;
        return Path.GetExtension(path).Equals(RealExtension, StringComparison.OrdinalIgnoreCase);
    }

    public string Transfer(string path)
    {
        if (!Detect(path, null)) return null;

        KRCLyrics krc = KRCLyrics.LoadFromFile(path);
        return krc.SaveToString();
    }
}
