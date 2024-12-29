//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev
using System;
using System.Collections.Generic;
using Typography.OpenFont.Tables;

namespace Typography.OpenFont
{


    public partial class Typeface
    {

        //TODO: implement vertical metrics
        HorizontalMetrics _hMetrics;
        NameEntry _nameEntry;
        Glyph[] _glyphs;
        CFF.Cff1FontSet _cff1FontSet;
        TableHeader[] _tblHeaders;
        bool _hasTtfOutline;
        bool _hasCffData;
        internal bool _useTypographicMertic;
#if DEBUG 
        static int s_dbugTotalId;
        public readonly int dbugId = ++s_dbugTotalId;
#endif

        internal Typeface()
        {
            //blank typefaces 
#if DEBUG
            if (dbugId == 5)
            {

            }
#endif
        }

        internal void SetTableEntryCollection(TableHeader[] headers) => _tblHeaders = headers;

        internal void SetBasicTypefaceTables(OS2Table os2Table,
             NameEntry nameEntry,
             Head head,
             HorizontalMetrics horizontalMetrics)
        {
            OS2Table = os2Table;
            _nameEntry = nameEntry;
            Head = head;
            Bounds = head.Bounds;
            UnitsPerEm = head.UnitsPerEm;
            _hMetrics = horizontalMetrics;
        }

        internal Head Head { get; set; }

        internal void SetTtfGlyphs(Glyph[] glyphs)
        {
            _glyphs = glyphs;
            _hasTtfOutline = true;
        }
        internal void SetBitmapGlyphs(Glyph[] glyphs, BitmapFontGlyphSource bitmapFontGlyphSource)
        {
            _glyphs = glyphs;
            _bitmapFontGlyphSource = bitmapFontGlyphSource;
        }
        internal void SetCffFontSet(CFF.Cff1FontSet cff1FontSet)
        {
            _cff1FontSet = cff1FontSet;
            _hasCffData = true;

            Glyph[] exisitingGlyphs = _glyphs;

            _glyphs = cff1FontSet._fonts[0]._glyphs; //TODO: review _fonts[0]

            if (exisitingGlyphs != null)
            {
                //
#if DEBUG
                if (_glyphs.Length != exisitingGlyphs.Length)
                {
                    throw new OpenFontNotSupportedException();
                }
#endif
                for (int i = 0; i < exisitingGlyphs.Length; ++i)
                {
                    Glyph.CopyExistingGlyphInfo(exisitingGlyphs[i], _glyphs[i]);
                }
            }

        }

        public Languages Languages { get; } = new Languages();
        /// <summary>
        /// control values in Font unit
        /// </summary>
        internal int[] ControlValues { get; set; }
        internal byte[] PrepProgramBuffer { get; set; }
        internal byte[] FpgmProgramBuffer { get; set; }

        internal MaxProfile MaxProfile { get; set; }
        internal Cmap CmapTable { get; set; }
        internal Kern KernTable { get; set; }
        internal Gasp GaspTable { get; set; }
        internal HorizontalHeader HheaTable { get; set; }
        public OS2Table OS2Table { get; set; }
        //
        public bool HasPrepProgramBuffer => PrepProgramBuffer != null;


        /// <summary>
        /// actual font filename (optional)
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// OS2 sTypoAscender/HheaTable.Ascent, in font designed unit
        /// </summary>         
        public short Ascender => _useTypographicMertic ? OS2Table.sTypoAscender : HheaTable.Ascent;

        /// <summary>
        /// OS2 sTypoDescender, in font designed unit
        /// </summary>
        public short Descender => _useTypographicMertic ? OS2Table.sTypoDescender : HheaTable.Descent;
        /// <summary>
        /// OS2 usWinAscender
        /// </summary>
        public ushort ClipedAscender => OS2Table.usWinAscent;
        /// <summary>
        /// OS2 usWinDescender
        /// </summary>
        public ushort ClipedDescender => OS2Table.usWinDescent;

        /// <summary>
        /// OS2 Linegap
        /// </summary>
        public short LineGap => _useTypographicMertic ? OS2Table.sTypoLineGap : HheaTable.LineGap;
        //The typographic line gap for this font.
        //Remember that this is not the same as the LineGap value in the 'hhea' table, 
        //which Apple defines in a far different manner.
        //The suggested usage for sTypoLineGap is 
        //that it be used in conjunction with unitsPerEm 
        //to compute a typographically correct default line spacing.
        //
        //Typical values average 7 - 10 % of units per em.
        //The goal is to free applications from Macintosh or Windows - specific metrics
        //which are constrained by backward compatability requirements
        //(see chapter, “Recommendations for OpenType Fonts”).
        //These new metrics, when combined with the character design widths,
        //will allow applications to lay out documents in a typographically correct and portable fashion. 
        //These metrics will be exposed through Windows APIs.
        //Macintosh applications will need to access the 'sfnt' resource and 
        //parse it to extract this data from the “OS / 2” table
        //(unless Apple exposes the 'OS/2' table through a new API)
        //---------------

