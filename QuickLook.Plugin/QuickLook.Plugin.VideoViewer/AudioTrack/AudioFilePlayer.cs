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

using NAudio.Wave;
using System;
using System.Diagnostics;

namespace QuickLook.Plugin.VideoViewer.AudioTrack;

internal sealed class AudioFilePlayer : IDisposable
{
    private readonly object _sync = new();
    private readonly MediaFoundationReader _reader;
    private readonly LoopingWaveProvider _provider;
    private readonly WaveOutEvent _output;
    private readonly long _durationTicks;
    private volatile bool _suppressEnded;
    private bool _stopPending;
    private bool _restartPending;
    private bool _disposed;

    private AudioFilePlayer(string path, bool loop, double volume)
    {
        _reader = new MediaFoundationReader(path);
        _durationTicks = _reader.TotalTime.Ticks;
        _provider = new LoopingWaveProvider(_reader, _sync) { Loop = loop };
        _output = new WaveOutEvent
        {
            DesiredLatency = 100,
            NumberOfBuffers = 3,
        };
        _output.PlaybackStopped += OnPlaybackStopped;

        try
        {
            _output.Init(_provider);
            _output.Volume = (float)Math.Max(0d, Math.Min(1d, volume));
        }
        catch
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _reader.Dispose();
            throw;
        }
    }

    public static AudioFilePlayer TryCreate(string path, bool loop, double volume)
    {
        try
        {
            return new AudioFilePlayer(path, loop, volume);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return null;
        }
    }

    public event EventHandler PlaybackEnded;

    public long DurationTicks => _durationTicks;

    public long PositionTicks
    {
        get
        {
            lock (_sync)
            {
                return _reader.CurrentTime.Ticks;
            }
        }
    }

    public bool IsPlaying => !_disposed && _output.PlaybackState == PlaybackState.Playing;

    public bool Loop
    {
        get => _provider.Loop;
        set => _provider.Loop = value;
    }

    public double Volume
    {
        get => _output.Volume;
        set => _output.Volume = (float)Math.Max(0d, Math.Min(1d, value));
    }

    public void Play()
    {
        if (_disposed)
            return;

        lock (_sync)
        {
            if (_durationTicks > 0 && _reader.CurrentTime >= _reader.TotalTime)
                _reader.CurrentTime = TimeSpan.Zero;

            if (_stopPending)
            {
                _restartPending = true;
                return;
            }
        }

        _output.Play();
    }

    public void Pause()
    {
        if (_disposed)
            return;

        lock (_sync)
        {
            _restartPending = false;
        }

        _output.Pause();
    }

    public void Seek(long ticks)
    {
        if (_disposed)
            return;

        PlaybackState state = _output.PlaybackState;
        if (state == PlaybackState.Playing)
        {
            lock (_sync)
            {
                _reader.CurrentTime = TimeSpan.FromTicks(Math.Max(0L, Math.Min(ticks, _durationTicks)));
                _suppressEnded = true;
                _stopPending = true;
                _restartPending = true;
            }

            _output.Stop();
            return;
        }

        if (state == PlaybackState.Paused)
        {
            lock (_sync)
            {
                _reader.CurrentTime = TimeSpan.FromTicks(Math.Max(0L, Math.Min(ticks, _durationTicks)));
                _suppressEnded = true;
                _stopPending = true;
                _restartPending = false;
            }

            _output.Stop();
            return;
        }

        lock (_sync)
        {
            _reader.CurrentTime = TimeSpan.FromTicks(Math.Max(0L, Math.Min(ticks, _durationTicks)));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _output.PlaybackStopped -= OnPlaybackStopped;
        _output.Stop();
        _output.Dispose();

        lock (_sync)
        {
            _reader.Dispose();
        }
    }

    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        if (_disposed)
            return;

        bool suppress;
        bool restart;

        lock (_sync)
        {
            suppress = _suppressEnded;
            restart = _restartPending;
            _suppressEnded = false;
            _stopPending = false;
            _restartPending = false;
        }

        // The playback worker has exited by the time this event is raised, so the
        // restart no longer races the old worker over the same buffers.
        if (restart)
        {
            _output.Play();
            return;
        }

        if (suppress)
            return;

        if (_provider.Loop)
            return;

        if (e.Exception != null)
            Debug.WriteLine(e.Exception);

        if (PositionTicks < _durationTicks - TimeSpan.TicksPerMillisecond * 200)
            return;

        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private sealed class LoopingWaveProvider : IWaveProvider
    {
        private readonly WaveStream _source;
        private readonly object _sync;
        private volatile bool _loop;

        public LoopingWaveProvider(WaveStream source, object sync)
        {
            _source = source;
            _sync = sync;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int total = 0;

            while (total < count)
            {
                int read;

                lock (_sync)
                {
                    read = _source.Read(buffer, offset + total, count - total);

                    if (read == 0 && _loop)
                    {
                        if (_source.Position == 0)
                            break;

                        _source.Position = 0;
                        continue;
                    }
                }

                if (read == 0)
                    break;

                total += read;
            }

            return total;
        }
    }
}
