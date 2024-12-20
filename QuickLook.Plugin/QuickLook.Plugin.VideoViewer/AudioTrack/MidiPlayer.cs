// Copyright © 2024 QL-Win Contributors
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

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using QuickLook.Common.Annotations;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace QuickLook.Plugin.VideoViewer.AudioTrack;

internal class MidiPlayer : IDisposable, INotifyPropertyChanged
{
    private ViewerPanel _vp;
    private ContextObject _context;
    private MidiFile _midiFile;
    private OutputDevice _outputDevice;
    private Playback _playback;
    private TimeSpan _duration;
    private MethodInfo _setShouldLoop; // Reflection to invoke `_vp.set_ShouldLoop()`

    private long _currentTicks = 0L;

    public long CurrentTicks
    {
        get => _currentTicks;
        private set
        {
            if (value == _currentTicks) return;
            _currentTicks = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public MidiPlayer(ViewerPanel panle, ContextObject context)
    {
        _vp = panle;
        _context = context;
    }

    public void Dispose()
    {
        _vp = null;
        _context = null;
        _playback?.Stop();
        _playback?.Dispose();
        _playback = null;
        _outputDevice?.Dispose();
        _outputDevice = null;
    }

    public void LoadAndPlay(string path, MediaInfoLib info)
    {
        _midiFile = MidiFile.Read(path);
        _vp.metaTitle.Text = Path.GetFileName(path);
        _vp.metaArtists.Text = _midiFile.OriginalFormat.ToString();
        _vp.metaAlbum.Text = _midiFile.TimeDivision.ToString();

        _outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
        _playback = _midiFile.GetPlayback(_outputDevice);

        if (_playback.GetDuration(TimeSpanType.Metric) is MetricTimeSpan metricTimeSpan)
        {
            _duration = TimeSpan.FromMilliseconds(metricTimeSpan.TotalMilliseconds);
            var durationString = new TimeTickToShortStringConverter().Convert(_duration.Ticks, typeof(string), null, CultureInfo.InvariantCulture).ToString();

            if (_vp.buttonTime.Content is TextBlock timeText)
            {
                timeText.Text = "00:00";
                _vp.metaLength.Text = durationString;
                _vp.sliderProgress.IsSelectionRangeEnabled = false;
                _vp.sliderProgress.SelectionEnd = 0L; // Unbinding
                _vp.sliderProgress.Value = 0L; // Unbinding
                _vp.sliderProgress.Maximum = _duration.Ticks; // Unbinding
                _vp.sliderProgress.SetBinding(Slider.ValueProperty, new Binding(nameof(CurrentTicks)) // Rebinding
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });
            }
        }

        _vp.buttonPlayPause.Click += (_, _) =>
        {
            if (_playback.IsRunning)
            {
                _playback.Stop();
                _vp.buttonPlayPause.Content = FontSymbols.Play;
            }
            else
            {
                _playback.Start();
                _vp.buttonPlayPause.Content = FontSymbols.Pause;
            }
        };

        if (_vp.GetType().GetProperty("ShouldLoop", BindingFlags.Instance | BindingFlags.Public) is PropertyInfo propertyShouldLoop)
        {
            _setShouldLoop = propertyShouldLoop.GetSetMethod(nonPublic: true);
            _setShouldLoop.Invoke(_vp, [SettingHelper.Get("ShouldLoop", false, "QuickLook.Plugin.VideoViewer")]);
        }

        _vp.buttonLoop.Click += (_, _) =>
        {
            _setShouldLoop.Invoke(_vp, [!_vp.ShouldLoop]);
            _playback.Loop = _vp.ShouldLoop;
        };

        // Event Slider.PreviewMouseDown will be prevented from being handled by the slider itself
        // So we should add a handler by ourself
        _vp.sliderProgress.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler((_, e) =>
        {
            _playback?.Stop();

            Point mousePosition = e.GetPosition(_vp.sliderProgress);
            double newValue = mousePosition.X / _vp.sliderProgress.ActualWidth * (_vp.sliderProgress.Maximum - _vp.sliderProgress.Minimum) + _vp.sliderProgress.Minimum;
            double seekPercent = newValue / _duration.Ticks;
            long moveTime = (long)(_duration.Ticks * seekPercent);
            TimeSpan timeSpan = TimeSpan.FromTicks(moveTime);

            _playback?.MoveToTime(new MetricTimeSpan(timeSpan));
            CurrentTicks = timeSpan.Ticks;

            _playback?.Start();
            _vp.buttonPlayPause.Content = FontSymbols.Pause;
        }), true);

        // Disable unsupported functionality
        {
            _vp.buttonMute.IsEnabled = false;
            _vp.buttonMute.Opacity = 0.5d;
        }

        _playback.Loop = _vp.ShouldLoop;
        _playback.EventPlayed += (_, _) =>
        {
            _vp?.Dispatcher.Invoke(() =>
            {
                if ((string)_vp?.buttonTime?.Tag == "Time")
                {
                    if (_playback?.GetCurrentTime(TimeSpanType.Metric) is MetricTimeSpan metricTimeSpan)
                    {
                        var current = TimeSpan.FromMilliseconds(metricTimeSpan.TotalMilliseconds);
                        var currentString = new TimeTickToShortStringConverter().Convert(current.Ticks, typeof(string), null, CultureInfo.InvariantCulture).ToString();

                        CurrentTicks = current.Ticks;

                        if (_vp?.buttonTime?.Content is TextBlock timeText)
                        {
                            timeText.Text = currentString;
                        }
                    }
                }
                else if ((string)_vp?.buttonTime?.Tag == "Length")
                {
                    if (_playback?.GetCurrentTime(TimeSpanType.Metric) is MetricTimeSpan metricTimeSpan)
                    {
                        var current = TimeSpan.FromMilliseconds(metricTimeSpan.TotalMilliseconds);
                        var subtractString = new TimeTickToShortStringConverter().Convert(_duration.Ticks - current.Ticks, typeof(string), null, CultureInfo.InvariantCulture).ToString();

                        CurrentTicks = current.Ticks;

                        if (_vp?.buttonTime?.Content is TextBlock timeText)
                        {
                            timeText.Text = subtractString;
                        }
                    }
                }
            });
        };
        _playback.Finished += (_, _) =>
        {
            if (!_playback.Loop)
            {
                _playback.MoveToStart();
                _vp.Dispatcher.Invoke(() =>
                {
                    _vp.buttonPlayPause.Content = FontSymbols.Play;
                });
            }
        };

        // Playback supported by DryWetMidi will block the current thread
        // So we should run it in a new thread
        _ = Task.Run(() => _playback?.Play());
        _vp.buttonPlayPause.Content = FontSymbols.Pause;
        _context.IsBusy = false;
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Segoe Fluent Icons
    /// https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
    /// </summary>
    private sealed class FontSymbols
    {
        public const string Play = "\xe768";
        public const string Pause = "\xe769";
    }
}
