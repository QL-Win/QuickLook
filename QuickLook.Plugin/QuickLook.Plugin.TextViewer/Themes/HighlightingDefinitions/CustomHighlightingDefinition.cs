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

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions;

[TypeConverter(typeof(HighlightingDefinitionTypeConverter))]
public class CustomHighlightingDefinition : ICustomHighlightingDefinition
{
    public virtual string Theme => "Light";

    public virtual string Name => null;

    public virtual string Extension => Name != null ? $".{Name}" : null;

    public virtual HighlightingRuleSet MainRuleSet => new();

    public virtual HighlightingRuleSet GetNamedRuleSet(string name) => null;

    public virtual HighlightingColor GetNamedColor(string name) => null;

    public virtual IEnumerable<HighlightingColor> NamedHighlightingColors => [];

    public virtual IDictionary<string, string> Properties => new Dictionary<string, string>();

    public virtual HighlightingColor DefaultTextColor => new();

    public virtual HighlightingRuleSet GetRuleSet(string name) => MainRuleSet;

    public virtual DocumentColorizingTransformer[] LineTransformers { get; }
}

[CustomHighlightingDefinition("Light")]
public abstract class LightHighlightingDefinition : CustomHighlightingDefinition
{
    public override string Theme => "Light";

    public override HighlightingColor DefaultTextColor => new()
    {
        Foreground = new SimpleHighlightingBrush(Colors.Black)
    };
}

[CustomHighlightingDefinition("Dark")]
public abstract class DarkHighlightingDefinition : CustomHighlightingDefinition
{
    public override string Theme => "Dark";

    public override HighlightingColor DefaultTextColor => new()
    {
        Foreground = new SimpleHighlightingBrush("#D4D4C9".ToColor())
    };
}

public sealed class CustomHighlightingDefinitionAttribute(string theme) : Attribute
{
    public string Theme { get; } = theme;
}

public sealed class CustomHighlightingDefinitionClass(ICustomHighlightingDefinition instance, string theme)
{
    public ICustomHighlightingDefinition Instance { get; } = instance;

    public string Theme { get; } = theme;
}

public interface ICustomHighlightingDefinition : IHighlightingDefinition
{
    public string Theme { get; }

    public string Extension { get; }

    public DocumentColorizingTransformer[] LineTransformers { get; }
}
