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

using FellowOakDicom;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer;

public partial class MetaProvider
{
    private static bool IsDicomFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".dcm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".dicom", StringComparison.OrdinalIgnoreCase);
    }

    private static Size TryGetDicomSize(string path)
    {
        try
        {
            var file = DicomFile.Open(path);
            var rows = file.Dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
            var columns = file.Dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);

            if (rows > 0 && columns > 0)
                return new Size(columns, rows);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        return Size.Empty;
    }
}
