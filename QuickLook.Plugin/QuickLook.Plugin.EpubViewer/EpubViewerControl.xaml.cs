using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    /// <summary>
    /// Logica di interazione per EpubViewerControl.xaml
    /// </summary>
    public partial class EpubViewerControl : UserControl, INotifyPropertyChanged
    {
        private EpubBookRef epubBook;
        private List<EpubChapterRef> chapterRefs;
        private int currChapter;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Chapter => $"{chapterRefs?[currChapter].Title} ({currChapter + 1}/{chapterRefs?.Count})";

        public EpubViewerControl()
        {
            InitializeComponent();

            // design-time only
            Resources.MergedDictionaries.Clear();

            this.DataContext = this;
            this.pagePanel.ImageLoad += PagePanel_ImageLoad;
        }

        private void PagePanel_ImageLoad(object sender, TheArtOfDev.HtmlRenderer.WPF.RoutedEvenArgs<TheArtOfDev.HtmlRenderer.Core.Entities.HtmlImageLoadEventArgs> args)
        {
            var key = this.epubBook.Content.Images.Keys.FirstOrDefault(k => args.Data.Src.IndexOf(k, StringComparison.InvariantCultureIgnoreCase) >= 0);
            if (key != null)
            {
                var img = ImageFromStream(this.epubBook.Content.Images[key].GetContentStream());
                args.Data.Callback(img);
                args.Handled = true;
            }
        }

        /// <summary>
        /// Get image by resource key.
        /// </summary>
        public static BitmapImage ImageFromStream(Stream stream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            // bitmapImage.Freeze();
            return bitmapImage;
        }

        internal void SetContent(EpubBookRef epubBook)
        {
            this.epubBook = epubBook;
            this.chapterRefs = Flatten(epubBook.GetChapters());
            this.currChapter = 0;
            this.pagePanel.Text = chapterRefs[currChapter].ReadHtmlContent();
            OnPropertyChanged("Chapter");
        }

        private List<EpubChapterRef> Flatten(List<EpubChapterRef> list)
        {
            return list.SelectMany(l => new EpubChapterRef[] { l }.Concat(Flatten(l.SubChapters))).ToList();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.NextChapter();
        }

        private async void NextChapter()
        {
            try
            {
                this.currChapter = Math.Min(this.currChapter + 1, chapterRefs.Count - 1);
                this.pagePanel.Text = await chapterRefs[currChapter].ReadHtmlContentAsync();
                if (chapterRefs[currChapter].Anchor != null)
                {
                    this.pagePanel.ScrollToElement(chapterRefs[currChapter].Anchor);
                }
                OnPropertyChanged("Chapter");
            }
            catch (Exception ex)
            {                
                this.pagePanel.Text = "<div>Invalid chapter.</div>";
                OnPropertyChanged("Chapter");
                Debug.WriteLine(ex);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            this.PrevChapter();
        }

        private async void PrevChapter()
        {
            try
            {
                this.currChapter = Math.Max(this.currChapter - 1, 0);
                this.pagePanel.Text = await chapterRefs[currChapter].ReadHtmlContentAsync();
                if (chapterRefs[currChapter].Anchor != null)
                {
                    this.pagePanel.ScrollToElement(chapterRefs[currChapter].Anchor);
                }
                OnPropertyChanged("Chapter");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                this.pagePanel.Text = "<div>Invalid chapter.</div>";
                OnPropertyChanged("Chapter");
            }
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Left)
            {
                this.PrevChapter();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                this.NextChapter();
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }
    }
}
