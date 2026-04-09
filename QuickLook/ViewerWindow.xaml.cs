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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using Wpf.Ui.Violeta.Controls;
using static QuickLook.Common.NativeMethods.Dwmapi;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Size = System.Windows.Size;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace QuickLook;

public partial class ViewerWindow : Window
{
    private Size _customWindowSize = Size.Empty;
    private bool _ignoreNextWindowSizeChange;
    private string _path = string.Empty;
    private FileSystemWatcher _autoReloadWatcher;
    private readonly bool _autoReload;

    internal ViewerWindow()
    {
        // this object should be initialized before loading UI components, because many of which are binding to it.
        ContextObject = new ContextObject() { Source = this };

        ContextObject.PropertyChanged += ContextObject_PropertyChanged;

        InitializeComponent();

        _autoReload = SettingHelper.Get("AutoReload", false);

        Icon = (App.IsWin10 ? Properties.Resources.app_white_png : Properties.Resources.app_png).ToBitmapSource();

        FontFamily = new FontFamily(TranslationHelper.Get("UI_FontFamily", failsafe: "Segoe UI"));

        SizeChanged += SaveWindowSizeOnSizeChanged;

        StateChanged += (_, _) => _ignoreNextWindowSizeChange = true;

        windowFrameContainer.PreviewMouseMove += ShowWindowCaptionContainer;

        Topmost = SettingHelper.Get("Topmost", false);
        buttonTop.Tag = Topmost ? "Top" : "Auto";

        ShowInTaskbar = SettingHelper.Get("ShowInTaskbar", false);

        buttonTop.Click += (_, _) =>
        {
            Topmost = !Topmost;
            SettingHelper.Set("Topmost", Topmost);
            buttonTop.Tag = Topmost ? "Top" : "Auto";
        };

        buttonPin.Click += (_, _) =>
        {
            if (Pinned)
            {
                Toast.Information(TranslationHelper.Get("InfoPanel_CantPreventClosing"));
                return;
            }

            ViewWindowManager.GetInstance().ForgetCurrentWindow();
        };

        buttonCloseWindow.Click += (_, _) =>
        {
            if (Pinned)
                Close();
            else
                ViewWindowManager.GetInstance().ClosePreview();
        };

        buttonOpen.Click += (_, _) =>
        {
            if (Pinned)
                RunAndClose();
            else
                ViewWindowManager.GetInstance().RunAndClosePreview();
        };

        buttonReload.Click += (_, _) =>
        {
            ViewWindowManager.GetInstance().ReloadPreview();
        };

        buttonWindowStatus.Click += (_, _) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        buttonShare.Click += (_, _) => ShareHelper.Share(_path, this);
        buttonOpenWith.Click += (_, _) => ShareHelper.Share(_path, this, true);

        buttonReload.Visibility = SettingHelper.Get("ShowReload", false) ? Visibility.Visible : Visibility.Collapsed;

        moreItemReload.Click += (_, _) =>
        {
            ViewWindowManager.GetInstance().ReloadPreview();
        };

        moreItemCopyAsPath.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText($"\"{(_path.Length >= 260 ? @"\\?\" + _path : _path)}\"");
                Toast.Success(TranslationHelper.Get("InfoPanelMoreItem_CopySucc"));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        };

        moreItemOpenSettings.Click += (_, _) =>
        {
            Toast.Warning("Coming soon...");
        };

