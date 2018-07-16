using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    /// <summary>
    /// Logica di interazione per EpubViewerControl.xaml
    /// </summary>
    public partial class EpubViewerControl : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<ChapterChangedEventArgs> ChapterChanged;

        private EpubBookRef epubBook;
        private List<EpubChapterRef> chapterRefs;
        private int currChapter;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Chapter => chapterRefs != null && currChapter >= 0 ? $"{chapterRefs?[currChapter].Title} ({currChapter + 1}/{chapterRefs?.Count})" : "";

        public EpubViewerControl()
        {
            InitializeComponent();

            // design-time only
            Resources.MergedDictionaries.Clear();

            this.DataContext = this;
        }

        internal void SetContent(EpubBookRef epubBook)
        {
            this.epubBook = epubBook;
            this.chapterRefs = Flatten(epubBook.GetChapters());
            this.currChapter = -1;
            this.pagePanel.EpubBook = epubBook;
            this.UpdateChapter();
        }

        private List<EpubChapterRef> Flatten(List<EpubChapterRef> list)
        {
            return list.SelectMany(l => new EpubChapterRef[] { l }.Concat(Flatten(l.SubChapters))).ToList();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.NextChapter();
        }

        private void NextChapter()
        {
            try
            {
                this.currChapter = Math.Min(this.currChapter + 1, chapterRefs.Count - 1);
                this.UpdateChapter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                this.pagePanel.Text = "<div>Invalid chapter.</div>";
            }
            OnPropertyChanged("Chapter");
            OnChapterChanged();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            this.PrevChapter();
        }

        private void PrevChapter()
        {
            try
            {
                this.currChapter = Math.Max(this.currChapter - 1, -1);
                this.UpdateChapter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                this.pagePanel.Text = "<div>Invalid chapter.</div>";                
            }
            OnPropertyChanged("Chapter");
            OnChapterChanged();
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected void OnChapterChanged()
        {
            ChapterChanged?.Invoke(this, new ChapterChangedEventArgs(currChapter));
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

        public class ChapterChangedEventArgs : EventArgs
        {
            public ChapterChangedEventArgs(int currChapter)
            {
                this.NewChapter = currChapter;
            }

            public int NewChapter { get; set; }
        }

        private void UpdateChapter()
        {
            if (currChapter < 0)
            {
                this.pagePanel.ChapterRef = null;
                this.pagePanel.Text = string.Format(@"
                <div style=""margin:4pt; text-align: center;"">
                    <img src=""COVER"" alt=""COVER"" style=""height:500px;""/>
                    <div style=""word-wrap: break-word;"">{0}</div>
                </div>", epubBook.Title);
            }
            else
            {
                this.pagePanel.ChapterRef = chapterRefs[currChapter];
                if (chapterRefs[currChapter].Anchor != null)
                {
                    this.pagePanel.ScrollToElement(chapterRefs[currChapter].Anchor);
                }
            }            
        }
    }
}
