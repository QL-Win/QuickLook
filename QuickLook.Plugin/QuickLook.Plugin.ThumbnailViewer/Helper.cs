using System.IO;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ThumbnailViewer;

internal static class Helper
{
    public static BitmapImage ReadAsBitmapImage(this Stream imageData)
    {
        imageData.Seek(0U, SeekOrigin.Begin);

        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = imageData;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
