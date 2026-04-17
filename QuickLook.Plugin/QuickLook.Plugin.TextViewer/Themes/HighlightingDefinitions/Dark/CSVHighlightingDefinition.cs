using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

/// <summary>
/// Rainbow CSV Highlighting Definition
/// </summary>
public class CSVHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "CSV";

    public override string Extension => ".csv";

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new RainbowTransformer(',')];

    public class RainbowTransformer(char sep) : DocumentColorizingTransformer
    {
        private readonly char _sep = sep;

        private readonly Brush[] _brushes =
        [
            Colors.OrangeRed.ToBrush(),
            Colors.CornflowerBlue.ToBrush(),
            Colors.LimeGreen.ToBrush(),
            Colors.MediumVioletRed.ToBrush(),
            Colors.Peru.ToBrush(),
            Colors.CadetBlue.ToBrush(),
            Colors.Khaki.ToBrush(),
            Colors.MediumSlateBlue.ToBrush(),
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
                int contentStart = i;
                int contentEnd = i;

                if (text[i] == '"')
                {
                    contentStart = i + 1;
                    i++;
                    while (i < len)
                    {
                        if (text[i] == '"')
                        {
                            if (i + 1 < len && text[i + 1] == '"')
                            {
                                i += 2;
                                continue;
                            }
                            else
                            {
                                contentEnd = i;
                                i++;
                                break;
                            }
                        }
                        i++;
                    }
                    while (i < len && text[i] != _sep) i++;
                }
                else
                {
                    contentStart = i;
                    while (i < len && text[i] != _sep) i++;
                    contentEnd = i;
                }

                if (contentEnd > contentStart)
                {
                    int a = offset + contentStart;
                    int b = offset + contentEnd;
                    var brush = _brushes[col % _brushes.Length];
                    ChangeLinePart(a, b, el => el.TextRunProperties.SetForegroundBrush(brush));
                }

                if (i < len && text[i] == _sep) i++;
                col++;
            }
        }
    }
}
