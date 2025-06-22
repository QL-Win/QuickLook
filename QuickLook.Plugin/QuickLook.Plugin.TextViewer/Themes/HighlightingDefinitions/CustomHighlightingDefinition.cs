using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
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

public class LightHighlightingDefinition : CustomHighlightingDefinition
{
    public override string Theme => "Light";

    public override HighlightingColor DefaultTextColor => new()
    {
        Foreground = new SimpleHighlightingBrush(Colors.Black)
    };
}

public class DarkHighlightingDefinition : CustomHighlightingDefinition
{
    public override string Theme => "Dark";

    public override HighlightingColor DefaultTextColor => new()
    {
        Foreground = new SimpleHighlightingBrush("#D4D4C9".ToColor())
    };
}

public interface ICustomHighlightingDefinition : IHighlightingDefinition
{
    public string Theme { get; }

    public string Extension { get; }

    public DocumentColorizingTransformer[] LineTransformers { get; }
}
