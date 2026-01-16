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

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class DiffHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "Diff";

    public override string Extension => ".diff;.patch;.rej";

    public override HighlightingRuleSet MainRuleSet => new()
    {
        Rules =
        {
            // Diff header (diff --git, index, etc.)
            new HighlightingRule
            {
                Regex = new Regex(@"^(diff --git|index|---|\+\+\+)", RegexOptions.Compiled),
                Color = GetNamedColor("Header")
            },
            // File mode and other metadata
            new HighlightingRule
            {
                Regex = new Regex(@"^(new file mode|deleted file mode|old mode|new mode|similarity index|dissimilarity index|rename from|rename to|copy from|copy to)", RegexOptions.Compiled),
                Color = GetNamedColor("Metadata")
            },
            // Hunk headers (@@ ... @@)
            new HighlightingRule
            {
                Regex = new Regex(@"^@@.*@@$", RegexOptions.Compiled),
                Color = GetNamedColor("HunkHeader")
            },
            // Added lines (+)
            new HighlightingRule
            {
                Regex = new Regex(@"^\+.*$", RegexOptions.Compiled),
                Color = GetNamedColor("Added")
            },
            // Removed lines (-)
            new HighlightingRule
            {
                Regex = new Regex(@"^-.*$", RegexOptions.Compiled),
                Color = GetNamedColor("Removed")
            },
            // Context lines (unchanged)
            new HighlightingRule
            {
                Regex = new Regex(@"^ .*", RegexOptions.Compiled),
                Color = GetNamedColor("Context")
            }
        }
    };

    public override HighlightingColor GetNamedColor(string name)
    {
        return name switch
        {
            "Header" => new HighlightingColor
            {
                Name = "Header",
                Foreground = new SimpleHighlightingBrush("#569CD6".ToColor()), // Blue for headers
            },
            "Metadata" => new HighlightingColor
            {
                Name = "Metadata",
                Foreground = new SimpleHighlightingBrush("#808080".ToColor()), // Gray for metadata
            },
            "HunkHeader" => new HighlightingColor
            {
                Name = "HunkHeader",
                Foreground = new SimpleHighlightingBrush("#C586C0".ToColor()), // Purple for hunk headers
            },
            "Added" => new HighlightingColor
            {
                Name = "Added",
                Foreground = new SimpleHighlightingBrush("#6A9949".ToColor()), // Green for added lines
            },
            "Removed" => new HighlightingColor
            {
                Name = "Removed",
                Foreground = new SimpleHighlightingBrush("#F44747".ToColor()), // Red for removed lines
            },
            "Context" => new HighlightingColor
            {
                Name = "Context",
                Foreground = new SimpleHighlightingBrush("#D4D4D4".ToColor()), // Light gray for context
            },
            _ => null,
        };
    }

    public override IEnumerable<HighlightingColor> NamedHighlightingColors =>
    [
        GetNamedColor("Header"),
        GetNamedColor("Metadata"),
        GetNamedColor("HunkHeader"),
        GetNamedColor("Added"),
        GetNamedColor("Removed"),
        GetNamedColor("Context"),
    ];

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [];
}