        public string Name => _nameEntry.FontName;
        public string FontSubFamily => _nameEntry.FontSubFamily;
        public string PostScriptName => _nameEntry.PostScriptName;
        public string VersionString => _nameEntry.VersionString;
        public string UniqueFontIden => _nameEntry.UniqueFontIden;

        internal NameEntry NameEntry => _nameEntry;

        public int GlyphCount => _glyphs.Length;
        /// <summary>
        /// find glyph index by codepoint
        /// </summary>
        /// <param name="codepoint"></param>
        /// <param name="nextCodepoint"></param>
        /// <returns></returns>

        public ushort GetGlyphIndex(int codepoint, int nextCodepoint, out bool skipNextCodepoint)
        {
            return CmapTable.GetGlyphIndex(codepoint, nextCodepoint, out skipNextCodepoint);
        }
        public ushort GetGlyphIndex(int codepoint)
        {
            return CmapTable.GetGlyphIndex(codepoint, 0, out bool skipNextCodepoint);
        }
        public void CollectUnicode(List<uint> unicodes)
        {
            CmapTable.CollectUnicode(unicodes);
        }
        public void CollectUnicode(int platform, List<uint> unicodes, List<ushort> glyphIndexList)
        {
            CmapTable.CollectUnicode(platform, unicodes, glyphIndexList);
        }
        public Glyph GetGlyphByName(string glyphName) => GetGlyph(GetGlyphIndexByName(glyphName));

        Dictionary<string, ushort> _cachedGlyphDicByName;

        void UpdateCff1FontSetNamesCache()
        {
            if (_cff1FontSet != null && _cachedGlyphDicByName == null)
            {
                //create cache data
                _cachedGlyphDicByName = new Dictionary<string, ushort>();
                for (int i = 1; i < _glyphs.Length; ++i)
                {
                    Glyph glyph = _glyphs[i];

                    if (glyph._cff1GlyphData != null && glyph._cff1GlyphData.Name != null)
                    {
                        _cachedGlyphDicByName.Add(glyph._cff1GlyphData.Name, (ushort)i);
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Cff unknown glyphname");
#endif
                    }
                }
            }
        }
        public ushort GetGlyphIndexByName(string glyphName)
        {
            if (glyphName == null) return 0;

            if (_cff1FontSet != null && _cachedGlyphDicByName == null)
            {
                //we create a dictionary 
                //create cache data
                _cachedGlyphDicByName = new Dictionary<string, ushort>();
                for (int i = 1; i < _glyphs.Length; ++i)
                {
                    Glyph glyph = _glyphs[i];
                    if (glyph._cff1GlyphData.Name != null)
                    {
                        _cachedGlyphDicByName.Add(glyph._cff1GlyphData.Name, (ushort)i);
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("Cff unknown glyphname");
#endif
                    }
                }
                return _cachedGlyphDicByName.TryGetValue(glyphName, out ushort glyphIndex) ? glyphIndex : (ushort)0;
            }
            else if (PostTable != null)
            {
                if (PostTable.Version == 2)
                {
                    return PostTable.GetGlyphIndex(glyphName);
                }
                else
                {
                    //check data from adobe glyph list 
                    //from the unicode value
                    //select glyph index   

                    //we use AdobeGlyphList
                    //from https://github.com/adobe-type-tools/agl-aglfn/blob/master/glyphlist.txt

                    //but user can provide their own map here...

                    return GetGlyphIndex(AdobeGlyphList.GetUnicodeValueByGlyphName(glyphName));
                }
            }
            return 0;
        }

        public IEnumerable<GlyphNameMap> GetGlyphNameIter()
        {
            if (_cachedGlyphDicByName == null && _cff1FontSet != null)
            {
                UpdateCff1FontSetNamesCache();
            }

            if (_cachedGlyphDicByName != null)
            {
                //iter from here
                foreach (var kv in _cachedGlyphDicByName)
                {
                    yield return new GlyphNameMap(kv.Value, kv.Key);
                }
            }
            else if (PostTable.Version == 2)
            {
                foreach (var kp in PostTable.GlyphNames)
                {
                    yield return new GlyphNameMap(kp.Key, kp.Value);
                }
            }
        }
        public Glyph GetGlyph(ushort glyphIndex)
        {
            if (glyphIndex < _glyphs.Length)
            {
                return _glyphs[glyphIndex];
            }
            else
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("found unknown glyph:" + glyphIndex);
#endif
                return _glyphs[0]; //return empty glyph?;
            }
        }

