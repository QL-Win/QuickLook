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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MediaInfo;
using QuickLook.Common.Annotations;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using WPFMediaKit.DirectShow.Controls;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable, INotifyPropertyChanged
    {
        private readonly ContextObject _context;
        private BitmapSource _coverArt;
        
        private bool _hasVideo;
        private bool _isPlaying;
        private bool _wasPlaying;
        private bool _shouldLoop;

        public ViewerPanel(ContextObject context)
        {
            InitializeComponent();

            // apply global theme
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();

            _context = context;

            mediaElement.MediaUriPlayer.LAVFilterDirectory =
                IntPtr.Size == 8 ? "LAVFilters-0.72-x64\\" : "LAVFilters-0.72-x86\\";

            //ShowViedoControlContainer(null, null);
            viewerPanel.PreviewMouseMove += ShowViedoControlContainer;

            mediaElement.MediaUriPlayer.PlayerStateChanged += PlayerStateChanged;
            mediaElement.MediaOpened += MediaOpened;
            mediaElement.MediaEnded += MediaEnded;
            mediaElement.MediaFailed += MediaFailed;

            ShouldLoop = SettingHelper.Get("ShouldLoop", false);

            buttonPlayPause.Click += TogglePlayPause;
            buttonLoop.Click += ToggleShouldLoop;
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

            PreviewMouseWheel += (sender, e) => ChangeVolume((double) e.Delta / 120 * 0.04);
        }

        public bool HasVideo
        {
            get => _hasVideo;
            private set
            {
                if (value == _hasVideo) return;
                _hasVideo = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (value == _isPlaying) return;
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public bool ShouldLoop
        {
            get => _shouldLoop;
            private set
            {
                if (value == _shouldLoop) return;
                _shouldLoop = value;
                OnPropertyChanged();
                if (!IsPlaying)
                {
                    IsPlaying = true;

                    mediaElement.Play();
                }
            }
        }

        public BitmapSource CoverArt
        {
            get => _coverArt;
            private set
            {
                if (ReferenceEquals(value, _coverArt)) return;
                if (value == null) return;
                _coverArt = value;
                OnPropertyChanged();
            }
        }

        public void Dispose()
        {
            // old plugin use an int-typed "Volume" config key ranged from 0 to 100. Let's use a new one here.
            SettingHelper.Set("VolumeDouble", LinearVolume);
            SettingHelper.Set("ShouldLoop", ShouldLoop);

            try
            {
                mediaElement?.Close();

                Task.Run(() =>
                {
                    mediaElement?.MediaUriPlayer.Dispose();
                    mediaElement = null;
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void MediaOpened(object o, RoutedEventArgs args)
        {
            if (mediaElement == null)
                return;

            HasVideo = mediaElement.HasVideo;

            _context.IsBusy = false;
        }

        private void MediaFailed(object sender, MediaFailedEventArgs e)
        {
            ((MediaUriElement) sender).Dispatcher.BeginInvoke(new Action(() =>
            {
                _context.ViewerContent =
                    new Label {Content = e.Exception, VerticalAlignment = VerticalAlignment.Center};
                _context.IsBusy = false;
            }));
        }

        private void MediaEnded(object sender, RoutedEventArgs e)
        {
            if (mediaElement == null)
                return;

            mediaElement.MediaPosition = 0;
            if (ShouldLoop)
            {
                IsPlaying = true;

                mediaElement.Play();
            }
            else
            {
                IsPlaying = false;
                
                mediaElement.Pause();
            }
        }

        private void ShowViedoControlContainer(object sender, MouseEventArgs e)
        {
            var show = (Storyboard) videoControlContainer.FindResource("ShowControlStoryboard");
            if (videoControlContainer.Opacity == 0 || videoControlContainer.Opacity == 1)
                show.Begin();
        }

        private void AutoHideViedoControlContainer(object sender, EventArgs e)
        {
            if (!HasVideo)
                return;

            if (videoControlContainer.IsMouseOver)
                return;

            var hide = (Storyboard) videoControlContainer.FindResource("HideControlStoryboard");

            hide.Begin();
        }

        private void PlayerStateChanged(PlayerState oldState, PlayerState newState)
        {
            switch (newState)
            {
                case PlayerState.Playing:
                    IsPlaying = true;
                    break;
                case PlayerState.Paused:
                case PlayerState.Stopped:
                case PlayerState.Closed:
                    IsPlaying = false;
                    break;
            }
        }

        private void UpdateMeta(string path, MediaInfo.MediaInfo info)
        {
            if (HasVideo)
                return;

            try
            {
                if (info == null)
                    throw new NullReferenceException();

                var title = info.Get(StreamKind.General, 0, "Title");
                var artist = info.Get(StreamKind.General, 0, "Performer");
                var album = info.Get(StreamKind.General, 0, "Album");

                metaTitle.Text = !string.IsNullOrWhiteSpace(title) ? title : Path.GetFileName(path);
                metaArtists.Text = artist;
                metaAlbum.Text = album;

                var cs = info.Get(StreamKind.General, 0, "Cover_Data");
                if (!string.IsNullOrEmpty(cs))
                    using (var ms = new MemoryStream(Convert.FromBase64String(cs)))
                    {
                        CoverArt = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
            }
            catch (Exception)
            {
                metaTitle.Text = Path.GetFileName(path);
                metaArtists.Text = metaAlbum.Text = string.Empty;
            }

            metaArtists.Visibility = string.IsNullOrEmpty(metaArtists.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            metaAlbum.Visibility = string.IsNullOrEmpty(metaAlbum.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // Newer .net has Math.Clamp
        private T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        // A change in amplitude by a factor of 10 corresponds to a 20 dB change
        private const double DecibelAmplitudeMult = 20.0;
        public double LinearVolume
        {
            // mediaElement.Volume returns [0,1] where 0 = -100db, 1 = 0db
            // Decibel is logarithmic. See amplitude table https://en.wikipedia.org/wiki/Decibel
            get
            {
                var dbVol = 100.0 * (mediaElement.Volume - 1.0);
                var linearVol = Math.Pow(10, dbVol / DecibelAmplitudeMult);
                return linearVol;
            }
            set
            {
                var linearVol = Clamp(value, 0.00001, 1.0);
                var dbVol = DecibelAmplitudeMult * Math.Log10(linearVol);
                mediaElement.Volume = dbVol / 100.0 + 1.0;
                OnPropertyChanged();
            }
        }

        private void ChangeVolume(double delta)
        {
            LinearVolume = LinearVolume + delta; // setter will clamp
        }

        private void TogglePlayPause(object sender, EventArgs e)
        {
            if (mediaElement.IsPlaying)
                mediaElement.Pause();
            else
                mediaElement.Play();
        }

        private void ToggleShouldLoop(object sender, EventArgs e)
        {
            ShouldLoop = !ShouldLoop;
        }

        public void LoadAndPlay(string path, MediaInfo.MediaInfo info)
        {
            UpdateMeta(path, info);
            
            // detect rotation
            double.TryParse(info?.Get(StreamKind.Video, 0, "Rotation"), out var rotation);
            if (Math.Abs(rotation) > 0.1)
                mediaElement.LayoutTransform = new RotateTransform(rotation, 0.5, 0.5);

            mediaElement.Source = new Uri(path);
            // old plugin use an int-typed "Volume" config key ranged from 0 to 100. Let's use a new one here.
            LinearVolume = SettingHelper.Get("VolumeDouble", 1d);

            mediaElement.Play();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
