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

using QuickLook.Common.Controls;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.TextViewer.Generators;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer;

// Privacy mode for .env files: values are masked until the user reveals them,
// with an eye button in the top right corner (and Alt+M) to show / hide them.
public partial class TextViewerPanel
{
    private bool _envMaskAvailable;
    private bool _envRevealed;
    private EnvMaskGenerator _envMask;

    /// <summary>Raised after the user hides or reveals the values of an .env file.</summary>
    public event EventHandler EnvMaskVisibilityChanged;

    /// <summary>True when the file being previewed is an .env file and masking is enabled.</summary>
    public bool IsEnvMaskAvailable => _envMaskAvailable;

    /// <summary>True while the real values of an .env file are shown. Always false after opening a file.</summary>
    public bool AreEnvValuesRevealed => _envRevealed;

    /// <summary>
    /// Decides synchronously, before the file is read, whether masking applies,
    /// so that the toolbar button can be created right after the preview starts.
    /// </summary>
    private void PrepareDeveloperFeatures(string path)
    {
        _envMaskAvailable = EnvMaskGenerator.IsEnabledFor(path);
    }

    /// <summary>
    /// Installs the mask generator. <paramref name="fullText"/> is the decoded text before long lines are truncated. Called before the document is assigned to the editor,
    /// so a secret is never drawn unmasked, not even for a single frame.
    /// </summary>
    private void InstallDeveloperFeatures(string path, string fullText)
    {
        if (_envMaskAvailable)
        {
            _envMask = new EnvMaskGenerator(fullText) { IsMasked = !_envRevealed };
            TextArea.TextView.ElementGenerators.Add(_envMask);
        }
    }

    /// <summary>Hides or reveals the values of an .env file. Does nothing for other files.</summary>
    public void ToggleEnvMask()
    {
        if (!_envMaskAvailable)
            return;

        _envRevealed = !_envRevealed;

        // The generator may not exist yet while the file is still loading;
        // it picks up _envRevealed when it is installed.
        if (_envMask is not null)
        {
            _envMask.IsMasked = !_envRevealed;
            TextArea.TextView.Redraw();
        }

        EnvMaskVisibilityChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns the element to show in the viewer window. For .env files this is the editor with an
    /// eye button in its top right corner (like the buttons of the image viewer); for every other
    /// file it is just the editor itself. Call it after <see cref="LoadFileAsync"/>.
    /// </summary>
    public FrameworkElement WrapWithEnvToggle()
    {
        if (!_envMaskAvailable)
            return this;

        var button = new Button
        {
            Width = 24,
            Height = 24,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            Focusable = false,
        };

        // Same look as the caption buttons of the viewer window. The style is looked up when the
        // button is added to the window, so it follows the current light / dark theme.
        button.SetResourceReference(FrameworkElement.StyleProperty, "CaptionButtonStyle");

        // Local values win over the style, so size and focus behavior stay as set above.
        button.Width = 24;
        button.Height = 24;

        button.Click += (_, _) => ToggleEnvMask();
        EnvMaskVisibilityChanged += (_, _) => UpdateEnvToggleButton(button);
        UpdateEnvToggleButton(button);

        var overlay = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,

            // Leaves room for the vertical scroll bar of the editor.
            Margin = new Thickness(0, 8, 24, 0),
        };
        overlay.Children.Add(button);

        var host = new Grid();
        host.Children.Add(this);
        host.Children.Add(overlay);
        return host;
    }

    /// <summary>
    /// Shows an eye while values are hidden (click to reveal) and a crossed-out eye while they are
    /// visible (click to hide), with a matching tooltip.
    /// </summary>
    private void UpdateEnvToggleButton(Button button)
    {
        bool revealed = _envRevealed;

        button.Content = revealed ? FontSymbols.Hide : FontSymbols.RedEye;

        string translationFile = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        button.ToolTip = TranslationHelper.Get(
            revealed ? "MW_HideSecretValues" : "MW_ShowSecretValues",
            translationFile,
            failsafe: revealed ? "Hide secret values" : "Show secret values");
    }
}
