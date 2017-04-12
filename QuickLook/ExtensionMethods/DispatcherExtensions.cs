using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QuickLook.ExtensionMethods
{
    public static class DispatcherExtensions
    {
        public static void Delay(this Dispatcher disp, int delayMs,
            Action<object> action, object parm = null)
        {
            Task.Delay(delayMs).ContinueWith(t => { disp.Invoke(action, parm); });
        }

        public static void DelayWithPriority(this Dispatcher disp, int delayMs,
            Action<object> action, object parm = null,
            DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            Task.Delay(delayMs).ContinueWith(t => { disp.BeginInvoke(action, priority, parm); });
        }

        public static async Task DelayAsync(this Dispatcher disp, int delayMs,
            Action<object> action, object parm = null,
            DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            await Task.Delay(delayMs);
            await disp.BeginInvoke(action, priority, parm);
        }
    }
}