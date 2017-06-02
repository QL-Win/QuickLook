using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.PDFViewer
{
    internal class LibMuPdf
    {
        public static Bitmap RenderPage(IntPtr context, IntPtr document, IntPtr page, double zoomFactor)
        {
            var pageBound = new Rectangle();

            var ctm = new Matrix();
            var pix = IntPtr.Zero;
            var dev = IntPtr.Zero;

            if (App.Is64Bit)
                NativeMethods.BoundPage_64(document, page, ref pageBound);
            else
                NativeMethods.BoundPage_32(document, page, ref pageBound);

            var currentDpi = DpiHelper.GetCurrentDpi();
            var zoomX = zoomFactor * (currentDpi.HorizontalDpi / DpiHelper.DEFAULT_DPI);
            var zoomY = zoomFactor * (currentDpi.VerticalDpi / DpiHelper.DEFAULT_DPI);

            // gets the size of the page and multiplies it with zoom factors
            var width = (int) (pageBound.Width * zoomX);
            var height = (int) (pageBound.Height * zoomY);

            // sets the matrix as a scaling matrix (zoomX,0,0,zoomY,0,0)
            ctm.A = (float) zoomX;
            ctm.D = (float) zoomY;

            // creates a pixmap the same size as the width and height of the page
            if (App.Is64Bit)
                pix = NativeMethods.NewPixmap_64(context,
                    NativeMethods.LookupDeviceColorSpace_64(context, "DeviceRGB"), width, height);
            else
                pix = NativeMethods.NewPixmap_32(context,
                    NativeMethods.LookupDeviceColorSpace_32(context, "DeviceRGB"), width, height);
            // sets white color as the background color of the pixmap
            if (App.Is64Bit)
                NativeMethods.ClearPixmap_64(context, pix, 0xFF);
            else
                NativeMethods.ClearPixmap_32(context, pix, 0xFF);

            // creates a drawing device
            if (App.Is64Bit)
                dev = NativeMethods.NewDrawDevice_64(context, pix);
            else
                dev = NativeMethods.NewDrawDevice_32(context, pix);
            // draws the page on the device created from the pixmap
            if (App.Is64Bit)
                NativeMethods.RunPage_64(document, page, dev, ref ctm, IntPtr.Zero);
            else
                NativeMethods.RunPage_32(document, page, dev, ref ctm, IntPtr.Zero);

            if (App.Is64Bit)
                NativeMethods.FreeDevice_64(dev); // frees the resources consumed by the device
            else
                NativeMethods.FreeDevice_32(dev); // frees the resources consumed by the device
            dev = IntPtr.Zero;

            // creates a colorful bitmap of the same size of the pixmap
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var imageData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            unsafe
            {
                // converts the pixmap data to Bitmap data
                byte* ptrSrc;
                if (App.Is64Bit)
                    ptrSrc =
                        (byte*) NativeMethods.GetSamples_64(context, pix); // gets the rendered data from the pixmap
                else
                    ptrSrc = (byte*) NativeMethods
                        .GetSamples_32(context, pix); // gets the rendered data from the pixmap
                var ptrDest = (byte*) imageData.Scan0;
                for (var y = 0; y < height; y++)
                {
                    var pl = ptrDest;
                    var sl = ptrSrc;
                    for (var x = 0; x < width; x++)
                    {
                        //Swap these here instead of in MuPDF because most pdf images will be rgb or cmyk.
                        //Since we are going through the pixels one by one anyway swap here to save a conversion from rgb to bgr.
                        pl[2] = sl[0]; //b-r
                        pl[1] = sl[1]; //g-g
                        pl[0] = sl[2]; //r-b
                        //sl[3] is the alpha channel, we will skip it here
                        pl += 3;
                        sl += 4;
                    }
                    ptrDest += imageData.Stride;
                    ptrSrc += width * 4;
                }
            }
            bmp.UnlockBits(imageData);
            if (App.Is64Bit)
                NativeMethods.DropPixmap_64(context, pix);
            else
                NativeMethods.DropPixmap_32(context, pix);

            bmp.SetResolution(currentDpi.HorizontalDpi, currentDpi.VerticalDpi);

            return bmp;
        }

        public struct Rectangle
        {
            public float Left, Top, Right, Bottom;

            public float Width => Right - Left;
            public float Height => Bottom - Top;
        }

        public struct Matrix
        {
            public float A, B, C, D, E, F;

            public Matrix(float a, float b, float c, float d, float e, float f)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
                F = f;
            }
        }

        internal class NativeMethods
        {
            private const uint FzStoreDefault = 256 << 20;
            private const string MuPdfVersion = "1.6";

            public static IntPtr NewContext()
            {
                return App.Is64Bit
                    ? NewContext_64(IntPtr.Zero, IntPtr.Zero, FzStoreDefault, MuPdfVersion)
                    : NewContext_32(IntPtr.Zero, IntPtr.Zero, FzStoreDefault, MuPdfVersion);
            }

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_new_context_imp", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr NewContext_32(IntPtr alloc, IntPtr locks, uint maxStore, string version);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_free_context", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr FreeContext_32(IntPtr ctx);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_open_file_w", CharSet = CharSet.Unicode,
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenFile_32(IntPtr ctx, string fileName);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_open_document_with_stream",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenDocumentStream_32(IntPtr ctx, IntPtr stm);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_close", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseStream_32(IntPtr stm);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_close_document", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseDocument_32(IntPtr doc);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_count_pages", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CountPages_32(IntPtr doc);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_bound_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void BoundPage_32(IntPtr doc, IntPtr page, ref Rectangle bound);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_clear_pixmap_with_value",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern void ClearPixmap_32(IntPtr ctx, IntPtr pix, int byteValue);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_lookup_device_colorspace",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LookupDeviceColorSpace_32(IntPtr ctx, string colorspace);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_free_device", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeDevice_32(IntPtr dev);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_free_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreePage_32(IntPtr doc, IntPtr page);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_load_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LoadPage_32(IntPtr doc, int pageNumber);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_new_draw_device", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewDrawDevice_32(IntPtr ctx, IntPtr pix);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_new_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewPixmap_32(IntPtr ctx, IntPtr colorspace, int width, int height);

            [DllImport("LibMuPdf.dll", EntryPoint = "pdf_run_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RunPage_32(IntPtr doc, IntPtr page, IntPtr dev, ref Matrix transform,
                IntPtr cookie);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_drop_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern void DropPixmap_32(IntPtr ctx, IntPtr pix);

            [DllImport("LibMuPdf.dll", EntryPoint = "fz_pixmap_samples", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr GetSamples_32(IntPtr ctx, IntPtr pix);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_new_context_imp",
                CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr NewContext_64(IntPtr alloc, IntPtr locks, uint maxStore, string version);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_free_context", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr FreeContext_64(IntPtr ctx);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_open_file_w", CharSet = CharSet.Unicode,
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenFile_64(IntPtr ctx, string fileName);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_open_document_with_stream",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenDocumentStream_64(IntPtr ctx, IntPtr stm);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_close", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseStream_64(IntPtr stm);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_close_document",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseDocument_64(IntPtr doc);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_count_pages", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CountPages_64(IntPtr doc);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_bound_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void BoundPage_64(IntPtr doc, IntPtr page, ref Rectangle bound);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_clear_pixmap_with_value",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern void ClearPixmap_64(IntPtr ctx, IntPtr pix, int byteValue);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_lookup_device_colorspace",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LookupDeviceColorSpace_64(IntPtr ctx, string colorspace);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_free_device", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeDevice_64(IntPtr dev);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_free_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreePage_64(IntPtr doc, IntPtr page);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_load_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LoadPage_64(IntPtr doc, int pageNumber);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_new_draw_device",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewDrawDevice_64(IntPtr ctx, IntPtr pix);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_new_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewPixmap_64(IntPtr ctx, IntPtr colorspace, int width, int height);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "pdf_run_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RunPage_64(IntPtr doc, IntPtr page, IntPtr dev, ref Matrix transform,
                IntPtr cookie);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_drop_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern void DropPixmap_64(IntPtr ctx, IntPtr pix);

            [DllImport("LibMuPdf.x64.dll", EntryPoint = "fz_pixmap_samples",
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr GetSamples_64(IntPtr ctx, IntPtr pix);
        }
    }
}