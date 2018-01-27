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
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QuickLook.Common.ExtensionMethods
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