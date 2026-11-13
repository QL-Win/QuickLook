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

using Microsoft.Win32;
using QuickLook.Common.Commands;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QuickLook.Plugin.CLSIDViewer;

public partial class RecycleBinPanel : UserControl, INotifyPropertyChanged
{
    private readonly ContextObject _context;
    private RecycleBinHelper.RecycleBinInfo _info;
    private ICommand _emptyRecycleBinCommand;

    // ICommand must be used so that the button can be automatically disabled
    public ICommand EmptyRecycleBinCommand =>
        _emptyRecycleBinCommand ??= new AsyncRelayCommand(OnEmptyRecycleBinAsync);

    public RecycleBinPanel(ContextObject context)
    {
        _context = context;

        DataContext = this;
        InitializeComponent();

        emptyButton.Content = TranslationHelper.Get("RecycleBinButton",
            domain: Assembly.GetExecutingAssembly().GetName().Name);

        Loaded += OnLoaded;
    }

    protected virtual void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(() =>
        {
            UpdateState();
            _context.IsBusy = false;
        });
    }

    private Task OnEmptyRecycleBinAsync()
    {
        var domain = Assembly.GetExecutingAssembly().GetName().Name;

        var result = MessageBox.Show(
            string.Format(TranslationHelper.Get("ConfirmDeleteText", domain: domain), _info.TotalCount),
            TranslationHelper.Get("RecycleBinButton", domain: domain),
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return Task.CompletedTask;

        // The emptying itself runs in a detached QuickLook.exe helper process, so it keeps
        // running even when this viewer window is closed or another file gets previewed.
        // This panel only reflects the state; it must never wait for the deletion itself.
        emptyButton.IsEnabled = false;
        totalSizeAndCount.Text = TranslationHelper.Get(
            "RecycleBinEmptyingText",
            domain: domain,
            failsafe: "Emptying the Recycle Bin in the background...");

        Process helperProcess;
        try
        {
            helperProcess = Process.Start(new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Arguments = "--empty-recycle-bin",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            });

            if (helperProcess == null)
                throw new InvalidOperationException("The helper process could not be started.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            MessageBox.Show(
                "QuickLook could not start the background process to empty the Recycle Bin."
                + Environment.NewLine + Environment.NewLine + ex.Message,
                TranslationHelper.Get("RecycleBinButton", domain: domain),
                MessageBoxButton.OK, MessageBoxImage.Error);

            emptyButton.IsEnabled = true;
            UpdateState();
            return Task.CompletedTask;
        }

        // Never block the UI thread. Waiting is only used to refresh the panel once the
        // helper is done; the window (and this plugin instance) may be gone by then.
        _ = Task.Run(() =>
        {
            try
            {
                helperProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                try
                {
                    helperProcess.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            try
            {
                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        UpdateState();
                        emptyButton.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                // The viewer window may have been closed while the helper was running.
                Debug.WriteLine(ex);
            }
        });

        return Task.CompletedTask;
    }

    private void UpdateState()
    {
        _info = RecycleBinHelper.GetRecycleBinInfo();

        Dispatcher.BeginInvoke(() =>
        {
            image.Source = _info.HasTrash ? _info.FullIcon : _info.EmptyIcon;

            if (_info.HasTrash)
            {
                totalSizeAndCount.Text = string.Format(
                    TranslationHelper.Get("RecycleBinSizeText",
                        domain: Assembly.GetExecutingAssembly().GetName().Name),
                    _info.TotalSizeString,
                    _info.TotalCount
                );
            }
            else
            {
                totalSizeAndCount.Text = TranslationHelper.Get("RecycleBinEmptyText",
                    domain: Assembly.GetExecutingAssembly().GetName().Name);
            }
            emptyButton.Visibility = _info.HasTrash ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal static class RecycleBinHelper
{
    public static ImageSource _emptyIcon = null;
    public static ImageSource _fullIcon = null;

    public sealed class RecycleBinInfo
    {
        /// <summary>
        /// In bytes
        /// </summary>
        public ulong TotalSize { get; set; } = 0L;

        public string TotalSizeString => FormatBytes(TotalSize);

        public ulong TotalCount { get; set; } = 0L;

        public bool HasTrash => TotalCount > 0;

        public ImageSource EmptyIcon { get; set; } = _emptyIcon;

        public ImageSource FullIcon { get; set; } = _fullIcon;

        private static string FormatBytes(ulong bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = bytes;
            int order = 0;
            while (len >= 1024d && order < sizes.Length - 1)
            {
                order++;
                len /= 1024d;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHQUERYRBINFO
    {
        public uint cbSize;
        public ulong i64Size;
        public ulong i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int ExtractIconEx(string lpszFile, int nIconIndex, nint[] phiconLarge, nint[] phiconSmall, int nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);

    public static RecycleBinInfo GetRecycleBinInfo()
    {
        var info = new SHQUERYRBINFO()
        {
            cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO))
        };

        int result = SHQueryRecycleBin(null, ref info);

        ImageSource[] bitmapSources = [_emptyIcon, _fullIcon];

        if (Environment.OSVersion.Version.Major >= 10)
        {
            if (bitmapSources[0] is null || bitmapSources[1] is null)
            {
                bitmapSources[0] = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.CLSIDViewer;component/Resources/recyclebinempty.png"));
                bitmapSources[1] = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.CLSIDViewer;component/Resources/recyclebinfull.png"));

                if (bitmapSources[0].CanFreeze) bitmapSources[0].Freeze();
                if (bitmapSources[1].CanFreeze) bitmapSources[1].Freeze();
            }
        }
        else
        {
            string[] icons = GetIcons();

            if (icons.Length >= 2)
            {
                if (bitmapSources[0] is null || bitmapSources[1] is null)
                {
                    bitmapSources[0] = ExtractIconBitmap(icons[0]);
                    bitmapSources[1] = ExtractIconBitmap(icons[1]);
                }
            }
        }

        if (result == 0) // S_OK (0)
        {
            var output = new RecycleBinInfo()
            {
                TotalSize = info.i64Size,
                TotalCount = info.i64NumItems,
                EmptyIcon = bitmapSources[0],
                FullIcon = bitmapSources[1],
            };

            return output;
        }
        return default;
    }

    private static string[] GetIcons()
    {
        const string keyPath = @"CLSID\{645FF040-5081-101B-9F08-00AA002F954E}\DefaultIcon";
        using RegistryKey key = Registry.ClassesRoot.OpenSubKey(keyPath);

        if (key != null)
        {
            if (key.GetValue("Empty") is string emptyIcon
             && key.GetValue("Full") is string fullIcon)
            {
                return [emptyIcon, fullIcon];
            }
        }

        return null;
    }

    private static ImageSource ExtractIconBitmap(string resourcePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;

            string expanded = Environment.ExpandEnvironmentVariables(resourcePath);
            string[] parts = expanded.Split(',');

            if (parts.Length != 2 || !int.TryParse(parts[1], out int iconIndex)) return null;

            string dllPath = parts[0];

            nint[] icons = new nint[1];
            int count = ExtractIconEx(dllPath, iconIndex, icons, null, 1);

            if (count > 0 && icons[0] != IntPtr.Zero)
            {
                try
                {
                    using Icon icon = Icon.FromHandle(icons[0]);
                    return ((Icon)icon.Clone()).ToImageSource();
                }
                finally
                {
                    DestroyIcon(icons[0]);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
        return null;
    }

    private static ImageSource ToImageSource(this Icon icon)
    {
        var imageSource = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        imageSource.Freeze();
        return imageSource;
    }
}
