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

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Light;

public class PropertiesHighlightingDefinition : LightHighlightingDefinition
{
    public override string Name => "Properties";

    public override string Extension => ".properties";

    public override HighlightingRuleSet MainRuleSet => new()
    {
        Rules =
        {
            new HighlightingRule
            {
                Regex = new Regex(@"#.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            }
        }
    };

    public override HighlightingColor GetNamedColor(string name)
    {
        return name switch
        {
            "Comment" => new HighlightingColor
            {
                Name = "Comment",
                Foreground = new SimpleHighlightingBrush(Colors.Green),
            },
            _ => null
        };
    }

    public override IEnumerable<HighlightingColor> NamedHighlightingColors =>
    [
        GetNamedColor("Comment")
    ];

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new KeyHighlighter()];

    public class KeyHighlighter : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);

            if (string.IsNullOrWhiteSpace(text) || text.TrimStart().StartsWith("#"))
                return;

            int idx = text.IndexOf('=');

            if (idx <= 0)
                return;

            ChangeLinePart(line.Offset, line.Offset + idx, el => el.TextRunProperties.SetForegroundBrush(Colors.Blue.ToBrush()));
        }
    }
}
