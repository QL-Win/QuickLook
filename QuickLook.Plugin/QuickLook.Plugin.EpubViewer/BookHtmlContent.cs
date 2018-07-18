using System;
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
        public static readonly DependencyProperty ChapterRefProperty = DependencyProperty.Register("ChapterRef", typeof(EpubChapterRef), typeof(BookHtmlContent), new PropertyMetadata(OnChapterRefChanged));

        public EpubChapterRef ChapterRef
        {
            get { return (EpubChapterRef)GetValue(ChapterRefProperty); }
            set { SetValue(ChapterRefProperty, value); }
        }

        public static readonly DependencyProperty EpubBookProperty = DependencyProperty.Register("EpubBook", typeof(EpubBookRef), typeof(BookHtmlContent), new PropertyMetadata(null));

        public EpubBookRef EpubBook
        {
            get { return (EpubBookRef)GetValue(EpubBookProperty); }
            set { SetValue(EpubBookProperty, value); }
        }

        protected override void OnStylesheetLoad(HtmlStylesheetLoadEventArgs args)
        {
            string styleSheetFilePath = GetFullPath(ChapterRef.ContentFileName, args.Src);
            if (EpubBook.Content.Css.TryGetValue(styleSheetFilePath, out EpubTextContentFileRef styleSheetContent))
            {
                args.SetStyleSheet = styleSheetContent.ReadContentAsText();
            }
        }

        protected override async void OnImageLoad(HtmlImageLoadEventArgs args)
        {
            string imageFilePath = ChapterRef != null ? GetFullPath(ChapterRef.ContentFileName, args.Src) : null;
            byte[] imageBytes = null;
            if (args.Src == "COVER")
            {
                imageBytes = await EpubBook.ReadCoverAsync();
            }
            else if (EpubBook.Content.Images.TryGetValue(imageFilePath, out EpubByteContentFileRef imageContent))
            {
                imageBytes = await imageContent.ReadContentAsBytesAsync();
            }

            if (imageBytes != null)
            {
                using (MemoryStream imageStream = new MemoryStream(imageBytes))
                {
                    try
                    {
                        BitmapImage bitmapImage = new BitmapImage();
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
        }

        private string GetFullPath(string htmlFilePath, string relativePath)
        {
            if (relativePath.StartsWith("/"))
            {
                return relativePath.Length > 1 ? relativePath.Substring(1) : String.Empty;
            }
            string basePath = System.IO.Path.GetDirectoryName(htmlFilePath);
            while (relativePath.StartsWith("../"))
            {
                relativePath = relativePath.Length > 3 ? relativePath.Substring(3) : String.Empty;
                basePath = System.IO.Path.GetDirectoryName(basePath);
            }
            string fullPath = String.Concat(basePath.Replace('\\', '/'), '/', relativePath);
            return fullPath.StartsWith("/") ? fullPath.Length > 1 ? fullPath.Substring(1) : String.Empty : fullPath;
        }

        private static async void OnChapterRefChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            BookHtmlContent bookHtmlContent = dependencyObject as BookHtmlContent;
            if (bookHtmlContent == null || bookHtmlContent.ChapterRef == null)
            {
                return;
            }
            bookHtmlContent.Text = await bookHtmlContent.ChapterRef.ReadHtmlContentAsync();
        }
    }
}
