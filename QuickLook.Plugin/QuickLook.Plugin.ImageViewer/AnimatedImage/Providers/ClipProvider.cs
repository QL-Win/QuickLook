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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class ClipProvider : AnimationProvider
{
    private readonly string SQLITE_MAGIC = "SQLite format 3";
    private readonly string FOOTER_MARKER = "CHNKFoot";

    private string _tempSqlitePath;
    private SqliteConnection _conn;
    private byte[] _imageData;
    private BitmapSource _frame;

    public ClipProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        Animator = new Int32AnimationUsingKeyFrames();
        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));

        try
        {
            var raw = File.ReadAllBytes(path.LocalPath);

            var sqliteSig = System.Text.Encoding.ASCII.GetBytes(SQLITE_MAGIC);
            var sqliteIndex = IndexOf(raw, sqliteSig, 0);
            if (sqliteIndex < 0)
                return;

            var footerSig = System.Text.Encoding.ASCII.GetBytes(FOOTER_MARKER);
            var footerIndex = LastIndexOf(raw, footerSig);
            if (footerIndex < 0)
                footerIndex = raw.Length;

            var len = footerIndex - sqliteIndex;
            if (len <= 0)
                return;

            var sqliteBytes = new byte[len];
            Array.Copy(raw, sqliteIndex, sqliteBytes, 0, len);

            _tempSqlitePath = System.IO.Path.GetTempFileName();
            File.WriteAllBytes(_tempSqlitePath, sqliteBytes);

            _conn = new SqliteConnection($"Data Source={_tempSqlitePath};Mode=ReadOnly;");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT ImageData FROM CanvasPreview LIMIT 1";
            var obj = cmd.ExecuteScalar();
            if (obj is byte[] b)
            {
                _imageData = b;
            }
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }
    }

    public override void Dispose()
    {
        try
        {
            _conn?.Close();
            _conn?.Dispose();
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(_tempSqlitePath) && File.Exists(_tempSqlitePath))
            {
                File.Delete(_tempSqlitePath);
                _tempSqlitePath = null;
            }
        }
        catch { }

        _frame = null;
        _imageData = null;
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        return new Task<BitmapSource>(() =>
        {
            if (_imageData == null)
                return null;

            try
            {
                using var ms = new MemoryStream(_imageData);
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                var decodeWidth = (int)Math.Round(Math.Min(renderSize.Width, Math.Max(1d, Math.Floor(renderSize.Width))));
                var decodeHeight = (int)Math.Round(Math.Min(renderSize.Height, Math.Max(1d, Math.Floor(renderSize.Height))));

                img.DecodePixelWidth = decodeWidth;
                img.DecodePixelHeight = decodeHeight;
                img.StreamSource = ms;
                img.EndInit();

                Helper.DpiHack(img);
                img.Freeze();

                _frame = img;
                return img;
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
        });
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            if (_imageData == null)
                return null;

            if (_frame != null)
                return _frame;

            try
            {
                using var ms = new MemoryStream(_imageData);
                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                img.StreamSource = ms;
                img.EndInit();

                Helper.DpiHack(img);
                img.Freeze();

                _frame = img;
                return img;
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
        });
    }

    private static int IndexOf(byte[] array, byte[] pattern, int start)
    {
        if (array == null || pattern == null) return -1;
        for (int i = start; i <= array.Length - pattern.Length; i++)
        {
            var ok = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j])
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return i;
        }
        return -1;
    }

    private static int LastIndexOf(byte[] array, byte[] pattern)
    {
        if (array == null || pattern == null) return -1;
        for (int i = array.Length - pattern.Length; i >= 0; i--)
        {
            var ok = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j])
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return i;
        }
        return -1;
    }
}
