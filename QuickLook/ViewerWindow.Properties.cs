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
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Violeta.Appearance;

namespace QuickLook;

public partial class ViewerWindow : INotifyPropertyChanged
{
    private readonly ResourceDictionary _darkDict = new()
    {
        Source = new Uri("pack://application:,,,/QuickLook.Common;component/Styles/MainWindowStyles.Dark.xaml")
    };

    private bool _canOldPluginResize;
    private bool _pinned;
    private bool _isFullscreen;
    private WindowState _preFullscreenWindowState;
    private WindowStyle _preFullscreenWindowStyle;
    private ResizeMode _preFullscreenResizeMode;
    private Rect _preFullscreenBounds;

    public bool Pinned
    {
        get => _pinned;
        set
        {
            _pinned = value;
            buttonPin.Tag = "Pin";
            OnPropertyChanged();
        }
    }

    public IViewer Plugin { get; private set; }
    public ContextObject ContextObject { get; private set; }
    public Themes CurrentTheme { get; private set; }

    public ICommand CloseCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ContextObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ContextObject.Theme):
                SwitchTheme(ContextObject.Theme);
                break;

            case nameof(ContextObject.Title):
                if (!string.IsNullOrWhiteSpace(ContextObject.Title))
                {
                    Dispatcher?.Invoke(() =>
                    {
                        // We can not update the Title when ShowInTaskbar is false
                        // https://github.com/QL-Win/QuickLook/issues/1628
                        Title = $"QuickLook - {ContextObject.Title}";
                    });
                }
                break;

            default:
                break;
        }
    }

    public void SwitchTheme(Themes theme)
    {
        var isDark = false;

        switch (theme)
        {
            case Themes.None:
                isDark = OSThemeHelper.AppsUseDarkTheme();
                break;

            case Themes.Dark:
            case Themes.Light:
                isDark = theme == Themes.Dark;
                break;
        }

        if (isDark)
        {
            CurrentTheme = Themes.Dark;

            // Update theme for QuickLook controls
            if (!Resources.MergedDictionaries.Contains(_darkDict))
                Resources.MergedDictionaries.Add(_darkDict);

            // Update theme for WPF-UI controls
            ThemeManager.Apply(ApplicationTheme.Dark);
        }
        else
        {
            CurrentTheme = Themes.Light;

            // Update theme for QuickLook controls
            if (Resources.MergedDictionaries.Contains(_darkDict))
                Resources.MergedDictionaries.Remove(_darkDict);

            // Update theme for WPF-UI controls
            ThemeManager.Apply(ApplicationTheme.Light);
        }
    }
}
