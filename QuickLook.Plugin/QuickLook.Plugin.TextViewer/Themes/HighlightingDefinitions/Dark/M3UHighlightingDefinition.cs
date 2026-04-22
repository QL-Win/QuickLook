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

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class M3UHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "M3U Playlist";

    public override string Extension => ".m3u;.m3u8";

    public override HighlightingRuleSet MainRuleSet => new()
    {
        Rules =
        {
            new HighlightingRule
            {
                Regex = new Regex(@"^#.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            },
            new HighlightingRule
            {
                Regex = new Regex(@"^#EXT[\w\-:.,=\s]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                Color = GetNamedColor("Tag")
            },
        }
    };

    public override HighlightingColor GetNamedColor(string name)
    {
        return name switch
        {
            "Comment" => new HighlightingColor
            {
                Name = "Comment",
                Foreground = new SimpleHighlightingBrush("#6A9949".ToColor()),
            },
            "Tag" => new HighlightingColor
            {
                Name = "Tag",
                Foreground = new SimpleHighlightingBrush("#8AB4F8".ToColor()),
            },
            _ => null,
        };
    }

    public override IEnumerable<HighlightingColor> NamedHighlightingColors =>
    [
        GetNamedColor("Comment"),
        GetNamedColor("Tag"),
    ];

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [];
}
