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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using QuickLook.ExtensionMethods;

namespace QuickLook.Plugin.PDFViewer
{
    /// <summary>
    ///     Interaction logic for PdfViewer.xaml
    /// </summary>
    public partial class PdfViewerControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        private const double MinZoomFactor = 0.1d;
        private const double MaxZoomFactor = 5d;
        private int _changePageDeltaSum;

        private bool _pdfLoaded;
        private double _viewRenderFactor = 1d;

        public PdfViewerControl()
        {
            InitializeComponent();

            listThumbnails.SelectionChanged += UpdatePageViewWhenSelectionChanged;

            pagePanel.DelayedReRender += ReRenderCurrentPageDelayed;
            pagePanel.ImageScrolled += NavigatePage;
        }

        public ObservableCollection<int> PageIds { get; set; } = new ObservableCollection<int>();

        public PdfFile PdfHandleForThumbnails { get; private set; }

        public PdfFile PdfHandle { get; private set; }

        public int TotalPages => PdfHandle.TotalPages;

        public int CurrentPage
        {
            get => listThumbnails.SelectedIndex;
            set
            {
                listThumbnails.SelectedIndex = value;
                listThumbnails.ScrollIntoView(listThumbnails.SelectedItem);

                CurrentPageChanged?.Invoke(this, new EventArgs());
                OnPropertyChanged();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (pagePanel != null)
            {
                pagePanel.DelayedReRender -= ReRenderCurrentPageDelayed;
                pagePanel.ImageScrolled -= NavigatePage;
            }

            _pdfLoaded = false;
            PdfHandleForThumbnails?.Dispose();
            PdfHandleForThumbnails = null;
            PdfHandle?.Dispose();
            PdfHandle = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ReRenderCurrentPageDelayed(object sender, EventArgs e)
        {
            ReRenderCurrentPage();
        }

        ~PdfViewerControl()
        {
            Dispose();
        }

        public event EventHandler CurrentPageChanged;

        private void NavigatePage(object sender, int delta)
        {
            if (!_pdfLoaded)
                return;

            var pos = pagePanel.GetScrollPosition();
            var size = pagePanel.GetScrollSize();

            const double tolerance = 0.0001d;
            if (Math.Abs(pos.Y) < tolerance && delta > 0)
            {
                _changePageDeltaSum += delta;
                if (Math.Abs(_changePageDeltaSum) < 20)
                    return;

                PrevPage();
                _changePageDeltaSum = 0;
            }
            else if (Math.Abs(pos.Y - size.Height) < tolerance && delta < -0)
            {
                _changePageDeltaSum += delta;
                if (Math.Abs(_changePageDeltaSum) < 20)
                    return;

                NextPage();
                _changePageDeltaSum = 0;
            }
        }

        private void NextPage()
        {
            if (CurrentPage < PdfHandle.TotalPages - 1)
            {
                CurrentPage++;
                pagePanel.ScrollToTop();
            }
        }

        private void PrevPage()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
                pagePanel.ScrollToBottom();
            }
        }

        private void ReRenderCurrentPage()
        {
            if (!_pdfLoaded)
                return;

            Debug.WriteLine($"Renrendering page {CurrentPage}");

            var pos = pagePanel.GetScrollPosition();

            var factor = pagePanel.ZoomFactor * _viewRenderFactor;
            factor = Math.Max(factor, MinZoomFactor);
            factor = Math.Min(factor, MaxZoomFactor);
            pagePanel.MinZoomFactor = MinZoomFactor / factor;
            pagePanel.MaxZoomFactor = MaxZoomFactor / factor;

            var bitmap = PdfHandle.GetPage(CurrentPage, factor);
            var image = bitmap.ToBitmapSource();
            bitmap.Dispose();

            pagePanel.Source = image;
            pagePanel.ResetZoom();

            _viewRenderFactor = factor;

            pagePanel.SetScrollPosition(pos);

            Dispatcher.Delay(500, t => GC.Collect());
        }

        private void UpdatePageViewWhenSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_pdfLoaded)
                return;

            if (CurrentPage == -1)
                return;

            CurrentPageChanged?.Invoke(this, new EventArgs());

            ReRenderCurrentPage();
        }

        public static Size GetDesiredControlSizeByFirstPage(string path)
        {
            var tempHandle = new PdfFile(path);

            var size = tempHandle.GetPageSize(0, 1d);
            tempHandle.Dispose();

            if (tempHandle.TotalPages > 1)
                size.Width += /*listThumbnails.ActualWidth*/ 150;

            return size;
        }

        public void LoadPdf(string path)
        {
            PageIds.Clear();

            PdfHandleForThumbnails = new PdfFile(path);
            PdfHandle = new PdfFile(path);
            _pdfLoaded = true;

            // fill thumbnails list
            Enumerable.Range(0, PdfHandle.TotalPages).ForEach(PageIds.Add);
            OnPropertyChanged(nameof(PageIds));

            if (PdfHandle.TotalPages < 2)
                listThumbnails.Visibility = Visibility.Collapsed;

            CurrentPage = 0;
            pagePanel.DoZoomToFit();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}