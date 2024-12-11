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

using System.Threading;
using System.Threading.Tasks;

namespace QuickLook;

internal class FocusMonitor
{
    private static FocusMonitor _instance;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;

        new Task(() =>
        {
            var last = string.Empty;

            while (IsRunning)
            {
                Thread.Sleep(500);

                if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                    NativeMethods.QuickLook.FocusedWindowType.Invalid)
                    continue;

                var path = NativeMethods.QuickLook.GetCurrentSelection();
                if (IsRunning && last != path)
                {
                    last = path;
                    PipeServerManager.SendMessage(PipeMessages.Switch, path);
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
