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

using QuickLook.Common.Annotations;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuickLook.Common.Plugin.MoreMenu;

/// <summary>
/// Base class for QuickLook menu items that provides common functionality
/// and implements INotifyPropertyChanged for dynamic property updates.
/// </summary>
public class MoreMenuItem : IMenuItem
{
    private object _icon;
    private object _header;
    private IEnumerable<IMenuItem> _menuItems;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private ICommand _command;
    private object _commandParameter;
    private string _toolTip;
    private bool _isSeparator;

    /// <inheritdoc/>
    public virtual object Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    /// <inheritdoc/>
    public virtual object Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    /// <inheritdoc/>
    public IEnumerable<IMenuItem> MenuItems
    {
        get => _menuItems;
        set => SetProperty(ref _menuItems, value);
    }

    /// <inheritdoc/>
    public virtual bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <inheritdoc/>
    public virtual bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <inheritdoc/>
    public virtual ICommand Command
    {
        get => _command;
        set => SetProperty(ref _command, value);
    }

    /// <inheritdoc/>
    public virtual object CommandParameter
    {
        get => _commandParameter;
        set => SetProperty(ref _commandParameter, value);
    }

    /// <inheritdoc/>
    public virtual string ToolTip
    {
        get => _toolTip;
        set => SetProperty(ref _toolTip, value);
    }

    /// <inheritdoc/>
    public virtual bool IsSeparator
    {
        get => _isSeparator;
        set => SetProperty(ref _isSeparator, value);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the property value and raises PropertyChanged if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">The backing field for the property.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>True if the property value was changed; otherwise, false.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