        public ushort GetAdvanceWidthFromGlyphIndex(ushort glyphIndex) => _hMetrics.GetAdvanceWidth(glyphIndex);
        public short GetLeftSideBearing(ushort glyphIndex) => _hMetrics.GetLeftSideBearing(glyphIndex);
        public short GetKernDistance(ushort leftGlyphIndex, ushort rightGlyphIndex)
        {
            //DEPRECATED -> use OpenFont layout instead
            return this.KernTable.GetKerningDistance(leftGlyphIndex, rightGlyphIndex);
        }
        //
        public Bounds Bounds { get; private set; }
        public ushort UnitsPerEm { get; private set; }
        public short UnderlinePosition => PostTable.UnderlinePosition; //TODO: review here
        //

        const int s_pointsPerInch = 72;//point per inch, fix?        

        /// <summary>
        /// default dpi
        /// </summary>
        public static uint DefaultDpi { get; set; } = 96;

        /// <summary>
        /// convert from point-unit value to pixel value
        /// </summary>
        /// <param name="targetPointSize"></param>
        /// <param name="resolution">dpi</param>
        /// <returns></returns>
        public static float ConvPointsToPixels(float targetPointSize, int resolution = -1)
        {
            //http://stackoverflow.com/questions/139655/convert-pixels-to-points
            //points = pixels * 72 / 96
            //------------------------------------------------
            //pixels = targetPointSize * 96 /72
            //pixels = targetPointSize * resolution / pointPerInch

            if (resolution < 0)
            {
                //use current DefaultDPI
                resolution = (int)DefaultDpi;
            }

            return targetPointSize * resolution / s_pointsPerInch;
        }
        /// <summary>
        /// calculate scale to target pixel size based on current typeface's UnitsPerEm
        /// </summary>
        /// <param name="targetPixelSize">target font size in point unit</param>
        /// <returns></returns>
        public float CalculateScaleToPixel(float targetPixelSize)
        {
            //1. return targetPixelSize / UnitsPerEm
            return targetPixelSize / this.UnitsPerEm;
        }
        /// <summary>
        ///  calculate scale to target pixel size based on current typeface's UnitsPerEm
        /// </summary>
        /// <param name="targetPointSize">target font size in point unit</param>
        /// <param name="resolution">dpi</param>
        /// <returns></returns>
        public float CalculateScaleToPixelFromPointSize(float targetPointSize, int resolution = -1)
        {
            //1. var sizeInPixels = ConvPointsToPixels(sizeInPointUnit);
            //2. return sizeInPixels / UnitsPerEm

            if (resolution < 0)
            {
                //use current DefaultDPI
                resolution = (int)DefaultDpi;
            }
            return (targetPointSize * resolution / s_pointsPerInch) / this.UnitsPerEm;
        }


        internal BASE BaseTable { get; set; }
        internal GDEF GDEFTable { get; set; }

        public COLR COLRTable { get; private set; }
        public CPAL CPALTable { get; private set; }

        internal bool HasColorAndPal { get; private set; }
        internal void SetColorAndPalTable(COLR colr, CPAL cpal)
        {
            COLRTable = colr;
            CPALTable = cpal;
            HasColorAndPal = colr != null;
        }

        public GPOS GPOSTable { get; internal set; }
        public GSUB GSUBTable { get; internal set; }

        internal void LoadOpenFontLayoutInfo(GDEF gdefTable, GSUB gsubTable, GPOS gposTable, BASE baseTable, COLR colrTable, CPAL cpalTable)
        {

            //***
            this.GDEFTable = gdefTable;
            this.GSUBTable = gsubTable;
            this.GPOSTable = gposTable;
            this.BaseTable = baseTable;
            this.COLRTable = colrTable;
            this.CPALTable = cpalTable;
            //---------------------------
            //fill glyph definition            
            if (gdefTable != null)
            {
                gdefTable.FillGlyphData(_glyphs);
            }
        }

        internal PostTable PostTable { get; set; }
        internal bool _evalCffGlyphBounds;
        public bool IsCffFont => _hasCffData;

        //Math Table

        MathGlyphs.MathGlyphInfo[] _mathGlyphInfos;
        internal MathTable _mathTable;
        //
        public MathGlyphs.MathConstants MathConsts => _mathTable?._mathConstTable;
        internal void LoadMathGlyphInfos(MathGlyphs.MathGlyphInfo[] mathGlyphInfos)
        {
            _mathGlyphInfos = mathGlyphInfos;
            if (mathGlyphInfos != null)
            {
                //fill to original glyph?
                for (int glyphIndex = 0; glyphIndex < _glyphs.Length; ++glyphIndex)
                {
                    _glyphs[glyphIndex].MathGlyphInfo = mathGlyphInfos[glyphIndex];
                }
            }
        }
        public MathGlyphs.MathGlyphInfo GetMathGlyphInfo(ushort glyphIndex) => _mathGlyphInfos[glyphIndex];

