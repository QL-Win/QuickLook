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
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UnblockZoneIdentifier;

namespace QuickLook.Plugin.OfficeViewer;

public class Plugin : IViewer
{
    private static readonly string[] Extensions =
    [
        ".doc", ".docx", ".docm", ".odt",
        ".xls", ".xlsx", ".xlsm", ".xlsb", ".ods",
        ".ppt", ".pptx", ".odp",
        ".vsd", ".vsdx",
    ];

    private PreviewPanel _panel;

    public int Priority => -1;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        if (!Extensions.Any(path.ToLower().EndsWith))
            return false;

        var previewHandler = ShellExRegister.GetPreviewHandlerGUID(Path.GetExtension(path));
        if (previewHandler == Guid.Empty)
            return false;

        var checkPreviewHandler = SettingHelper.Get("CheckPreviewHandler", true, "QuickLook.Plugin.OfficeViewer");
        if (!checkPreviewHandler)
            return true;

        if (!string.IsNullOrWhiteSpace(CLSIDRegister.GetName(previewHandler.ToString("B"))))
        {
            return true;
        }
        else
        {
            // To restore the preview handler CLSID to MS Office
            // if running with administrative privileges
            if (ShellExRegister.IsRunAsAdmin())
            {
                var fileExtension = Path.GetExtension(path);
                var fallbackHandler = fileExtension switch
                {
                    ".doc" or ".docx" or ".docm" or ".odt" => CLSIDRegister.MicrosoftWord,
                    ".xls" or ".xlsx" or ".xlsm" or ".xlsb" or ".ods" => CLSIDRegister.MicrosoftExcel,
                    ".ppt" or ".pptx" or ".odp" => CLSIDRegister.MicrosoftPowerPoint,
                    ".vsd" or ".vsdx" => CLSIDRegister.MicrosoftVisio,
                    _ => null,
                };

                if (fallbackHandler == null)
                    return false;

                if (!string.IsNullOrWhiteSpace(CLSIDRegister.GetName(fallbackHandler)))
                {
                    // Admin requested
                    ShellExRegister.SetPreviewHandlerGUID(fileExtension, new Guid(fallbackHandler));
                    return true;
                }
            }
        }

        return false;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.SetPreferredSizeFit(new Size { Width = 1200, Height = 800 }, 0.8d);
    }

    public void View(string path, ContextObject context)
    {
        // MS Office interface does not allow loading of protected view (It's also possible that I haven't found a way)
        // Therefore, we need to predict in advance and then let users choose whether to lift the protection
        if (ZoneIdentifierManager.IsZoneBlocked(path))
        {
            context.Title = $"[PROTECTED VIEW] {Path.GetFileName(path)}";

            MessageBoxResult result = MessageBox.Show(
                """
                Be careful - files from the Internet can contain viruses.
                The Office interface prevents loading in Protected View.

                Would you like OfficeViewer-Native to unblock the ZoneIdentifier of Internet?
                """,
                "PROTECTED VIEW",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _ = ZoneIdentifierManager.UnblockZone(path);
            }
            else
            {
                context.ViewerContent = new Label()
                {
                    Content = "The Office interface prevents loading in Protected View.",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                context.Title = $"[PROTECTED VIEW] {Path.GetFileName(path)}";
                context.IsBusy = false;
                return;
            }
        }

        try
        {
            _panel = new PreviewPanel();
            context.ViewerContent = _panel;
            context.Title = Path.GetFileName(path);
            _panel.PreviewFile(path, context);
        }
        catch (Exception e)
        {
            context.ViewerContent = new Label()
            {
                Content = e.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
        }

        context.IsBusy = false;
    }

    public void Cleanup()
    {
        _panel?.Dispose();
        _panel = null;
    }
}
