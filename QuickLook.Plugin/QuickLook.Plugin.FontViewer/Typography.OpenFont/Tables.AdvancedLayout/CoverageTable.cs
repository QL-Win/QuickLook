//Apache2, 2016-present, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2

    public abstract class CoverageTable
    {
        public abstract int FindPosition(ushort glyphIndex);
        public abstract IEnumerable<ushort> GetExpandedValueIter();

#if DEBUG

#endif

        public static CoverageTable CreateFrom(BinaryReader reader, long beginAt)
        {
            reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
            ushort format = reader.ReadUInt16();
            switch (format)
            {
                default: throw new OpenFontNotSupportedException();
                case 1: return CoverageFmt1.CreateFrom(reader);
                case 2: return CoverageFmt2.CreateFrom(reader);
            }
        }

        public static CoverageTable[] CreateMultipleCoverageTables(long initPos, ushort[] offsets, BinaryReader reader)
        {
            CoverageTable[] results = new CoverageTable[offsets.Length];
            for (int i = 0; i < results.Length; ++i)
            {
                results[i] = CoverageTable.CreateFrom(reader, initPos + offsets[i]);
            }
            return results;
        }
    }

    public class CoverageFmt1 : CoverageTable
    {
        public static CoverageFmt1 CreateFrom(BinaryReader reader)
        {
            // CoverageFormat1 table: Individual glyph indices
            // Type      Name                     Description
            // uint16    CoverageFormat           Format identifier-format = 1
            // uint16    GlyphCount               Number of glyphs in the GlyphArray
            // uint16    GlyphArray[GlyphCount]   Array of glyph IDs — in numerical order

            ushort glyphCount = reader.ReadUInt16();
            ushort[] glyphs = Utils.ReadUInt16Array(reader, glyphCount);
            return new CoverageFmt1() { _orderedGlyphIdList = glyphs };
        }

        public override int FindPosition(ushort glyphIndex)
        {
            // "The glyph indices must be in numerical order for binary searching of the list"
            // (https://www.microsoft.com/typography/otspec/chapter2.htm#coverageFormat1)
            int n = Array.BinarySearch(_orderedGlyphIdList, glyphIndex);
            return n < 0 ? -1 : n;
        }
        public override IEnumerable<ushort> GetExpandedValueIter() { return _orderedGlyphIdList; }

#if DEBUG

        public override string ToString()
        {
            List<string> stringList = new List<string>();
            foreach (ushort g in _orderedGlyphIdList)
            {
                stringList.Add(g.ToString());
            }
            return "CoverageFmt1: " + string.Join(",", stringList.ToArray());
        }
#endif

        internal ushort[] _orderedGlyphIdList;
    }

    public class CoverageFmt2 : CoverageTable
    {
        public override int FindPosition(ushort glyphIndex)
        {
            // Ranges must be in glyph ID order, and they must be distinct, with no overlapping.
            // [...] quick calculation of the Coverage Index for any glyph in any range using the
            // formula: Coverage Index (glyphID) = startCoverageIndex + glyphID - startGlyphID.
            // (https://www.microsoft.com/typography/otspec/chapter2.htm#coverageFormat2)
            int n = Array.BinarySearch(_endIndices, glyphIndex);
            n = n < 0 ? ~n : n;
            if (n >= RangeCount || glyphIndex < _startIndices[n])
            {
                return -1;
            }
            return _coverageIndices[n] + glyphIndex - _startIndices[n];
        }

        public override IEnumerable<ushort> GetExpandedValueIter()
        {
            for (int i = 0; i < RangeCount; ++i)
            {
                for (ushort n = _startIndices[i]; n <= _endIndices[i]; ++n)
                {
                    yield return n;
                }
            }
        }
        public static CoverageFmt2 CreateFrom(BinaryReader reader)
        {
            // CoverageFormat2 table: Range of glyphs
            // Type      Name                     Description
            // uint16    CoverageFormat           Format identifier-format = 2
            // uint16    RangeCount               Number of RangeRecords
            // struct    RangeRecord[RangeCount]  Array of glyph ranges — ordered by StartGlyphID.
            //
            // RangeRecord
            // Type      Name                Description
            // uint16    StartGlyphID        First glyph ID in the range
            // uint16    EndGlyphID          Last glyph ID in the range
            // uint16    StartCoverageIndex  Coverage Index of first glyph ID in range

            ushort rangeCount = reader.ReadUInt16();
            ushort[] startIndices = new ushort[rangeCount];
            ushort[] endIndices = new ushort[rangeCount];
            ushort[] coverageIndices = new ushort[rangeCount];
            for (int i = 0; i < rangeCount; ++i)
            {
                startIndices[i] = reader.ReadUInt16();
                endIndices[i] = reader.ReadUInt16();
                coverageIndices[i] = reader.ReadUInt16();
            }

            return new CoverageFmt2()
            {
                _startIndices = startIndices,
                _endIndices = endIndices,
                _coverageIndices = coverageIndices
            };
        }

#if DEBUG

        public override string ToString()
        {
            List<string> stringList = new List<string>();
            for (int i = 0; i < RangeCount; ++i)
            {
                stringList.Add(string.Format("{0}-{1}", _startIndices[i], _endIndices[i]));
            }
            return "CoverageFmt2: " + string.Join(",", stringList.ToArray());
        }
#endif

        internal ushort[] _startIndices;
        internal ushort[] _endIndices;
        internal ushort[] _coverageIndices;

        private int RangeCount => _startIndices.Length;
    }

}
