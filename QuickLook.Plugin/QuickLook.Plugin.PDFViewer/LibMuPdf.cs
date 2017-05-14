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

            NativeMethods.BoundPage(document, page, ref pageBound);

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
            pix = NativeMethods.NewPixmap(context, NativeMethods.LookupDeviceColorSpace(context, "DeviceRGB"), width,
                height);
            // sets white color as the background color of the pixmap
            NativeMethods.ClearPixmap(context, pix, 0xFF);

            // creates a drawing device
            dev = NativeMethods.NewDrawDevice(context, pix);
            // draws the page on the device created from the pixmap
            NativeMethods.RunPage(document, page, dev, ref ctm, IntPtr.Zero);

            NativeMethods.FreeDevice(dev); // frees the resources consumed by the device
            dev = IntPtr.Zero;

            // creates a colorful bitmap of the same size of the pixmap
            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var imageData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            unsafe
            {
                // converts the pixmap data to Bitmap data
                var ptrSrc = (byte*) NativeMethods.GetSamples(context, pix); // gets the rendered data from the pixmap
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
            NativeMethods.DropPixmap(context, pix);

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
            private const uint FZ_STORE_DEFAULT = 256 << 20;
            private const string DLL = "libmupdf.dll";
            // please modify the version number to match the FZ_VERSION definition in "fitz\version.h" file
            private const string MuPDFVersion = "1.6";

            [DllImport(DLL, EntryPoint = "fz_new_context_imp", CallingConvention = CallingConvention.Cdecl)]
            private static extern IntPtr NewContext(IntPtr alloc, IntPtr locks, uint max_store, string version);

            public static IntPtr NewContext()
            {
                return NewContext(IntPtr.Zero, IntPtr.Zero, FZ_STORE_DEFAULT, MuPDFVersion);
            }

            [DllImport(DLL, EntryPoint = "fz_free_context", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr FreeContext(IntPtr ctx);

            [DllImport(DLL, EntryPoint = "fz_open_file_w", CharSet = CharSet.Unicode,
                CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenFile(IntPtr ctx, string fileName);

            [DllImport(DLL, EntryPoint = "pdf_open_document_with_stream", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr OpenDocumentStream(IntPtr ctx, IntPtr stm);

            [DllImport(DLL, EntryPoint = "fz_close", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseStream(IntPtr stm);

            [DllImport(DLL, EntryPoint = "pdf_close_document", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr CloseDocument(IntPtr doc);

            [DllImport(DLL, EntryPoint = "pdf_count_pages", CallingConvention = CallingConvention.Cdecl)]
            public static extern int CountPages(IntPtr doc);

            [DllImport(DLL, EntryPoint = "pdf_bound_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void BoundPage(IntPtr doc, IntPtr page, ref Rectangle bound);

            [DllImport(DLL, EntryPoint = "fz_clear_pixmap_with_value", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ClearPixmap(IntPtr ctx, IntPtr pix, int byteValue);

            [DllImport(DLL, EntryPoint = "fz_lookup_device_colorspace", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LookupDeviceColorSpace(IntPtr ctx, string colorspace);

            [DllImport(DLL, EntryPoint = "fz_free_device", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeDevice(IntPtr dev);

            [DllImport(DLL, EntryPoint = "pdf_free_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreePage(IntPtr doc, IntPtr page);

            [DllImport(DLL, EntryPoint = "pdf_load_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr LoadPage(IntPtr doc, int pageNumber);

            [DllImport(DLL, EntryPoint = "fz_new_draw_device", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewDrawDevice(IntPtr ctx, IntPtr pix);

            [DllImport(DLL, EntryPoint = "fz_new_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr NewPixmap(IntPtr ctx, IntPtr colorspace, int width, int height);

            [DllImport(DLL, EntryPoint = "pdf_run_page", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RunPage(IntPtr doc, IntPtr page, IntPtr dev, ref Matrix transform, IntPtr cookie);

            [DllImport(DLL, EntryPoint = "fz_drop_pixmap", CallingConvention = CallingConvention.Cdecl)]
            public static extern void DropPixmap(IntPtr ctx, IntPtr pix);

            [DllImport(DLL, EntryPoint = "fz_pixmap_samples", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr GetSamples(IntPtr ctx, IntPtr pix);
        }
    }
}