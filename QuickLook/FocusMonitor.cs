using System;
using System.Threading;
using System.Threading.Tasks;
using QuickLook.Helpers;

namespace QuickLook
{
    internal class FocusMonitor
    {
        public delegate void FocusedItemChangedEventHandler(FocusedItemChangedEventArgs e);

        private static FocusMonitor _instance;

        public bool IsRunning { get; private set; }

        public event FocusedItemChangedEventHandler FocusedItemChanged;

        public void Start()
        {
            IsRunning = true;

            new Task(() =>
            {
                var currentPath = NativeMethods.QuickLook.GetCurrentSelectionFirst();

                while (IsRunning)
                {
                    Thread.Sleep(500);

                    if (WindowHelper.IsFocusedControlExplorerItem())
                    {
                        var file = NativeMethods.QuickLook.GetCurrentSelectionFirst();
                        if (file != currentPath)
                        {
                            FocusedItemChanged?.Invoke(new FocusedItemChangedEventArgs(file));
                            currentPath = file;
                        }
                    }
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

    internal class FocusedItemChangedEventArgs : EventArgs
    {
        public FocusedItemChangedEventArgs(string files)
        {
            FocusedFile = files;
        }

        public string FocusedFile { get; }
    }
}