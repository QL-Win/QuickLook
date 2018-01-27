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

namespace QuickLook.Common.Plugin
{
    /// <summary>
    ///     Interface implemented by every QuickLook.Plugin
    /// </summary>
    public interface IViewer
    {
        /// <summary>
        ///     Set the priority of this plugin. A plugin with a higher priority may override one with lower priority.
        ///     Set this to int.MaxValue for a maximum priority, int.MinValue for minimum.
        /// </summary>
        int Priority { get; }

        /// <summary>
        ///     Do ont-time job when application starts. You may extract nessessary resource here.
        /// </summary>
        void Init();

        /// <summary>
        ///     Determine whether this plugin can open this file. Please also check the file header, if applicable.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        bool CanHandle(string path);

        /// <summary>
        ///     Do some preparation stuff before the window is showing. Please not do any work that costs a lot of time.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        /// <param name="context">A runtime object which allows interaction between this plugin and QuickLook.</param>
        void Prepare(string path, ContextObject context);

        /// <summary>
        ///     Start the loading process. During the process a busy indicator will be shown. Finish by setting context.IsBusy to
        ///     false.
        /// </summary>
        /// <param name="path">The full path of the target file.</param>
        /// <param name="context">A runtime object which allows interaction between this plugin and QuickLook.</param>
        void View(string path, ContextObject context);

        /// <summary>
        ///     Release any unmanaged resource here.
        /// </summary>
        void Cleanup();
    }
}