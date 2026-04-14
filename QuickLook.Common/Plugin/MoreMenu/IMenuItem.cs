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
using System.ComponentModel;
using System.Windows.Input;

namespace QuickLook.Common.Plugin.MoreMenu;

/// <summary>
/// Represents a menu item that can be added to the QuickLook viewer's context menu.
/// </summary>
public interface IMenuItem : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the icon for the menu item. Can be a string (for Unicode symbols)
    /// String, <seealso cref="Wpf.Ui.Controls.FontSymbols"/> check from https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
    /// ImageSource, or null for no icon.
    /// </summary>
    public object Icon { get; set; }

    /// <summary>
    /// Gets the display text for the menu item.
    /// </summary>
    public object Header { get; set; }

    /// <summary>
    /// Gets the collection of menu items.
    /// </summary>
    public IEnumerable<IMenuItem> MenuItems { get; set; }

    /// <summary>
    /// Gets a value indicating whether the menu item is visible.
    /// This allows dynamic show/hide based on file context.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Gets a value indicating whether the menu item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the command to execute when the menu item is clicked.
    /// </summary>
    public ICommand Command { get; set; }

    /// <summary>
    /// Gets the command parameter for the Command.
    /// </summary>
    public object CommandParameter { get; set; }

    /// <summary>
    /// Gets the tooltip text for the menu item.
    /// </summary>
    public string ToolTip { get; set; }

    /// <summary>
    /// Gets a value indicating whether this menu item represents a separator.
    /// When true, other properties are ignored and a separator line is shown.
    /// </summary>
    public bool IsSeparator { get; set; }
}
