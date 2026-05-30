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

using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.BinaryViewer;

public partial class BinaryViewerPanel : UserControl
{
    public BinaryViewerPanel()
    {
        InitializeComponent();
    }

    public void LoadFile(string path)
    {
        _hexEditor.FileName = path;

        // The HexEditorViewModel intentionally skips RefreshVisibleLines() on construction
        // for startup performance. UpdateVisibleLines() is deferred at ApplicationIdle
        // priority inside HexEditor. We dispatch at ContextIdle (lower priority, runs after
        // ApplicationIdle) to ensure VisibleLines is correctly computed before we force a
        // RefreshView(), which actually populates the Lines collection and makes bytes visible
        // without requiring the user to scroll first.
        _hexEditor.Dispatcher.BeginInvoke(
            () => _hexEditor.RefreshView(),
            DispatcherPriority.ContextIdle);
    }

    public void Unload()
    {
        // Close() properly releases the file stream and lock,
        // whereas setting FileName = null does not free the handle.
        _hexEditor.Close();
    }
}
