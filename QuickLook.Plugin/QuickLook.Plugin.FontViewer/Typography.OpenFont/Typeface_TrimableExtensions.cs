//MIT, 2020-present, WinterDev

using System;
using System.IO;

namespace Typography.OpenFont.Trimmable
{

    //-------------------------
    //This is our extension***,
    //NOT in OpenFont spec
    //-------------------------
    //user can reload a new clone of glyphs with fewer detail
    //or restore a new clone of glyphs with full detail 

    //for unload and reload

    using Typography.OpenFont.Tables;

    public class RestoreTicket
    {
        internal RestoreTicket()
        {
        }
        internal string TypefaceName { get; set; }

        internal TableHeader[] Headers;
        internal bool HasTtf;
        internal bool HasCff;
        internal bool HasSvg;
        internal bool HasBitmapSource;

        internal bool ControlValues;
        internal bool PrepProgramBuffer;
        internal bool FpgmProgramBuffer;
        internal bool CPALTable;
        internal bool COLRTable;
        internal bool GaspTable;
    }

    public enum TrimMode
    {
        /// <summary>
        /// No trim, full glyph instruction
        /// </summary>
        No, //default
        /// <summary>
        /// only essential info for glyph layout
        /// </summary>
        EssentailLayoutInfo,
        /// <summary>
        /// restore again
        /// </summary>
        Restored,
    }

    public static class TypefaceExtensions
    {
        public static RestoreTicket TrimDown(this Typeface typeface) => typeface.TrimDownAndRemoveGlyphBuildingDetail();
        public static TrimMode GetTrimMode(this Typeface typeface) => typeface._typefaceTrimMode;
        public static bool IsTrimmed(this Typeface typeface) => typeface._typefaceTrimMode == TrimMode.EssentailLayoutInfo;

        public static void RestoreUp(this Typeface typeface, RestoreTicket ticket, OpenFontReader openFontReader, Stream fontStream)
        {
            if (typeface.IsTrimmed())
            {
                openFontReader.Read(typeface, ticket, fontStream);
            }
        }
        public static void RestoreUp(this Typeface typeface, RestoreTicket ticket, Stream fontStream)
        {
            //use default opent font reader
            RestoreUp(typeface, ticket, new OpenFontReader(), fontStream);
        } 
    }
}


namespace Typography.OpenFont
{
    using Typography.OpenFont.Tables;
    using Typography.OpenFont.Trimmable;

    //-------------------------
    //This is our extension***,
    //NOT in OpenFont spec
    //-------------------------
    //user can reload a new clone of glyphs with fewer detail
    //or restore a new clone of glyphs with full detail 

    partial class Typeface
    {
        internal TrimMode _typefaceTrimMode;


        internal RestoreTicket TrimDownAndRemoveGlyphBuildingDetail()
        {
            switch (_typefaceTrimMode)
            {
                default: throw new OpenFontNotSupportedException();
                case TrimMode.EssentailLayoutInfo: return null;//same mode
                case TrimMode.Restored:
                case TrimMode.No:
                    {
                        RestoreTicket ticket = new RestoreTicket();
                        ticket.TypefaceName = Name;
                        ticket.Headers = _tblHeaders; //a copy 

                        //FROM:GlyphLoadingMode.Full => TO: GlyphLoadingMode.EssentailLayoutInfo 

                        ticket.HasTtf = _hasTtfOutline;

                        //cache glyph name before unload 
                        if (_cff1FontSet != null)
                        {
                            ticket.HasCff = true;
                            UpdateCff1FontSetNamesCache();//***
                            _cff1FontSet = null;
                        }

                        //1.Ttf and Otf => clone each glyphs in NO building
                        Glyph[] newClones = new Glyph[_glyphs.Length];
                        for (int i = 0; i < newClones.Length; ++i)
                        {
                            newClones[i] = Glyph.Clone_NO_BuildingInstructions(_glyphs[i]);
                        }
                        _glyphs = newClones;

                        //and since glyph has no building instructions in this mode
                        //so  ... 

                        ticket.ControlValues = ControlValues != null;
                        ControlValues = null;

                        ticket.PrepProgramBuffer = PrepProgramBuffer != null;
                        PrepProgramBuffer = null;

                        ticket.FpgmProgramBuffer = FpgmProgramBuffer != null;
                        FpgmProgramBuffer = null;

                        ticket.CPALTable = CPALTable != null;
                        CPALTable = null;

                        ticket.COLRTable = COLRTable != null;
                        COLRTable = null;

                        ticket.GaspTable = GaspTable != null;
                        GaspTable = null;

                        // 
                        //3. Svg=> remove SvgTable
                        if (_svgTable != null)
                        {
                            ticket.HasSvg = true;
                            _svgTable.UnloadSvgData();
                            _svgTable = null;
                        }

                        //4. Bitmap Font => remove embeded bitmap data
                        if (_bitmapFontGlyphSource != null)
                        {
                            ticket.HasBitmapSource = true;
                            _bitmapFontGlyphSource.UnloadCBDT();
                        }


                        _typefaceTrimMode = TrimMode.EssentailLayoutInfo;

                        return ticket;
                    }
            }
        }


        internal bool CompareOriginalHeadersWithNewlyLoadOne(TableHeader[] others)
        {
            if (_tblHeaders != null && others != null &&
                _tblHeaders.Length == others.Length)
            {
                for (int i = 0; i < _tblHeaders.Length; ++i)
                {
                    TableHeader a = _tblHeaders[i];
                    TableHeader b = others[i];

                    if (a.Tag != b.Tag ||
                       a.Offset != b.Offset ||
                       a.Length != b.Length ||
                       a.CheckSum != b.CheckSum)
                    {
#if DEBUG
                        System.Diagnostics.Debugger.Break();
#endif

                        return false;
                    }
                }
                //pass all
                return true;
            }
            return false;
        }
    }

}


