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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using QuickLook.Annotations;
using QuickLook.ExtensionMethods;
using TagLib;
using File = TagLib.File;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable, INotifyPropertyChanged
    {
        private readonly ContextObject _context;

        private BitmapSource _coverArt;
        private bool _wasPlaying;

        public ViewerPanel(ContextObject context, bool hasVideo)
        {
            ShowVideo = hasVideo;

            InitializeComponent();

            // apply global theme
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();

            ShowViedoControlContainer(null, null);
            viewerPanel.PreviewMouseMove += ShowViedoControlContainer;

            //mediaElement.PropertyChanged += PlayerPropertyChanged;
            //mediaElement.StateChanged += PlayerStateChanged;

            _context = context;

            buttonPlayPause.Click += TogglePlayPause;
            buttonTime.Click += (sender, e) => buttonTime.Tag = (string) buttonTime.Tag == "Time" ? "Length" : "Time";
            buttonMute.Click += (sender, e) => volumeSliderLayer.Visibility = Visibility.Visible;
            volumeSliderLayer.MouseDown += (sender, e) => volumeSliderLayer.Visibility = Visibility.Collapsed;

            sliderProgress.PreviewMouseDown += (sender, e) =>
            {
                _wasPlaying = mediaElement.IsPlaying;
                mediaElement.Pause();
            };
            sliderProgress.PreviewMouseUp += (sender, e) =>
            {
                if (_wasPlaying) mediaElement.Play();
            };
            mediaElement.MediaFailed += ShowErrorNotification;
            mediaElement.MediaOpening += (sender, e) => e.Options.EnableHardwareAcceleration = true;
            /*mediaElement.MediaEnded += (s, e) =>
            {
                if (mediaElement.IsOpen)
                    if (!mediaElement.NaturalDuration.HasTimeSpan)
                    {
                        mediaElement.Stop();
                        mediaElement.Play();
                    }
            };*/

            PreviewMouseWheel += (sender, e) => ChangeVolume((double) e.Delta / 120 * 0.05);
        }

        public bool ShowVideo { get; private set; }

        public BitmapSource CoverArt
        {
            get => _coverArt;
            private set
            {
                if (Equals(value, _coverArt)) return;
                if (value == null) return;
                _coverArt = value;
                OnPropertyChanged();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            try
            {
                CoverArt = null;
                mediaElement?.Dispose();
                mediaElement = null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ShowViedoControlContainer(object sender, MouseEventArgs e)
        {
            var show = (Storyboard) videoControlContainer.FindResource("ShowControlStoryboard");
            if (videoControlContainer.Opacity == 0 || videoControlContainer.Opacity == 1)
                show.Begin();
        }

        private void AutoHideViedoControlContainer(object sender, EventArgs e)
        {
            if (!ShowVideo)
                return;

            if (videoControlContainer.IsMouseOver)
                return;

            var hide = (Storyboard) videoControlContainer.FindResource("HideControlStoryboard");

            hide.Begin();
        }

        private void UpdateMeta(string path)
        {
            if (ShowVideo)
                return;

            using (var h = File.Create(path))
            {
                metaTitle.Text = h.Tag.Title;
                metaArtists.Text = h.Tag.FirstPerformer;
                metaAlbum.Text = h.Tag.Album;

                //var cs = h.Tag.Pictures.FirstOrDefault(p => p.Type == TagLib.PictureType.FrontCover);
                var cs = h.Tag.Pictures.FirstOrDefault();
                if (cs != default(IPicture))
                    using (var ms = new MemoryStream(cs.Data.Data))
                    {
                        CoverArt = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        DetermineTheme();
                    }
            }
            metaArtists.Visibility = string.IsNullOrEmpty(metaArtists.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            metaAlbum.Visibility = string.IsNullOrEmpty(metaAlbum.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void DetermineTheme()
        {
            if (ShowVideo)
                return;

            if (CoverArt == null)
                return;

            bool dark;
            using (var b = CoverArt.ToBitmap())
            {
                dark = b.IsDarkImage();
            }

            _context.UseDarkTheme = dark;
        }

        private void ChangeVolume(double delta)
        {
            mediaElement.IsMuted = false;

            mediaElement.Volume += delta;
        }

        private void TogglePlayPause(object sender, EventArgs e)
        {
            if (mediaElement.IsPlaying)
            {
                mediaElement.Pause();
            }
            else
            {
                if (mediaElement.HasMediaEnded)
                    mediaElement.Stop();
                mediaElement.Play();
            }
        }

        [DebuggerNonUserCode]
        private void ShowErrorNotification(object sender, EventArgs e)
        {
            _context.ShowNotification("", "An error occurred while loading the video.");
            mediaElement?.Stop();

            Dispose();


            //throw new Exception("fallback to default viewer.");
        }

        public void LoadAndPlay(string path)
        {
            UpdateMeta(path);

            mediaElement.Source = new Uri(path);
            mediaElement.Volume = 0.5;

            mediaElement.Play();
        }

        ~ViewerPanel()
        {
            Dispose();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}