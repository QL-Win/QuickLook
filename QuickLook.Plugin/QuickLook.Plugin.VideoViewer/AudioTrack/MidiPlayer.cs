using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.VideoViewer.AudioTrack;

internal class MidiPlayer : IDisposable
{
    private ViewerPanel _vp;
    private ContextObject _context;
    private MidiFile _midiFile;
    private OutputDevice _outputDevice;
    private Playback _playback;
    private TimeSpan _duration;
    private MethodInfo _setShouldLoop; // _vp.set_ShouldLoop()

    public MidiPlayer(ViewerPanel panle, ContextObject context)
    {
        _vp = panle;
        _context = context;
    }

    public void Dispose()
    {
        _vp = null;
        _context = null;
        _outputDevice?.Dispose();
        _playback?.Stop();
        _playback?.Dispose();
    }

    public void LoadAndPlay(string path, MediaInfo.MediaInfo info)
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
                _vp.sliderProgress.Maximum = _duration.Ticks; // Unbinding
                _vp.sliderProgress.Value = 0L; // Unbinding
            }
        }

        _vp.buttonPlayPause.Click += (_, _) =>
        {
            if (_playback.IsRunning)
            {
                _playback.Stop();
                _vp.buttonPlayPause.Content = "\xE768";
            }
            else
            {
                _playback.Start();
                _vp.buttonPlayPause.Content = "\xE769";
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

        //_vp.sliderProgress.ValueChanged += (_, _) =>
        //{
        //    if (!_isValueHandling)
        //    {
        //        _playback?.Stop();

        //        double seekPercent = _vp.sliderProgress.Value / _duration.Ticks;
        //        long moveTime = (long)(_duration.Ticks * seekPercent);
        //        TimeSpan timeSpan = TimeSpan.FromTicks(moveTime);
        //        _playback?.MoveToTime(new MetricTimeSpan(timeSpan));

        //        _playback.Start();
        //    }
        //};

        _vp.sliderProgress.PreviewMouseDown += (_, e) =>
        {
            _playback?.Stop();

            Point mousePosition = e.GetPosition(_vp.sliderProgress);
            double newValue = mousePosition.X / _vp.sliderProgress.ActualWidth * (_vp.sliderProgress.Maximum - _vp.sliderProgress.Minimum) + _vp.sliderProgress.Minimum;
            double seekPercent = newValue / _duration.Ticks;
            long moveTime = (long)(_duration.Ticks * seekPercent);
            TimeSpan timeSpan = TimeSpan.FromTicks(moveTime);
            _playback?.MoveToTime(new MetricTimeSpan(timeSpan));

            _playback.Start();
        };
        _vp.sliderProgress.IsSelectionRangeEnabled = false;
        _vp.sliderProgress.PreviewMouseUp += (_, _) =>
        {
            //_playback.Start();
        };

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

                        _vp.sliderProgress.Value = current.Ticks;

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

                        _vp.sliderProgress.Value = current.Ticks;

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
                    _vp.buttonPlayPause.Content = "\xE768";
                });
            }
        };
        _ = Task.Run(() => _playback?.Play());
        _vp.buttonPlayPause.Content = "\xE769";
        _context.IsBusy = false;
    }
}
