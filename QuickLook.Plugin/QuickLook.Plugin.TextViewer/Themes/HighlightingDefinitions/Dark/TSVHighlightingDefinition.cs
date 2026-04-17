using ICSharpCode.AvalonEdit.Rendering;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

/// <summary>
/// Rainbow TSV Highlighting Definition
/// </summary>
public class TSVHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "TSV";

    public override string Extension => ".tsv";

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new CSVHighlightingDefinition.RainbowTransformer('\t')];
}
