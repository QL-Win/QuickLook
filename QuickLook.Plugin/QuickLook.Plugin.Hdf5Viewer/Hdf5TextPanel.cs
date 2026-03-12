using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.Hdf5Viewer;

public sealed class Hdf5TextPanel : UserControl
{
    private readonly TextBox _textBox;

    public Hdf5TextPanel()
    {
        _textBox = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = System.Windows.TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            BorderThickness = new System.Windows.Thickness(0),
            Padding = new System.Windows.Thickness(12, 8, 12, 8)
        };

        Content = _textBox;
    }

    public void SetText(string text)
    {
        _textBox.Text = text ?? string.Empty;
        _textBox.CaretIndex = 0;
    }
}
