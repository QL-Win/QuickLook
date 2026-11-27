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

using PdfiumViewer;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.PDFViewer;

public sealed class Plugin : IViewer
{
    private ContextObject _context;
    private string _path;
    private PdfViewerControl _pdfControl;
    private PasswordControl _passwordControl;
    private CancellationTokenSource _loadCts;
    private Task<PdfDocumentWrapper> _loadTask;

    /// <summary>How long Prepare() waits for the document to get the exact window size.</summary>
    private const int PrepareWaitMilliseconds = 200;

    public int Priority => -1;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        if (File.Exists(path) && Path.GetExtension(path).EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return true;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] buffer = new byte[4];
        if (fs.Read(buffer, 0, 4) < 4) return false;
        return buffer[0] == (byte)'%' &&
               buffer[1] == (byte)'P' &&
               buffer[2] == (byte)'D' &&
               buffer[3] == (byte)'F';
    }

    public void Prepare(string path, ContextObject context)
    {
        _context = context;
        _path = path;

        // Load on a background thread: a large PDF takes seconds to read, and blocking the UI
        // thread here would also block switching to the next file until it finishes.
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        _loadTask = Task.Run(() =>
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new PdfDocumentWrapper(stream, null, token);
        }, token);

        try
        {
            // Typical documents are ready within this wait, so the window still gets the page size.
            if (_loadTask.Wait(PrepareWaitMilliseconds))
            {
                context.SetPreferredSizeFit(PdfViewerControl.GetDesiredControlSize(_loadTask.Result.PdfDocument), 0.9);
                return;
            }

            // Still loading: assume an A4 portrait page
            context.SetPreferredSizeFit(new Size(595 * 3, 842 * 3), 0.9);
        }
        catch (AggregateException)
        {
            // Fallback to a size to request the password, or to show the error (handled in View)
            context.PreferredSize = new Size { Width = 800, Height = 600 };
        }
    }

    public void View(string path, ContextObject context)
    {
        _pdfControl = new PdfViewerControl();
        context.ViewerContent = _pdfControl;

        var control = _pdfControl;
        var loadTask = _loadTask;

        if (loadTask.IsCompleted)
        {
            // Keep the original behaviour: let the control be loaded first, and rethrow load
            // errors so QuickLook falls back to another viewer.
            Exception exception = null;
            _ = control.Dispatcher.BeginInvoke(() => exception = OnDocumentLoaded(loadTask, control, path, context),
                DispatcherPriority.Loaded).Wait();

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
            return;
        }

        loadTask.ContinueWith(_ => control.Dispatcher.BeginInvoke(() =>
        {
            // Superseded by another file meanwhile: Cleanup() releases the document
            if (_pdfControl != control)
                return;

            var exception = OnDocumentLoaded(loadTask, control, path, context);
            if (exception != null)
            {
                context.ViewerContent = new TextBlock
                {
                    Text = exception.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(24),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                context.Title = Path.GetFileName(path);
                context.IsBusy = false;
            }
        }, DispatcherPriority.Loaded), TaskScheduler.Default);
    }

    /// <summary>
    /// Shows the loaded document, or the password prompt. Returns any other load error.
    /// </summary>
    private Exception OnDocumentLoaded(Task<PdfDocumentWrapper> loadTask, PdfViewerControl control, string path, ContextObject context)
    {
        try
        {
            control.LoadPdf(loadTask.GetAwaiter().GetResult());

            context.Title = $"1 / {control.TotalPages}: {Path.GetFileName(path)}";

            control.CurrentPageChanged += UpdateWindowCaption;
            context.IsBusy = false;
        }
        catch (PdfException ex) when (ex.Message == "Password required or incorrect password")
        {
            // Fallback to request a password
            _passwordControl = new PasswordControl();
            _passwordControl.PasswordRequested += (string password) =>
            {
                try
                {
                    var desiredSize = PdfViewerControl.GetDesiredControlSizeByFirstPage(path, password);
                    context.SetPreferredSizeFit(desiredSize, 0.9); // Actually it is no longer effective here
                    context.ViewerContent = _pdfControl;

                    // Call the viewer window private method using reflection
                    // QuickLook.ViewerWindow.ResizeAndCentreExistingWindow
                    if (Window.GetWindow(_pdfControl) is Window window)
                    {
                        var ResizeAndCentreExistingWindow = window.GetType().GetMethod("ResizeAndCentreExistingWindow",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (ResizeAndCentreExistingWindow != null)
                        {
                            var newRect = (Rect)ResizeAndCentreExistingWindow.Invoke(window, [context.PreferredSize]);

                            window.MoveWindow(newRect.Left, newRect.Top, newRect.Width, newRect.Height);
                        }
                    }

                    context.IsBusy = true;
                    _pdfControl.LoadPdf(path, password);

                    context.Title = $"1 / {_pdfControl.TotalPages}: {Path.GetFileName(path)}";

                    _pdfControl.CurrentPageChanged += UpdateWindowCaption;
                    context.IsBusy = false;
                }
                catch (PdfException ex) when (ex.Message == "Password required or incorrect password")
                {
                    // This password is not accepted
                    return false;
                }

                // This password is accepted
                return true;
            };

            context.ViewerContent = _passwordControl;
            context.Title = $"[PASSWORD PROTECTED] {Path.GetFileName(path)}";
            context.IsBusy = false;
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        // Stop reading the file if it is still loading
        _loadCts?.Cancel();

        var adopted = _pdfControl?.PdfDocumentWrapper;
        _pdfControl?.Dispose();
        _pdfControl = null;

        // Release a document that finished loading but was never shown
        _loadTask?.ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion && !ReferenceEquals(t.Result, adopted))
                t.Result.Dispose();
        }, TaskScheduler.Default);
        _loadTask = null;

        _context = null;
    }

    private void UpdateWindowCaption(object sender, EventArgs e2)
    {
        _context.Title = $"{_pdfControl.CurrentPage + 1} / {_pdfControl.TotalPages}: {Path.GetFileName(_path)}";
    }
}
