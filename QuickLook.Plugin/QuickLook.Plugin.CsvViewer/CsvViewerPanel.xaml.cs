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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CsvHelper;

namespace QuickLook.Plugin.CsvViewer
{
    /// <summary>
    ///     Interaction logic for CsvViewerPanel.xaml
    /// </summary>
    public partial class CsvViewerPanel : UserControl
    {
        public CsvViewerPanel()
        {
            InitializeComponent();
        }

        public List<string[]> Rows { get; private set; } = new List<string[]>();

        public void LoadFile(string path)
        {
            const int limit = 10000;
            var binded = false;

            using (var sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                using (var parser = new CsvParser(sr))
                {
                    var i = 0;
                    while (true)
                    {
                        var row = parser.Read();
                        if (row == null)
                            break;
                        row = Concat(new[] {$"{i++ + 1}".PadLeft(6)}, row);

                        if (!binded)
                        {
                            SetupColumnBinding(row.Length);
                            binded = true;
                        }

                        if (i > limit)
                        {
                            Rows.Add(Enumerable.Repeat("...", row.Length).ToArray());
                            break;
                        }

                        Rows.Add(row);
                    }
                }
            }
        }

        private void SetupColumnBinding(int rowLength)
        {
            for (var i = 0; i < rowLength; i++)
            {
                var col = new DataGridTextColumn
                {
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeight.FromOpenTypeWeight(i == 0 ? 700 : 400),
                    Binding = new Binding($"[{i}]")
                };
                dataGrid.Columns.Add(col);
            }
        }

        public static T[] Concat<T>(T[] x, T[] y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");
            var oldLen = x.Length;
            Array.Resize(ref x, x.Length + y.Length);
            Array.Copy(y, 0, x, oldLen, y.Length);
            return x;
        }
    }
}