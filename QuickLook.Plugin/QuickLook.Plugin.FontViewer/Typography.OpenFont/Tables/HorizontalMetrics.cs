//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.Collections.Generic;
using System.IO;
namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// hmtx
    /// </summary>
    class HorizontalMetrics : TableEntry
    {
        public const string _N = "hmtx";
        public override string Name => _N;
        //
        //https://docs.microsoft.com/en-us/typography/opentype/spec/hmtx
        // A font rendering engine must use the advanceWidths in the hmtx table for the advances of a CFF OFF font,
        //even though the CFF table specifies its own glyph widths.//***

        //Note that fonts in a Font Collection which share a CFF table may specify different advanceWidths in their hmtx table for a particular glyph index.
        //For any glyph, xmax and xmin are given in 'glyf' table, lsb and aw are given in 'hmtx' table. rsb is calculated as follows:
        //  rsb = aw - (lsb + xmax - xmin)
        //If pp1 and pp2 are phantom points used to control lsb and rsb, their initial position in x is calculated as follows:
        //  pp1 = xmin - lsb
        //  pp2 = pp1 + aw


        //NOTE: 
        //lsb=> left-side bearing
        //rsb=> right-side bearing
        //aw=> advance width

        readonly ushort[] _advanceWidths; //in font design unit
        readonly short[] _leftSideBearings;//lsb, in font design unit
        readonly int _numOfHMetrics;
        readonly int _numGlyphs;
        public HorizontalMetrics(ushort numOfHMetrics, ushort numGlyphs)
        {
            //The value numOfHMetrics comes from the 'hhea' table**   
            _advanceWidths = new ushort[numGlyphs];
            _leftSideBearings = new short[numGlyphs];
            _numOfHMetrics = numOfHMetrics;
            _numGlyphs = numGlyphs;
#if DEBUG
            if (numGlyphs < numOfHMetrics)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
        }

        public ushort GetAdvanceWidth(ushort glyphIndex) => _advanceWidths[glyphIndex];

        public short GetLeftSideBearing(ushort glyphIndex) => _leftSideBearings[glyphIndex];

        public void GetHMetric(ushort glyphIndex, out ushort advWidth, out short lsb)
        {
            advWidth = _advanceWidths[glyphIndex];
            lsb = _leftSideBearings[glyphIndex];
            //TODO: calculate other value?
        }
        protected override void ReadContentFrom(BinaryReader input)
        {
            //===============================================================================
            //1. hMetrics : have both advance width and leftSideBearing(lsb)
            //Paired advance width and left side bearing values for each glyph. 
            //The value numOfHMetrics comes from the 'hhea' table**
            //If the font is monospaced, only one entry need be in the array, 
            //but that entry is required. The last entry applies to all subsequent glyphs

            int gid = 0; //gid=> glyphIndex

            int numOfHMetrics = _numOfHMetrics;
            for (int i = 0; i < numOfHMetrics; i++)
            {
                _advanceWidths[gid] = input.ReadUInt16();
                _leftSideBearings[gid] = input.ReadInt16();

                gid++;//***
            }

            //===============================================================================
            //2. (only) LeftSideBearing:  (same advanced width (eg. monospace font), vary only left side bearing)
            //Here the advanceWidth is assumed to be the same as the advanceWidth for the last entry above.
            //The number of entries in this array is derived from numGlyphs (from 'maxp' table) minus numberOfHMetrics.

            int nEntries = _numGlyphs - numOfHMetrics;
            ushort advanceWidth = _advanceWidths[numOfHMetrics - 1];

            for (int i = 0; i < nEntries; i++)
            {
                _advanceWidths[gid] = advanceWidth;
                _leftSideBearings[gid] = input.ReadInt16();

                gid++;//***
            }
        }
    }
}
