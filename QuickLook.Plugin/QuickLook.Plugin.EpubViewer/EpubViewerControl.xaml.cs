// Copyright © 2018 Marco Gavelli and Paddy Xu
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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using QuickLook.Common.Annotations;
using QuickLook.Common.Plugin;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    public partial class EpubViewerControl : IDisposable
    {
        private ContextObject _context;
        private List<EpubChapterRef> _chapterRefs;
        private int _currChapter;

        private EpubBookRef _epubBook;

        public EpubViewerControl(ContextObject context)
        {
            _context = context;

            InitializeComponent();

            // design-time only
            Resources.MergedDictionaries.Clear();

            DataContext = this;

            buttonPrevChapter.Click += (sender, e) => PrevChapter();
            buttonNextChapter.Click += (sender, e) => NextChapter();
        }

        public string Chapter => _chapterRefs != null && _currChapter >= 0
            ? $"{_chapterRefs?[_currChapter].Title} ({_currChapter + 1}/{_chapterRefs?.Count})"
            : "";

        public void Dispose()
        {
            _chapterRefs.Clear();
            _epubBook?.Dispose();
            _epubBook = null;
        }

        internal void SetContent(EpubBookRef epubBook)
        {
            _epubBook = epubBook;
            _chapterRefs = Flatten(epubBook.GetChapters());
            _currChapter = -1;
            pagePanel.EpubBook = epubBook;
            UpdateChapter();
        }

        private static List<EpubChapterRef> Flatten(IEnumerable<EpubChapterRef> list)
        {
            return list.SelectMany(l => new[] {l}.Concat(Flatten(l.SubChapters))).ToList();
        }

        private void NextChapter()
        {
            try
            {
                _currChapter = Math.Min(_currChapter + 1, _chapterRefs.Count - 1);
                UpdateChapter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                pagePanel.Text = "<div>Invalid chapter.</div>";
            }
        }

        private void PrevChapter()
        {
            try
            {
                _currChapter = Math.Max(_currChapter - 1, -1);
                UpdateChapter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                pagePanel.Text = "<div>Invalid chapter.</div>";
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                PrevChapter();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                NextChapter();
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void UpdateChapter()
        {
            if (_currChapter < 0)
            {
                pagePanel.ChapterRef = null;
                pagePanel.Text = $@"
                <div style=""margin:4pt; text-align: center;"">
                    <img src=""COVER"" alt=""COVER"" style=""height:500px;""/>
                    <div style=""word-wrap: break-word;"">{_epubBook.Title}</div>
                </div>";

                _context.Title = _epubBook.Title;
            }
            else
            {
                pagePanel.ChapterRef = _chapterRefs[_currChapter];
                if (_chapterRefs[_currChapter].Anchor != null)
                    pagePanel.ScrollToElement(_chapterRefs[_currChapter].Anchor);

                _context.Title =
                    $"{_epubBook.Title}: {_chapterRefs[_currChapter].Title} ({_currChapter}/{_chapterRefs.Count})";
            }
        }
    }
}