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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.PDFViewer;

public partial class PdfViewerControl : UserControl, INotifyPropertyChanged, IDisposable
{
    private int _changePageDeltaSum;
    private bool _initPage = true;
    private int _renderRequestId;
    private double _maxZoomFactor = double.NaN;
    private double _minZoomFactor = double.NaN;

    private bool _pdfLoaded;
    private double _viewRenderFactor = double.NaN;

    public PdfViewerControl()
    {
        InitializeComponent();

        // remove theme used in designer
        Resources.MergedDictionaries.RemoveAt(0);

        listThumbnails.SelectionChanged += UpdatePageViewWhenSelectionChanged;

        pagePanel.ZoomChanged += ReRenderCurrentPageDelayed;
        pagePanel.ImageScrolled += NavigatePage;
    }

    public ObservableCollection<int> PageThumbnails { get; private set; } = [];

    public int TotalPages => PdfDocumentWrapper.PageCount;

    public int CurrentPage
    {
        get => listThumbnails.SelectedIndex;
        set
        {
            listThumbnails.SelectedIndex = value;
            listThumbnails.ScrollIntoView(listThumbnails.SelectedItem);

            CurrentPageChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged();
        }
    }

    public PdfDocumentWrapper PdfDocumentWrapper { get; private set; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (pagePanel != null)
        {
            pagePanel.ZoomChanged -= ReRenderCurrentPageDelayed;
            pagePanel.ImageScrolled -= NavigatePage;
        }

        _renderRequestId++;
        _pdfLoaded = false;
        PdfDocumentWrapper?.Dispose();
        PdfDocumentWrapper = null;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ReRenderCurrentPageDelayed(object sender, EventArgs e)
    {
        ReRenderCurrentPage();
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
        if (CurrentPage < TotalPages - 1)
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

    private async void ReRenderCurrentPage()
    {
        if (!_pdfLoaded)
            return;

        var page = CurrentPage;
        var document = PdfDocumentWrapper;
        if (document == null || page < 0 || page >= TotalPages)
            return;

        Debug.WriteLine($"Re-rendering page {page}");

        var pos = pagePanel.GetScrollPosition();
        var requestId = ++_renderRequestId;

        double factor;
        var pageSize = document.GetPageSize(page);

        // First time showing. Set thresholds here.
        if (double.IsNaN(_minZoomFactor) || double.IsNaN(_maxZoomFactor))
        {
            factor = Math.Min(pagePanel.ActualHeight / pageSize.Height,
                pagePanel.ActualWidth / pageSize.Width);
            _viewRenderFactor = factor;
            _minZoomFactor = 0.1 * factor;
            _maxZoomFactor = 5 * factor;
        }
        else if (pagePanel.ZoomToFit)
        {
            factor = Math.Min(pagePanel.ActualHeight / pageSize.Height,
                pagePanel.ActualWidth / pageSize.Width);
        }
        else
        {
            factor = pagePanel.ZoomFactor * _viewRenderFactor;
            factor = Math.Max(factor, _minZoomFactor);
            factor = Math.Min(factor, _maxZoomFactor);
            pagePanel.MinZoomFactor = _minZoomFactor / factor;
            pagePanel.MaxZoomFactor = _maxZoomFactor / factor;
        }

        BitmapSource image;
        try
        {
            image = await Task.Run(() => document.Render(page, factor));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to render PDF page {page}: {ex}");
            return;
        }

        if (!_pdfLoaded || requestId != _renderRequestId || !ReferenceEquals(document, PdfDocumentWrapper))
            return;

        pagePanel.Source = image;
        pagePanel.ResetZoom();

        _viewRenderFactor = factor;

        pagePanel.SetScrollPosition(pos);
    }

    private void UpdatePageViewWhenSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_pdfLoaded)
            return;

        if (CurrentPage == -1)
            return;

        CurrentPageChanged?.Invoke(this, EventArgs.Empty);

        ReRenderCurrentPage();

        if (_initPage)
        {
            _initPage = false;
            pagePanel.DoZoomToFit();
        }
    }

    public static Size GetDesiredControlSizeByFirstPage(PdfDocumentWrapper document)
    {
        var size = new Size(0, 0);
        for (var page = 0; page < Math.Min(5, document.PageCount); page++)
        {
            var pageSize = document.GetPageSize(page);
            size.Width = Math.Max(size.Width, pageSize.Width);
            size.Height = Math.Max(size.Height, pageSize.Height);
        }

        if (document.PageCount > 1)
            size.Width += 150;

        return new Size(size.Width * 3, size.Height * 3);
    }

    public void LoadPdf(string path, string password = null)
    {
        LoadPdf(new PdfDocumentWrapper(path, password));
    }

    public void LoadPdf(PdfDocumentWrapper document)
    {
        var previousDocument = PdfDocumentWrapper;
        _pdfLoaded = false;
        _renderRequestId++;
        listThumbnails.SelectedIndex = -1;
        if (previousDocument != null && !ReferenceEquals(previousDocument, document))
            previousDocument.Dispose();

        PdfDocumentWrapper = document;
        _pdfLoaded = true;
        _initPage = true;
        _minZoomFactor = double.NaN;
        _maxZoomFactor = double.NaN;
        _viewRenderFactor = double.NaN;

        if (PdfDocumentWrapper.PageCount < 2)
            listThumbnails.Visibility = Visibility.Collapsed;
        else
            listThumbnails.Visibility = Visibility.Visible;

        BeginLoadThumbnails();

        if (PdfDocumentWrapper.PageCount > 0)
            listThumbnails.SelectedIndex = 0;
    }

    public void LoadPdf(MemoryStream stream)
    {
        stream.Position = 0;
        LoadPdf(new PdfDocumentWrapper(stream));
    }

    private void BeginLoadThumbnails()
    {
        PageThumbnails = new ObservableCollection<int>(Enumerable.Range(0, PdfDocumentWrapper.PageCount));
        OnPropertyChanged(nameof(PageThumbnails));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
