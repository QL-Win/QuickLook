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

using QuickLook.Common.Helpers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace QuickLook.Helpers;

/// <summary>
/// Diagnostic log for the keyboard pipeline, written to QuickLook.Keyboard.log in the data folder.
/// Only QuickLook's own keys and anomalies are logged, never other keystrokes.
/// </summary>
internal static class KeyboardLog
{
    private const long MAX_SIZE = 512 * 1024;

    /// <summary>Preview phases slower than this block switching to the next file and get logged.</summary>
    private const long SLOW_PREVIEW_PHASE_MS = 100;

    private static readonly string LogPath = Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Keyboard.log");

    // Bounded so that a stuck disk can never make the keyboard hook wait or grow memory.
    private static readonly BlockingCollection<string> Queue = new(new ConcurrentQueue<string>(), 1000);

    static KeyboardLog()
    {
        new Thread(WriteLoop)
        {
            IsBackground = true,
            Name = "QuickLook.KeyboardLog",
            Priority = ThreadPriority.BelowNormal,
        }.Start();
    }

    /// <summary>
    /// Never blocks; safe to call from inside the low-level keyboard hook.
    /// </summary>
    public static void Write(string message)
    {
        Queue.TryAdd($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [T{Thread.CurrentThread.ManagedThreadId}] {message}");
    }

    /// <summary>
    /// Logs a preview phase that ran on the UI thread only when it was slow enough to delay
    /// the next selection change, naming the plugin and file type, never the file name.
    /// </summary>
    public static void WriteIfSlow(string phase, long elapsedMs)
    {
        if (elapsedMs >= SLOW_PREVIEW_PHASE_MS)
            Write($"Slow preview {phase}: {elapsedMs} ms");
    }

    private static void WriteLoop()
    {
        var batch = new StringBuilder();

        foreach (var line in Queue.GetConsumingEnumerable())
        {
            batch.AppendLine(line);
            while (Queue.TryTake(out var more))
                batch.AppendLine(more);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

                var file = new FileInfo(LogPath);
                if (file.Exists && file.Length > MAX_SIZE)
                {
                    File.Delete(LogPath + ".old");
                    File.Move(LogPath, LogPath + ".old");
                }

                File.AppendAllText(LogPath, batch.ToString());
            }
            catch
            {
                // logging must never take QuickLook down
            }

            batch.Clear();
        }
    }
}
