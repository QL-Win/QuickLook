using System;
using System.Threading.Tasks;

namespace QuickLook.Helpers
{
    internal class ProcessHelper
    {
        // ReSharper disable once InconsistentNaming
        public static void PerformAggressiveGC()
        {
            // delay some time to make sure that all windows are closed
            Task.Delay(1000).ContinueWith(t => GC.Collect(GC.MaxGeneration));
        }
    }
}