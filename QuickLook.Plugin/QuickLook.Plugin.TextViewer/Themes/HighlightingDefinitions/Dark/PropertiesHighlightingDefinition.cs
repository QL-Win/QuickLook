using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class PropertiesHighlightingDefinition : DarkHighlightingDefinition
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
                Foreground = new SimpleHighlightingBrush("#6A9949".ToColor()),
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
            int idx = text.IndexOf('=');
            if (idx > 0)
            {
                ChangeLinePart(
                    line.Offset,
                    line.Offset + idx,
                    el => el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush())
                );
            }
        }
    }
}
