// Copyright © 2017-2025 QL-Win Contributors
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
using QuickLook.Common.Plugin;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuickLook.Plugin.MediaInfoViewer;

public class TextViewerPanel : TextBox
{
    public TextViewerPanel(string text, ContextObject context)
    {
        _ = context;

        Margin = new Thickness(8, 0, 0, 0);
        FontSize = 14;
        IsReadOnly = true;
        IsManipulationEnabled = true;

        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_Copy", domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.Copy
        });
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_SelectAll",
                domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.SelectAll
        });

        ManipulationInertiaStarting += Viewer_ManipulationInertiaStarting;
        ManipulationStarting += Viewer_ManipulationStarting;
        ManipulationDelta += Viewer_ManipulationDelta;

        PreviewMouseWheel += Viewer_MouseWheel;

        FontFamily = new FontFamily("Consolas, " + TranslationHelper.Get("Editor_FontFamily",
            domain: Assembly.GetExecutingAssembly().GetName().Name));

        LoadTextAsync(text);
    }

    private void Viewer_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
    {
        e.TranslationBehavior = new InertiaTranslationBehavior
        {
            InitialVelocity = e.InitialVelocities.LinearVelocity,
            DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0)
        };
    }

    private void Viewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;

        ScrollToVerticalOffset(VerticalOffset - e.Delta);
    }

    private void Viewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
        e.Handled = true;

        var delta = e.DeltaManipulation;
        ScrollToVerticalOffset(VerticalOffset - delta.Translation.Y);
    }

    private void Viewer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
    {
        e.Mode = ManipulationModes.Translate;
    }

    private void LoadTextAsync(string text)
    {
        Text = text;
    }
}