        // Set UI translations
        buttonTop.ToolTip = TranslationHelper.Get("MW_StayTop");
        buttonPin.ToolTip = TranslationHelper.Get("MW_PreventClosing");
        buttonOpenWith.ToolTip = TranslationHelper.Get("MW_OpenWithMenu");
        buttonShare.ToolTip = TranslationHelper.Get("MW_Share");
        buttonReload.ToolTip = TranslationHelper.Get("MW_Reload", failsafe: "Reload");
        buttonMore.ToolTip = TranslationHelper.Get("MW_More", failsafe: "More");
        moreItemReload.Header = TranslationHelper.Get("MW_Reload");
        moreItemCopyAsPath.Header = TranslationHelper.Get("InfoPanelMoreItem_CopyAsPath");
    }

    public new void Close()
    {
        // Workaround to prevent DPI jump animation when closing window in .NET Framework 4.6.2
        // Safe to remove this line if QuickLook no longer targets .NET Framework 4.6.2
        Hide();

        base.Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowHelper.RemoveWindowControls(this);

        ApplyWindowBackgroundEffects();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        ApplyWindowBackgroundEffects();
    }

    private void ApplyWindowBackgroundEffects()
    {
        var useTransparency = SettingHelper.Get("UseTransparency", true)
            && SystemParameters.IsGlassEnabled
            && !App.IsGPUInBlacklist;
        var backdrop = GetBackdropOption();

        if (useTransparency)
        {
            ApplyBackdrop(backdrop);
        }
        else
        {
            WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
            WindowHelper.DisableDwmBlur(this); // Fix white flash in dark mode
            Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
        }

        var customColor = SettingHelper.Get("WindowBackgroundColor", string.Empty, "QuickLook");
        if (!string.IsNullOrEmpty(customColor))
        {
            try
            {
                Background = (Brush)new BrushConverter().ConvertFromString(customColor);
            }
            catch
            {
                // Ignore invalid color
            }
        }
    }

    private void ApplyBackdrop(SystembackdropType backdrop)
    {
        switch (backdrop)
        {
            case SystembackdropType.None:
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.DisableDwmBlur(this); // Fix white flash in dark mode
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }
                break;

            case SystembackdropType.Auto:
            case SystembackdropType.Mica:
            default:
                if (App.IsWin11)
                {
                    if (Environment.OSVersion.Version >= new Version(10, 0, 22523))
                    {
                        WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                        WindowHelper.EnableBackdropMicaBlur(this, CurrentTheme == Themes.Dark);
                        Background = Brushes.Transparent;
                    }
                    else
                    {
                        WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                        WindowHelper.EnableMicaBlur(this, CurrentTheme == Themes.Dark);
                        Background = Brushes.Transparent;
                    }
                }
                else if (App.IsWin10)
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.EnableBlur(this);
                    Background = (Brush)FindResource("MainWindowBackground");
                }
                else
                {
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }

                break;

            case SystembackdropType.Acrylic:
                if (App.IsWin11 && Environment.OSVersion.Version >= new Version(10, 0, 22523))
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.EnableBackdropAcrylicBlur(this, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else if (App.IsWin10)
                {
                    var acrylicTint = GetAcrylicTintColor();

                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(0d);
                    WindowHelper.EnableAcrylicBlur(this, acrylicTint, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else
                {
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }

                break;

            case SystembackdropType.Acrylic10:
                if (App.IsWin10 || App.IsWin11)
                {
                    var acrylicTint = GetAcrylicTintColor();

                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(0d);
                    WindowHelper.DisableDwmBlur(this); // Restore rounded corners on Windows 11
                    WindowHelper.EnableAcrylicBlur(this, acrylicTint, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else
                {
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }

                break;

            case SystembackdropType.Acrylic11:
                if (App.IsWin11 && Environment.OSVersion.Version >= new Version(10, 0, 22523))
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.EnableBackdropAcrylicBlur(this, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else if (App.IsWin11)
                {
                    var acrylicTint = GetAcrylicTintColor();

                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(0d);
                    WindowHelper.DisableDwmBlur(this); // Restore rounded corners on Windows 11
                    WindowHelper.EnableAcrylicBlur(this, acrylicTint, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else if (App.IsWin10)
                {
                    var acrylicTint = GetAcrylicTintColor();

                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(0d);
                    WindowHelper.EnableAcrylicBlur(this, acrylicTint, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else
                {
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }

                break;

            case SystembackdropType.Tabbed:
                if (App.IsWin11 && Environment.OSVersion.Version >= new Version(10, 0, 22523))
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.EnableBackdropTabbedBlur(this, CurrentTheme == Themes.Dark);
                    Background = Brushes.Transparent;
                }
                else if (App.IsWin10)
                {
                    WindowChrome.GetWindowChrome(this)?.GlassFrameThickness = new Thickness(1d);
                    WindowHelper.EnableBlur(this);
                    Background = (Brush)FindResource("MainWindowBackground");
                }
                else
                {
                    Background = (Brush)FindResource("MainWindowBackgroundNoTransparent");
                }

                break;
        }
    }

    private Color GetAcrylicTintColor()
    {
        var customColor = SettingHelper.Get("WindowBackgroundColor", string.Empty, "QuickLook");

        if (!string.IsNullOrEmpty(customColor))
        {
            try
            {
                return ((SolidColorBrush)new BrushConverter().ConvertFromString(customColor)).Color;
            }
            catch
            {
                // Ignore invalid color
            }
        }

        return ((SolidColorBrush)FindResource("MainWindowBackground")).Color;
    }

    private static SystembackdropType GetBackdropOption()
    {
        var option = SettingHelper.Get("WindowBackdrop", nameof(SystembackdropType.Auto), "QuickLook")?.Trim();

        if (string.IsNullOrEmpty(option))
            return SystembackdropType.Auto;

        if (string.Equals(option, nameof(SystembackdropType.Acrylic), StringComparison.OrdinalIgnoreCase))
            return SystembackdropType.Acrylic;

        if (string.Equals(option, nameof(SystembackdropType.Tabbed), StringComparison.OrdinalIgnoreCase))
            return SystembackdropType.Tabbed;

        return Enum.TryParse(option, true, out SystembackdropType parsed)
            ? parsed
            : SystembackdropType.Auto;
    }

    private void SaveWindowSizeOnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // first shown?
        if (e.PreviousSize == new Size(0, 0))
            return;
        // resize when switching preview?
        if (_ignoreNextWindowSizeChange)
        {
            _ignoreNextWindowSizeChange = false;
            return;
        }

        // by user?
        _customWindowSize = new Size(Width, Height);
    }

    private void ShowWindowCaptionContainer(object sender, MouseEventArgs e)
    {
        var show = (Storyboard)windowCaptionContainer.FindResource("ShowCaptionContainerStoryboard");

        if (windowCaptionContainer.Opacity == 0 || windowCaptionContainer.Opacity == 1)
            show.Begin();
    }

    private void AutoHideCaptionContainer(object sender, EventArgs e)
    {
        if (!ContextObject.TitlebarAutoHide)
            return;

        if (!ContextObject.TitlebarOverlap)
            return;

        if (windowCaptionContainer.IsMouseOver)
            return;

        var hide = (Storyboard)windowCaptionContainer.FindResource("HideCaptionContainerStoryboard");

        hide.Begin();
    }
}
