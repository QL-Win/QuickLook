using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Light;

/// <summary>
/// Rainbow CSV Highlighting Definition
/// </summary>
public class CSVHighlightingDefinition : LightHighlightingDefinition
{
    public override string Name => "CSV";

    public override string Extension => ".csv";

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new RainbowTransformer(',')];

    public class RainbowTransformer(char sep) : DocumentColorizingTransformer
    {
        private readonly char _sep = sep;

        private readonly Brush[] _brushes =
        [
            Colors.DarkRed.ToBrush(),
            Colors.DarkBlue.ToBrush(),
            Colors.DarkGreen.ToBrush(),
            Colors.DarkMagenta.ToBrush(),
            Colors.SaddleBrown.ToBrush(),
            Colors.Teal.ToBrush(),
            Colors.Olive.ToBrush(),
            Colors.SlateBlue.ToBrush(),
        ];

        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);
            if (string.IsNullOrEmpty(text))
                return;

            int offset = line.Offset;
            int i = 0;
            int col = 0;
            int len = text.Length;

            while (i < len)
            {
                int fieldStart = i;
                int contentStart = i;
                int contentEnd = i;

                if (text[i] == '"')
                {
                    // quoted field
                    contentStart = i + 1;
                    i++;
                    while (i < len)
                    {
                        if (text[i] == '"')
                        {
                            if (i + 1 < len && text[i + 1] == '"')
                            {
                                // escaped quote
                                i += 2;
                                continue;
                            }
                            else
                            {
                                // end quote
                                contentEnd = i;
                                i++;
                                break;
                            }
                        }
                        i++;
                    }
                    // skip to separator
                    while (i < len && text[i] != _sep) i++;
                }
                else
                {
                    // unquoted
                    contentStart = i;
                    while (i < len && text[i] != _sep) i++;
                    contentEnd = i;
                }

                // color content [contentStart, contentEnd)
                if (contentEnd > contentStart)
                {
                    int a = offset + contentStart;
                    int b = offset + contentEnd;
                    var brush = _brushes[col % _brushes.Length];
                    ChangeLinePart(a, b, el => el.TextRunProperties.SetForegroundBrush(brush));
                }

                // advance past separator
                if (i < len && text[i] == _sep) i++;
                col++;
            }
        }
    }
}
