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
    private LrcLine[] _lyricLines;
    private MidiPlayer _midiPlayer;

    private bool _hasVideo;
    private bool _isPlaying;
    private bool _wasPlaying;
    private bool _shouldLoop;
    private bool _useHardwareAcceleration;
    private double _playbackSpeed = 1.0d;

    // Preset playback speeds cycled through by the speed button and the +/- hotkeys.
    private static readonly double[] SpeedPresets = [0.25d, 0.5d, 0.75d, 1.0d, 1.25d, 1.5d, 1.75d, 2.0d];

    // Seek step sizes (in 100ns ticks, same unit as MediaPosition/MediaDuration) used by the
    // timeline hotkeys: Shift+Left/Right for a short seek, Ctrl+Left/Right for a long seek.
    // Plain arrow keys are intentionally left untouched: QuickLook's global hotkey dispatcher
    // already uses them to switch between files in Explorer.
    private static readonly long ShortSeekTicks = TimeSpan.FromSeconds(5).Ticks;
    private static readonly long LongSeekTicks = TimeSpan.FromSeconds(30).Ticks;

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
        buttonSpeed.ToolTip = TranslationHelper.Get("BTN_Speed", translationFile, failsafe: "Playback Speed (+/- to change, 0 to reset)");
        buttonMute.ToolTip = TranslationHelper.Get("BTN_Volume", translationFile, failsafe: "Volume");
        buttonTime.ToolTip = TranslationHelper.Get("BTN_Time", translationFile, failsafe: "Time Elapsed/Remaining");

        buttonPlayPause.Click += TogglePlayPause;
        buttonLoop.Click += ToggleShouldLoop;
        buttonHardwareAcceleration.Click += ToggleHardwareAcceleration;
        buttonSpeed.Click += (_, _) => CycleSpeed(1);
        buttonSpeed.MouseRightButtonUp += (_, e) =>
        {
            PlaybackSpeed = 1.0d;
            e.Handled = true;
        };
        buttonTime.Click += (_, _) => buttonTime.Tag = (string)buttonTime.Tag == "Time" ? "Length" : "Time";
        buttonMute.Click += (_, _) => volumeSliderLayer.Visibility = Visibility.Visible;
        volumeSliderLayer.MouseDown += (_, _) => volumeSliderLayer.Visibility = Visibility.Collapsed;

        sliderProgress.PreviewMouseDown += (_, e) =>
        {
            _wasPlaying = mediaElement.IsPlaying;
            mediaElement.Pause();
        };
        sliderProgress.PreviewMouseUp += (_, _) =>
        {
            if (_wasPlaying) mediaElement.Play();
        };

        PreviewMouseWheel += (_, e) => ChangeVolume(e.Delta / 120d * 0.04d);

        // Keyboard hotkeys for seeking the timeline and changing playback speed.
        // Keep keyboard focus on the panel itself: every child control in this panel
        // (buttons, sliders) is Focusable="False" by design, so focus otherwise stays
        // on the host window and PreviewKeyDown here would not fire on it.
        Focusable = true;
        Loaded += (_, _) => Focus();
        PreviewKeyDown += ViewerPanel_PreviewKeyDown;
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

    /// <summary>
    /// The current playback speed multiplier (1.0 = normal speed). Backed by the
    /// underlying <see cref="mediaElement"/>'s DirectShow SpeedRatio (IMediaSeeking.SetRate).
    /// </summary>
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            var clamped = Math.Max(SpeedPresets[0], Math.Min(SpeedPresets[SpeedPresets.Length - 1], value));
            if (Math.Abs(clamped - _playbackSpeed) < 0.0001d) return;

            _playbackSpeed = clamped;

            if (mediaElement != null)
                mediaElement.SpeedRatio = _playbackSpeed;

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
        SettingHelper.Set("PlaybackSpeed", PlaybackSpeed, "QuickLook.Plugin.VideoViewer");

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
        // Make sure the panel (not the window) owns keyboard focus, so that the
        // seek/speed hotkeys keep working after the user interacts with the mouse.
        Focus();

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var wnd = Window.GetWindow(this);
            // Do not allow dragging when window is borderless (e.g. fullscreen)
            if (wnd?.WindowStyle == WindowStyle.None)
                return;

            wnd?.DragMove();
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts for timeline seeking and playback speed.
    ///
    /// Plain Left/Right/Up/Down are deliberately NOT used here: QuickLook's global
    /// low-level keyboard hook (see QuickLook.KeystrokeDispatcher) already reuses those
    /// keys system-wide to switch between files in Explorer, and it never marks the
    /// keystroke as handled, so it would still reach this handler too. Using modifier
    /// combinations avoids fighting over the same keys:
    ///   Shift+Left / Shift+Right  - seek 5 seconds backward/forward
    ///   Ctrl+Left  / Ctrl+Right   - seek 30 seconds backward/forward
    ///   Home / End                - jump to the start/end of the media
    ///   +/-  (OemPlus/OemMinus)   - increase/decrease playback speed
    ///   0    (D0/NumPad0)         - reset playback speed to 1.0x
    /// </summary>
    private void ViewerPanel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (mediaElement?.Source == null)
            return;

        var modifiers = Keyboard.Modifiers;

        switch (e.Key)
        {
            case Key.Left when modifiers == ModifierKeys.Shift:
                Seek(-ShortSeekTicks);
                e.Handled = true;
                break;

            case Key.Right when modifiers == ModifierKeys.Shift:
                Seek(ShortSeekTicks);
                e.Handled = true;
                break;

            case Key.Left when modifiers == ModifierKeys.Control:
                Seek(-LongSeekTicks);
                e.Handled = true;
                break;

            case Key.Right when modifiers == ModifierKeys.Control:
                Seek(LongSeekTicks);
                e.Handled = true;
                break;

            case Key.Home when modifiers == ModifierKeys.None:
                SeekTo(0L);
                e.Handled = true;
                break;

            case Key.End when modifiers == ModifierKeys.None:
                SeekTo(mediaElement.MediaDuration);
                e.Handled = true;
                break;

            case Key.OemPlus or Key.Add when modifiers == ModifierKeys.None:
                CycleSpeed(1);
                e.Handled = true;
                break;

            case Key.OemMinus or Key.Subtract when modifiers == ModifierKeys.None:
                CycleSpeed(-1);
                e.Handled = true;
                break;

            case Key.D0 or Key.NumPad0 when modifiers == ModifierKeys.None:
                PlaybackSpeed = 1.0d;
                e.Handled = true;
                break;
        }
    }

    /// <summary>Seeks the timeline by a relative amount of 100ns ticks (same unit as MediaPosition).</summary>
    private void Seek(long deltaTicks)
    {
        if (mediaElement == null)
            return;

        SeekTo(mediaElement.MediaPosition + deltaTicks);
    }

    /// <summary>Seeks the timeline to an absolute position, clamped to the media's duration.</summary>
    private void SeekTo(long positionTicks)
    {
        if (mediaElement == null)
            return;

        var duration = mediaElement.MediaDuration;
        var clamped = duration > 0
            ? Math.Max(0L, Math.Min(duration, positionTicks))
            : Math.Max(0L, positionTicks);

        mediaElement.MediaPosition = clamped;
    }

    /// <summary>Moves the playback speed to the next/previous preset in <see cref="SpeedPresets"/>.</summary>
    private void CycleSpeed(int direction)
    {
        var index = 3; // default: land on 1.0x if the current speed isn't an exact preset match
        var smallestDelta = double.MaxValue;
        for (var i = 0; i < SpeedPresets.Length; i++)
        {
            var delta = Math.Abs(SpeedPresets[i] - PlaybackSpeed);
            if (delta < smallestDelta)
            {
                smallestDelta = delta;
                index = i;
            }
        }

        index = Math.Max(0, Math.Min(SpeedPresets.Length - 1, index + direction));
        PlaybackSpeed = SpeedPresets[index];
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
        if (mediaElement == null)
            return;

        mediaElement.MediaPosition = 0L;
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
                    var lyric = LrcHelper.GetNearestLrc(_lyricLines, new TimeSpan(mediaElement.MediaPosition));
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
        get => mediaElement.Volume;
        set
        {
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
        if (mediaElement.IsPlaying)
            mediaElement.Pause();
        else
            mediaElement.Play();
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

    public void LoadAndPlay(string path, MediaInfoNative info)
    {
        // Detect whether it is other playback formats
        if (!HasVideo)
        {
            string audioCodec = info?.Get(StreamKind.Audio, 0, "Format");

            if (audioCodec?.Equals("MIDI", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                _midiPlayer = new MidiPlayer(this, _context);
                _midiPlayer.LoadAndPlay(path);
                return; // Midi player will handle the playback at all
            }
        }

        UpdateMeta(path, info);

        // detect rotation
        _ = double.TryParse(info?.Get(StreamKind.Video, 0, "Rotation"), out var rotation);
        // Correct rotation: on some machine the value "90" becomes "90000" by some reason
        if (rotation > 360d)
            rotation /= 1e3;
        if (Math.Abs(rotation) > 0.1d)
            mediaElement.LayoutTransform = new RotateTransform(rotation, 0.5d, 0.5d);

        mediaElement.Source = new Uri(path);
        // old plugin use an int-typed "Volume" config key ranged from 0 to 100. Let's use a new one here.
        LinearVolume = Math.Max(0d, Math.Min(1d, SettingHelper.Get("VolumeDouble", 1d, "QuickLook.Plugin.VideoViewer")));
        PlaybackSpeed = SettingHelper.Get("PlaybackSpeed", 1d, "QuickLook.Plugin.VideoViewer");

        mediaElement.Play();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
