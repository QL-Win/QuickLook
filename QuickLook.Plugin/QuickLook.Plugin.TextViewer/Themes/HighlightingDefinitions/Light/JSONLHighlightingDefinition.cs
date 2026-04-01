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

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Light;

public class JSONLHighlightingDefinition : LightHighlightingDefinition
{
    public override string Name => "JSONL";

    public override string Extension => ".jsonl";

    public override HighlightingRuleSet MainRuleSet => new();

    public override HighlightingColor GetNamedColor(string name) => null;

    public override IEnumerable<HighlightingColor> NamedHighlightingColors => [];

    public override DocumentColorizingTransformer[] LineTransformers { get; } =
    [
        new JsonLineHighlighter(
            keyColor:     "#0451A5",   // field names  (VSCode light default)
            stringColor:  "#A31515",   // string values (VSCode light default)
            numberColor:  "#098658",   // numbers       (VSCode light default)
            boolNullColor:"#0000FF",   // true/false/null (VSCode light default)
            braceColor:   "#800000",   // { }
            bracketColor: "#0451A5")   // [ ]
    ];
}
