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

using System.Collections.Generic;

namespace QuickLook.Common.Plugin.MoreMenu;

/// <summary>
/// Interface implemented by QuickLook plugins that want to provide custom context menu items.
/// Plugins implementing this interface can add menu items to the viewer's title bar context menu.
/// </summary>
public interface IMoreMenu
{
    /// <summary>
    /// Gets the collection of menu items that this plugin provides.
    /// This property will be queried after the Prepare method is called.
    /// </summary>
    public IEnumerable<IMenuItem> MenuItems { get; }
}
