using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickLook
{
    internal class FocusMonitor
    {
        public delegate void HeartbeatEventHandler(HeartbeatEventArgs e);

        private static FocusMonitor _instance;

        public bool IsRunning { get; private set; }

        public event HeartbeatEventHandler Heartbeat;

        public void Start()
        {
            IsRunning = true;

            new Task(() =>
            {
                while (IsRunning)
                {
                    Thread.Sleep(500);

                    if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                        NativeMethods.QuickLook.FocusedWindowType.Invalid)
                        continue;

                    var file = NativeMethods.QuickLook.GetCurrentSelection();
                    Heartbeat?.Invoke(new HeartbeatEventArgs(DateTime.Now.Ticks, file));
                }
            }).Start();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        internal static FocusMonitor GetInstance()
        {
            return _instance ?? (_instance = new FocusMonitor());
        }
    }

    internal class HeartbeatEventArgs : EventArgs
    {
        public HeartbeatEventArgs(long tick, string files)
        {
            InvokeTick = tick;
            FocusedFile = files;
        }

        public long InvokeTick { get; }
        public string FocusedFile { get; }
    }
}