using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Forms.Clipboard;

namespace QuickLook.Plugin.ImageViewer.NativeMethods;

internal static class ClipboardEx
{
    public static void SetClipboardImage(this BitmapSource img)
    {
        if (img == null)
            return;

        var thread = new Thread((img) =>
        {
            if (img == null)
                return;

            var image = (BitmapSource)img;

            try
            {
                Clipboard.Clear();
            }
            catch (ExternalException) { }

            try
            {
                // BitmapFrameDecode or BitmapFrame may need to be converted to
                // a standard format of BitmapSource to ensure compatibility
                // and only the frozen image is supported
                if (image is BitmapFrame && image.IsFrozen)
                {
                    image = new WriteableBitmap(image);
                }

                using var pngMemStream = new MemoryStream();
                using var bitmap = image.Dispatcher?.Invoke(() => image.ToBitmap()) ?? image.ToBitmap();
                var data = new DataObject();

                bitmap.Save(pngMemStream, ImageFormat.Png);
                data.SetData("PNG", pngMemStream, false);

                Clipboard.SetDataObject(data, true);
            }
            catch (Exception e)
            {
                // Clipboard competition leading to failure is common
                // There is currently no UI notification of success or failure
                Debug.WriteLine(e);
            }
        })
        {
            Name = nameof(ClipboardEx),
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(img);
    }

    private static Bitmap ToBitmap(this BitmapSource source)
    {
        using (var outStream = new MemoryStream())
        {
            BitmapEncoder enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(source));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }
    }
}
