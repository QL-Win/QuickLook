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

using QuickLook.Common.Annotations;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.MediaInfo;
using QuickLook.MediaInfo.Core;
using QuickLook.Plugin.VideoViewer.AudioTrack;
using QuickLook.Plugin.VideoViewer.LyricTrack;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UtfUnknown;
using WPFMediaKit.DirectShow.Controls;
using WPFMediaKit.DirectShow.MediaPlayers;

namespace QuickLook.Plugin.VideoViewer;

public partial class ViewerPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    private readonly ContextObject _context;
    private BitmapSource _coverArt;
    private DispatcherTimer _lyricTimer;
    private DispatcherTimer _positionTimer;
    private LrcLine[] _lyricLines;
    private MidiPlayer _midiPlayer;
    private AudioFilePlayer _audioPlayer;

    private bool _hasVideo;
    private bool _isPlaying;
    private bool _wasPlaying;
    private bool _shouldLoop;
    private bool _useHardwareAcceleration;
    private bool _isSeeking;
    private bool _updatingPosition;
    private bool _audioPlayerStarting;
    private bool _audioOnly;
    private long _mediaPosition;
    private long _mediaDuration;

    public ViewerPanel(ContextObject context)
    {
        InitializeComponent();
        LoadAndInsertGlassLayer();

        // apply global theme
        Resources.MergedDictionaries[0].MergedDictionaries.Clear();

        _context = context;

        mediaElement.MediaUriPlayer.LAVFilterDirectory =
            IntPtr.Size == 8 ? @"LAVFilters-x64\" : @"LAVFilters-x86\";

        //ShowViedoControlContainer(null, null);
        viewerPanel.PreviewMouseMove += ShowViedoControlContainer;

        mediaElement.MediaUriPlayer.PlayerStateChanged += PlayerStateChanged;
        mediaElement.MediaOpened += MediaOpened;
        mediaElement.MediaEnded += MediaEnded;
        mediaElement.MediaFailed += MediaFailed;

        ShouldLoop = SettingHelper.Get("ShouldLoop", false, "QuickLook.Plugin.VideoViewer");
        UseHardwareAcceleration = SettingHelper.Get("UseHardwareAcceleration", false, "QuickLook.Plugin.VideoViewer");

        // Apply persisted HW/SW mode to the underlying player if supported.
        HardwareAccelerationModeChanged(UseHardwareAcceleration);

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        buttonPlayPause.ToolTip = TranslationHelper.Get("BTN_PlayPause", translationFile, failsafe: "Play/Pause");
        buttonLoop.ToolTip = TranslationHelper.Get("BTN_Loop", translationFile, failsafe: "Loop");
        buttonHardwareAcceleration.ToolTip = TranslationHelper.Get("BTN_HardwareAcceleration", translationFile, failsafe: "Hardware/Software Decoding");
        buttonMute.ToolTip = TranslationHelper.Get("BTN_Volume", translationFile, failsafe: "Volume");
        buttonTime.ToolTip = TranslationHelper.Get("BTN_Time", translationFile, failsafe: "Time Elapsed/Remaining");

        buttonPlayPause.Click += TogglePlayPause;
        buttonLoop.Click += ToggleShouldLoop;
        buttonHardwareAcceleration.Click += ToggleHardwareAcceleration;
        buttonTime.Click += (_, _) => buttonTime.Tag = (string)buttonTime.Tag == "Time" ? "Length" : "Time";
        buttonMute.Click += (_, _) => volumeSliderLayer.Visibility = Visibility.Visible;
        volumeSliderLayer.MouseDown += (_, _) => volumeSliderLayer.Visibility = Visibility.Collapsed;

        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _positionTimer.Tick += (_, _) => UpdatePlaybackState();
        _positionTimer.Start();

        sliderProgress.PreviewMouseDown += (_, e) =>
        {
            _isSeeking = true;
            _wasPlaying = IsPlaying;
            PausePlayback();
        };
        sliderProgress.PreviewMouseUp += (_, _) =>
        {
            _isSeeking = false;
            if (_wasPlaying) ResumePlayback();
        };

        PreviewMouseWheel += (_, e) => ChangeVolume(e.Delta / 120d * 0.04d);
    }

    private partial void LoadAndInsertGlassLayer();

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
            if (_audioPlayer != null)
                _audioPlayer.Loop = value;
            else if (value)
                TryStartAudioPlayer();
            OnPropertyChanged();
        }
    }

    public long MediaPosition
    {
        get => _mediaPosition;
        set
        {
            if (value == _mediaPosition) return;
            _mediaPosition = value;
            OnPropertyChanged();

            if (!_updatingPosition)
                Seek(value);
        }
    }

    public long MediaDuration
    {
        get => _mediaDuration;
        private set
        {
            if (value == _mediaDuration) return;
            _mediaDuration = value;
            OnPropertyChanged();
        }
    }

    public bool UseHardwareAcceleration
    {
        get => _useHardwareAcceleration;
        private set
        {
            if (value == _useHardwareAcceleration) return;
            _useHardwareAcceleration = value;
            OnPropertyChanged();
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
        SettingHelper.Set("VolumeDouble", LinearVolume, "QuickLook.Plugin.VideoViewer");
        SettingHelper.Set("ShouldLoop", ShouldLoop, "QuickLook.Plugin.VideoViewer");
        SettingHelper.Set("UseHardwareAcceleration", UseHardwareAcceleration, "QuickLook.Plugin.VideoViewer");

        _positionTimer?.Stop();
        _positionTimer = null;
        _audioPlayer?.Dispose();
        _audioPlayer = null;

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

        _lyricTimer?.Stop();
        _lyricTimer = null;
        _lyricLines = null;
        _midiPlayer?.Dispose();
        _midiPlayer = null;
    }

    private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var wnd = Window.GetWindow(this);
            // Do not allow dragging when window is borderless (e.g. fullscreen)
            if (wnd?.WindowStyle == WindowStyle.None)
                return;

            wnd?.DragMove();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void MediaOpened(object o, RoutedEventArgs args)
    {
        if (mediaElement == null)
            return;

        HasVideo = mediaElement.HasVideo;
        SetMediaDuration(mediaElement.MediaDuration);

        _context.IsBusy = false;
    }

    private void MediaFailed(object sender, MediaFailedEventArgs e)
    {
        ((MediaUriElement)sender).Dispatcher.BeginInvoke(new Action(() =>
        {
            _context.ViewerContent = new TextBlock()
            {
                Text = e.Exception.ToString(),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _context.IsBusy = false;
        }));
    }

    private void MediaEnded(object sender, RoutedEventArgs e)
    {
        if (mediaElement == null || _audioPlayer != null || _audioPlayerStarting)
            return;

        mediaElement.MediaPosition = 0L;
        SetMediaPosition(0L);
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
        var show = (Storyboard)videoControlContainer.FindResource("ShowControlStoryboard");
        if (videoControlContainer.Opacity == 0d || videoControlContainer.Opacity == 1d)
            show.Begin();
    }

    private void AutoHideViedoControlContainer(object sender, EventArgs e)
    {
        if (!HasVideo)
            return;

        if (videoControlContainer.IsMouseOver)
            return;

        var hide = (Storyboard)videoControlContainer.FindResource("HideControlStoryboard");

        hide.Begin();
    }

    private void PlayerStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (_audioPlayer != null)
            return;

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

    private void UpdatePlaybackState()
    {
        if (_isSeeking)
            return;

        if (_audioPlayer != null)
        {
            SetMediaPosition(_audioPlayer.PositionTicks);
            SetMediaDuration(_audioPlayer.DurationTicks);
            IsPlaying = _audioPlayer.IsPlaying;
        }
        else if (mediaElement != null)
        {
            SetMediaPosition(mediaElement.MediaPosition);
            SetMediaDuration(mediaElement.MediaDuration);
        }
    }

    private void SetMediaPosition(long value)
    {
        if (value == _mediaPosition)
            return;

        _updatingPosition = true;
        MediaPosition = value;
        _updatingPosition = false;
    }

    private void SetMediaDuration(long value)
    {
        if (value > 0)
            MediaDuration = value;
    }

    private void Seek(long position)
    {
        if (_audioPlayer != null)
            _audioPlayer.Seek(position);
        else if (mediaElement?.Source != null)
            mediaElement.MediaPosition = position;
    }

    private void PausePlayback()
    {
        if (_audioPlayer != null)
            _audioPlayer.Pause();
        else
            mediaElement.Pause();
    }

    private void ResumePlayback()
    {
        if (_audioPlayer != null)
            _audioPlayer.Play();
        else
            mediaElement.Play();
    }

    private void AudioPlaybackEnded(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_audioPlayer == null)
                return;

            SetMediaPosition(0L);
            IsPlaying = false;
        }));
    }

    private void TryStartAudioPlayer()
    {
        if (!_audioOnly || _audioPlayer != null || _midiPlayer != null || HasVideo || mediaElement?.Source == null)
            return;

        _audioPlayerStarting = true;
        bool wasPlaying = false;
        long position = 0L;

        try
        {
            position = mediaElement.MediaDuration > 0 ? mediaElement.MediaPosition : _mediaPosition;
            wasPlaying = mediaElement.IsPlaying;

            mediaElement.Stop();

            double volume = Math.Max(0d, Math.Min(1d, SettingHelper.Get("VolumeDouble", 1d, "QuickLook.Plugin.VideoViewer")));
            var audioPlayer = AudioFilePlayer.TryCreate(mediaElement.Source.LocalPath, true, volume);

            if (audioPlayer == null)
            {
                mediaElement.MediaPosition = position;
                if (wasPlaying)
                    mediaElement.Play();
                else
                    mediaElement.Pause();
                return;
            }

            _audioPlayer = audioPlayer;
            _audioPlayer.PlaybackEnded += AudioPlaybackEnded;
            _audioPlayer.Seek(position);
            SetMediaDuration(_audioPlayer.DurationTicks);
            SetMediaPosition(position);

            if (wasPlaying)
            {
                var startTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                startTimer.Tick += (_, _) =>
                {
                    startTimer.Stop();

                    if (_audioPlayer != audioPlayer)
                        return;

                    audioPlayer.Seek(position);
                    audioPlayer.Play();
                    IsPlaying = true;
                };
                startTimer.Start();
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            mediaElement.MediaPosition = position;

            if (wasPlaying)
                mediaElement.Play();
            else
                mediaElement.Pause();
        }
        finally
        {
            _audioPlayerStarting = false;
        }
    }

    private void UpdateMeta(string path, MediaInfoNative info)
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

            // Extract cover art
            var coverData = info.Get(StreamKind.General, 0, "Cover_Data");
            var coverBytes = CoverDataExtractor.Extract(coverData);
            CoverArt = CoverDataExtractor.Extract(coverBytes);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            metaTitle.Text = Path.GetFileName(path);
            metaArtists.Text = metaAlbum.Text = string.Empty;
        }

        metaArtists.Visibility = string.IsNullOrEmpty(metaArtists.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;
        metaAlbum.Visibility = string.IsNullOrEmpty(metaAlbum.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

        var lyricPath = Path.ChangeExtension(path, ".lrc");

        // Stop previous timer if any.
        _lyricTimer?.Stop();
        _lyricTimer = null;
        _lyricLines = null;

        if (File.Exists(lyricPath))
        {
            var buffer = File.ReadAllBytes(lyricPath);
            var encoding = CharsetDetector.DetectFromBytes(buffer).Detected?.Encoding ?? Encoding.Default;

            _lyricLines = [.. LrcHelper.ParseText(encoding.GetString(buffer))];
        }
        else
        {
            // Use embedded lyrics from MediaInfo if present.
            // Common tag: General/Lyrics (may contain LRC formatted content).
            var embeddedLyrics = info?.Get(StreamKind.General, 0, "Lyrics");

            // Only check whether the tag of lyrics is present by MediaInfo
            if (!string.IsNullOrWhiteSpace(embeddedLyrics))
            {
                var file = TagLib.File.Create(path);
                embeddedLyrics = file.Tag.Lyrics;

                // Check whether the tag of lyrics is present by TagLib#
                if (!string.IsNullOrWhiteSpace(embeddedLyrics))
                {
                    _lyricLines = [.. LrcHelper.ParseText(embeddedLyrics)];
                }
            }
        }

        if (_lyricLines != null && _lyricLines.Length != 0)
        {
            _lyricTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _lyricTimer.Tick += (sender, e) =>
            {
                if (_lyricLines != null && _lyricLines.Length != 0)
                {
                    var lyric = LrcHelper.GetNearestLrc(_lyricLines, new TimeSpan(MediaPosition));
                    metaLyric.Text = lyric?.LrcText?.Trim();
                }
                else
                {
                    metaLyric.Text = null;
                    metaLyric.Visibility = Visibility.Collapsed;
                }
            };
            _lyricTimer.Start();

            metaLyric.Visibility = Visibility.Visible;
        }
        else
        {
            metaLyric.Visibility = Visibility.Collapsed;
        }
    }

    public double LinearVolume
    {
        get => _audioPlayer?.Volume ?? mediaElement?.Volume ?? 0d;
        set
        {
            if (_audioPlayer != null)
                _audioPlayer.Volume = value;
            else if (mediaElement != null)
                mediaElement.Volume = value;

            OnPropertyChanged();
        }
    }

    private void ChangeVolume(double delta)
    {
        LinearVolume = Math.Max(0d, Math.Min(1d, LinearVolume + delta));
    }

    private void TogglePlayPause(object sender, EventArgs e)
    {
        if (_audioPlayer != null)
        {
            if (_audioPlayer.IsPlaying)
                _audioPlayer.Pause();
            else
                _audioPlayer.Play();
        }
        else if (mediaElement.IsPlaying)
        {
            mediaElement.Pause();
        }
        else
        {
            mediaElement.Play();
        }
    }

    private void ToggleShouldLoop(object sender, EventArgs e)
    {
        ShouldLoop = !ShouldLoop;
    }

    private void ToggleHardwareAcceleration(object sender, EventArgs e)
    {
        UseHardwareAcceleration = !UseHardwareAcceleration;
        SettingHelper.Set("UseHardwareAcceleration", UseHardwareAcceleration, "QuickLook.Plugin.VideoViewer");
        HardwareAccelerationModeChanged(UseHardwareAcceleration);
    }

    private void HardwareAccelerationModeChanged(bool enable)
    {
        try
        {
            var player = mediaElement?.MediaUriPlayer;
            if (player == null) return;

            if (mediaElement.Source == null)
            {
                // No source loaded yet – just store the flag for the next Open
                player.Dispatcher.BeginInvoke(() =>
                    player.EnableLAVHardwareAcceleration = enable);
                return;
            }

            // Dispatch to the player's own MTA thread.
            // ApplyHardwareAcceleration will call OpenSource() there, which
            // rebuilds the full graph (incl. EVR/VMR9 allocator) so that
            // NewAllocatorSurface fires and the WPF back buffer is refreshed.
            // Position + play state are restored inside ApplyHardwareAcceleration
            // via a MediaOpened callback.
            player.Dispatcher.BeginInvoke(() =>
                player.ApplyHardwareAcceleration(enable));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    /// <summary>
    /// Loads media for playback and applies video rotation from its metadata.
    /// </summary>
    public void LoadAndPlay(string path, MediaInfoNative info)
    {
        bool hasVideo = !string.IsNullOrWhiteSpace(info?.Get(StreamKind.Video, 0, "Format"));
        string audioCodec = info?.Get(StreamKind.Audio, 0, "Format");
        bool hasAudio = !string.IsNullOrWhiteSpace(audioCodec);
        _audioOnly = false;

        // Detect whether it is other playback formats
        if (!hasVideo)
        {
            if (audioCodec?.Equals("MIDI", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                _midiPlayer = new MidiPlayer(this, _context);
                _midiPlayer.LoadAndPlay(path);
                return; // Midi player will handle the playback at all
            }

            _audioOnly = hasAudio;
        }

        UpdateMeta(path, info);

        if (_audioOnly && ShouldLoop)
        {
            double volume = Math.Max(0d, Math.Min(1d, SettingHelper.Get("VolumeDouble", 1d, "QuickLook.Plugin.VideoViewer")));
            var audioPlayer = AudioFilePlayer.TryCreate(path, ShouldLoop, volume);

            if (audioPlayer != null)
            {
                _audioPlayer = audioPlayer;
                _audioPlayer.PlaybackEnded += AudioPlaybackEnded;
                SetMediaDuration(_audioPlayer.DurationTicks);
                SetMediaPosition(0L);
                IsPlaying = true;
                _context.IsBusy = false;
                _audioPlayer.Play();
                return;
            }
        }

        // detect rotation
        _ = double.TryParse(info?.Get(StreamKind.Video, 0, "Rotation"), NumberStyles.Float, CultureInfo.InvariantCulture, out var rotation);
        if (Math.Abs(rotation) > 0.1d)
            mediaElement.LayoutTransform = new RotateTransform(rotation, 0.5d, 0.5d);

        mediaElement.Source = new Uri(path);
        // old plugin use an int-typed "Volume" config key ranged from 0 to 100. Let's use a new one here.
        LinearVolume = Math.Max(0d, Math.Min(1d, SettingHelper.Get("VolumeDouble", 1d, "QuickLook.Plugin.VideoViewer")));

        mediaElement.Play();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