        //-------------------------
        //svg and bitmap font
        SvgTable _svgTable;

        internal bool HasSvgTable { get; private set; }
        internal void SetSvgTable(SvgTable svgTable)
        {
            HasSvgTable = (_svgTable = svgTable) != null;
        }
        public void ReadSvgContent(ushort glyphIndex, System.Text.StringBuilder output) => _svgTable?.ReadSvgContent(glyphIndex, output);


        internal BitmapFontGlyphSource _bitmapFontGlyphSource;
        public bool IsBitmapFont => _bitmapFontGlyphSource != null;
        public void ReadBitmapContent(Glyph glyph, System.IO.Stream output)
        {
            _bitmapFontGlyphSource.CopyBitmapContent(glyph, output);
        }

        /// <summary>
        /// undate lang info
        /// </summary>
        /// <param name="metaTable"></param>
        internal void UpdateLangs(Meta metaTable) => Languages.Update(OS2Table, metaTable, CmapTable, this.GSUBTable, this.GPOSTable);



        internal ushort _whitespaceWidth; //common used value

        internal void UpdateFrequentlyUsedValues()
        {
            //whitespace
            ushort whitespace_glyphIndex = this.GetGlyphIndex(' ');
            if (whitespace_glyphIndex > 0)
            {
                _whitespaceWidth = this.GetAdvanceWidthFromGlyphIndex(whitespace_glyphIndex);
            }
        }

#if DEBUG
        public override string ToString() => Name;
#endif

    }

    public interface IGlyphPositions
    {
        int Count { get; }

        GlyphClassKind GetGlyphClassKind(int index);
        void AppendGlyphOffset(int index, short appendOffsetX, short appendOffsetY);
        void AppendGlyphAdvance(int index, short appendAdvX, short appendAdvY);

        ushort GetGlyph(int index, out short advW);
        ushort GetGlyph(int index, out ushort inputOffset, out short offsetX, out short offsetY, out short advW);
        //
        void GetOffset(int index, out short offsetX, out short offsetY);
    }


    public static class StringUtils
    {
        public static void FillWithCodepoints(List<int> codepoints, char[] str, int startAt = 0, int len = -1)
        {

            if (len == -1) len = str.Length;
            // this is important!
            // -----------------------
            //  from @samhocevar's PR: (https://github.com/LayoutFarm/Typography/pull/56/commits/b71c7cf863531ebf5caa478354d3249bde40b96e)
            // In many places, "char" is not a valid type to handle characters, because it
            // only supports 16 bits.In order to handle the full range of Unicode characters,
            // we need to use "int".
            // This allows characters such as 🙌 or 𐐷 or to be treated as single codepoints even
            // though they are encoded as two "char"s in a C# string.
            for (int i = 0; i < len; ++i)
            {
                char ch = str[startAt + i];
                int codepoint = ch;
                if (char.IsHighSurrogate(ch) && i + 1 < len)
                {
                    char nextCh = str[startAt + i + 1];
                    if (char.IsLowSurrogate(nextCh))
                    {
                        ++i;
                        codepoint = char.ConvertToUtf32(ch, nextCh);
                    }
                }
                codepoints.Add(codepoint);
            }
        }
        public static IEnumerable<int> GetCodepoints(char[] str, int startAt = 0, int len = -1)
        {
            if (len == -1) len = str.Length;
            // this is important!
            // -----------------------
            //  from @samhocevar's PR: (https://github.com/LayoutFarm/Typography/pull/56/commits/b71c7cf863531ebf5caa478354d3249bde40b96e)
            // In many places, "char" is not a valid type to handle characters, because it
            // only supports 16 bits.In order to handle the full range of Unicode characters,
            // we need to use "int".
            // This allows characters such as 🙌 or 𐐷 or to be treated as single codepoints even
            // though they are encoded as two "char"s in a C# string.
            for (int i = 0; i < len; ++i)
            {
                char ch = str[startAt + i];
                int codepoint = ch;
                if (char.IsHighSurrogate(ch) && i + 1 < len)
                {
                    char nextCh = str[startAt + i + 1];
                    if (char.IsLowSurrogate(nextCh))
                    {
                        ++i;
                        codepoint = char.ConvertToUtf32(ch, nextCh);
                    }
                }
                yield return codepoint;
            }
        }
    }




    public readonly struct GlyphNameMap
    {
        public readonly ushort glyphIndex;
        public readonly string glyphName;
        public GlyphNameMap(ushort glyphIndex, string glyphName)
        {
            this.glyphIndex = glyphIndex;
            this.glyphName = glyphName;
        }
    }


}




