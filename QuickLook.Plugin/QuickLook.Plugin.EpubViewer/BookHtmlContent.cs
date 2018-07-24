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

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.WPF;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    public class BookHtmlContent : HtmlPanel
    {
        public static readonly DependencyProperty ChapterRefProperty = DependencyProperty.Register("ChapterRef",
            typeof(EpubChapterRef), typeof(BookHtmlContent), new PropertyMetadata(OnChapterRefChanged));

        public static readonly DependencyProperty EpubBookProperty = DependencyProperty.Register("EpubBook",
            typeof(EpubBookRef), typeof(BookHtmlContent), new PropertyMetadata(null));

        public EpubChapterRef ChapterRef
        {
            get => (EpubChapterRef) GetValue(ChapterRefProperty);
            set => SetValue(ChapterRefProperty, value);
        }

        public EpubBookRef EpubBook
        {
            get => (EpubBookRef) GetValue(EpubBookProperty);
            set => SetValue(EpubBookProperty, value);
        }

        protected override void OnStylesheetLoad(HtmlStylesheetLoadEventArgs args)
        {
            var styleSheetFilePath = GetFullPath(ChapterRef.ContentFileName, args.Src);
            if (EpubBook.Content.Css.TryGetValue(styleSheetFilePath, out var styleSheetContent))
                args.SetStyleSheet = styleSheetContent.ReadContentAsText();
        }

        protected override async void OnImageLoad(HtmlImageLoadEventArgs args)
        {
            var imageFilePath = ChapterRef != null ? GetFullPath(ChapterRef.ContentFileName, args.Src) : null;
            byte[] imageBytes = null;
            if (args.Src == "COVER")
                imageBytes = await EpubBook.ReadCoverAsync();
            else if (EpubBook.Content.Images.TryGetValue(imageFilePath, out var imageContent))
                imageBytes = await imageContent.ReadContentAsBytesAsync();

            if (imageBytes != null)
                using (var imageStream = new MemoryStream(imageBytes))
                {
                    try
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = imageStream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        args.Callback(bitmapImage);
                        args.Handled = true;
                    }
                    catch
                    {
                        Debug.WriteLine($"Failed to load image: {args.Src}");
                    }
                }
        }

        private string GetFullPath(string htmlFilePath, string relativePath)
        {
            if (relativePath.StartsWith("/")) return relativePath.Length > 1 ? relativePath.Substring(1) : string.Empty;
            var basePath = Path.GetDirectoryName(htmlFilePath);
            while (relativePath.StartsWith("../"))
            {
                relativePath = relativePath.Length > 3 ? relativePath.Substring(3) : string.Empty;
                basePath = Path.GetDirectoryName(basePath);
            }

            var fullPath = string.Concat(basePath.Replace('\\', '/'), '/', relativePath);
            return fullPath.StartsWith("/") ? fullPath.Length > 1 ? fullPath.Substring(1) : string.Empty : fullPath;
        }

        private static async void OnChapterRefChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(dependencyObject is BookHtmlContent bookHtmlContent) || bookHtmlContent.ChapterRef == null)
                return;
            bookHtmlContent.Text = await bookHtmlContent.ChapterRef.ReadHtmlContentAsync();
        }
    }
}