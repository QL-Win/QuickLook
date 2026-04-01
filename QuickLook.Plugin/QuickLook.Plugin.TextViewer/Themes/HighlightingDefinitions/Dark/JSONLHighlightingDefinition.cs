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

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class JSONLHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "JSONL";

    public override string Extension => ".jsonl";

    public override HighlightingRuleSet MainRuleSet => new();

    public override HighlightingColor GetNamedColor(string name) => null;

    public override IEnumerable<HighlightingColor> NamedHighlightingColors => [];

    public override DocumentColorizingTransformer[] LineTransformers { get; } =
    [
        new JsonLineHighlighter(
            keyColor:     "#9CDCF0",   // field names  (VSCode default)
            stringColor:  "#CE9178",   // string values (VSCode default)
            numberColor:  "#B5CEA8",   // numbers       (VSCode default)
            boolNullColor:"#569CD6",   // true/false/null (VSCode default)
            braceColor:   "#DA66BE",   // { }
            bracketColor: "#FFD710")   // [ ]
    ];
}
