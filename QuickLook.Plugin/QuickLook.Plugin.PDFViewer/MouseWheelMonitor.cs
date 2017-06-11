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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace QuickLook.Plugin.PDFViewer
{
    public sealed class PreviewMouseWheelMonitor : IDisposable
    {
        private readonly UIElement _canvas;
        private readonly Dispatcher _dispatcher;
        private readonly int _sensitivity;

        private bool _disposed;
        private volatile bool _inactive;
        private AutoResetEvent _resetMonitorEvent;
        private volatile bool _stopped;

        public PreviewMouseWheelMonitor(UIElement canvas, int sensitivity)
        {
            _canvas = canvas;
            _canvas.PreviewMouseWheel += (s, e) => RaisePreviewMouseWheel(e);

            _sensitivity = sensitivity;
            _dispatcher = Dispatcher.CurrentDispatcher;
            _resetMonitorEvent = new AutoResetEvent(false);

            _disposed = false;
            _inactive = true;
            _stopped = true;

            var monitor = new Thread(Monitor) {IsBackground = true};
            monitor.Start();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                DetachEventHandlers();
                if (_resetMonitorEvent != null)
                {
                    _resetMonitorEvent.Close();
                    _resetMonitorEvent = null;
                }
            }
        }

        public event EventHandler<MouseWheelEventArgs> PreviewMouseWheel;
        public event EventHandler<EventArgs> PreviewMouseWheelStarted;
        public event EventHandler<EventArgs> PreviewMouseWheelStopped;

        private void Monitor()
        {
            while (!_disposed)
            {
                if (_inactive) // if wheel is still inactive...
                {
                    _resetMonitorEvent.WaitOne(_sensitivity / 10); // ...wait negligibly small quantity of time...
                    continue; // ...and check again
                }
                // otherwise, if wheel is active...
                _inactive = true; // ...purposely change the state to inactive
                _resetMonitorEvent.WaitOne(_sensitivity); // wait...
                if (_inactive
                ) // ...and after specified time check if the state is still not re-activated inside mouse wheel event
                    RaiseMouseWheelStopped();
            }
        }

        private void RaisePreviewMouseWheel(MouseWheelEventArgs args)
        {
            if (_stopped)
                RaiseMouseWheelStarted();

            _inactive = false;
            if (PreviewMouseWheel != null)
                PreviewMouseWheel(_canvas, args);
        }

        private void RaiseMouseWheelStarted()
        {
            _stopped = false;
            if (PreviewMouseWheelStarted != null)
                PreviewMouseWheelStarted(_canvas, new EventArgs());
        }

        private void RaiseMouseWheelStopped()
        {
            _stopped = true;
            if (PreviewMouseWheelStopped != null)
                _dispatcher.Invoke(() => PreviewMouseWheelStopped(_canvas,
                    new
                        EventArgs())); // invoked on cached dispatcher for convenience (because fired from non-UI thread)
        }

        private void DetachEventHandlers()
        {
            if (PreviewMouseWheel != null)
                foreach (var handler in PreviewMouseWheel.GetInvocationList().Cast<EventHandler<MouseWheelEventArgs>>())
                    PreviewMouseWheel -= handler;
            if (PreviewMouseWheelStarted != null)
                foreach (var handler in PreviewMouseWheelStarted.GetInvocationList().Cast<EventHandler<EventArgs>>())
                    PreviewMouseWheelStarted -= handler;
            if (PreviewMouseWheelStopped != null)
                foreach (var handler in PreviewMouseWheelStopped.GetInvocationList().Cast<EventHandler<EventArgs>>())
                    PreviewMouseWheelStopped -= handler;
        }
    }
}