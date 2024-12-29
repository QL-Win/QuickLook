//Apache2, 2017-present, WinterDev 

using System;
using System.Collections.Generic;
using System.IO;
namespace Typography.OpenFont.Tables
{
    class SvgTable : TableEntry
    {
        public const string _N = "SVG "; //with 1 whitespace ***
        public override string Name => _N;
        //
        //https://docs.microsoft.com/en-us/typography/opentype/spec/svg
        //OpenType fonts with either TrueType or CFF outlines may also contain an optional 'SVG ' table, 
        //which allows some or all glyphs in the font to be defined with color, gradients, or animation.


        Dictionary<ushort, int> _dicSvgEntries;
        SvgDocumentEntry[] _entries; //TODO: review again
        protected override void ReadContentFrom(BinaryReader reader)
        {
            long svgTableStartAt = reader.BaseStream.Position;
            //SVG Main Header
            //Type      Name                Description
            //uint16    version             Table version(starting at 0). Set to 0.
            //Offset32  svgDocIndexOffset   Offset(relative to the start of the SVG table) to the SVG Documents Index.Must be non - zero.
            //uint32    reserved            Set to 0.
            //-----------
            ushort version = reader.ReadUInt16();
            uint offset32 = reader.ReadUInt32();
            uint reserved = reader.ReadUInt32();
            //-------


            //-------
            //SVG Document Index
            //The SVG Document Index is a set of SVG documents, each of which defines one or more glyph descriptions.
            //Type                          Name               Description
            //uint16                        numEntries         Number of SVG Document Index Entries.Must be non - zero.
            //SVG Document Index Entry      entries[numEntries] Array of SVG Document Index Entries.
            //        
            long svgDocIndexStartAt = svgTableStartAt + offset32;
            reader.BaseStream.Seek(svgDocIndexStartAt, SeekOrigin.Begin);
            //
            ushort numEntries = reader.ReadUInt16();
            //
            //SVG Document Index Entry
            //Each SVG Document Index Entry specifies a range[startGlyphID, endGlyphID], inclusive,
            //of glyph IDs and the location of its associated SVG document in the SVG table.
            //Type      Name            Description
            //uint16    startGlyphID    The first glyph ID in the range described by this index entry.
            //uint16    endGlyphID      The last glyph ID in the range described by this index entry. Must be >= startGlyphID.
            //Offset32  svgDocOffset    Offset from the beginning of the SVG Document Index to an SVG document.Must be non - zero.
            //uint32    svgDocLength    Length of the SVG document.Must be non - zero.

            //Index entries must be arranged in order of increasing startGlyphID.
            //
            //...this specification requires that the SVG documents be either plain-text or gzip-encoded [RFC1952]. 
            //The encoding of the (uncompressed) SVG document must be UTF-8. 
            //In both cases, svgDocLength encodes the length of the encoded data, not the decoded document.

            _entries = new SvgDocumentEntry[numEntries];
            for (int i = 0; i < numEntries; ++i)
            {
                _entries[i] = new SvgDocumentEntry()
                {
                    startGlyphID = reader.ReadUInt16(),
                    endGlyphID = reader.ReadUInt16(),
                    svgDocOffset = reader.ReadUInt32(),
                    svgDocLength = reader.ReadUInt32()
                };
            }

            //TODO: review lazy load
            for (int i = 0; i < numEntries; ++i)
            {
                //read data
                SvgDocumentEntry entry = _entries[i];

                if (entry.endGlyphID - entry.startGlyphID > 0)
                {
                    //TODO review here again
                    throw new System.NotSupportedException();
                }

                reader.BaseStream.Seek(svgDocIndexStartAt + entry.svgDocOffset, SeekOrigin.Begin);

                if (entry.svgDocLength == 0)
                {
                    throw new System.NotSupportedException();
                }

                //
                byte[] svgData = reader.ReadBytes((int)entry.svgDocLength);
                _entries[i].svgBuffer = svgData;

#if DEBUG
                if (svgData.Length == 0) { throw new OpenFontNotSupportedException(); }
#endif

                if (svgData[0] == (byte)'<')
                {
                    //should be plain-text
#if DEBUG
                    //string svgDataString = System.Text.Encoding.UTF8.GetString(svgData);
                    //dbugSaveAsHtml("svg" + i + ".html", svgDataString);
#endif
                }
                else
                {
                    _entries[i].compressed = true;
                }
            }
        }

        public void UnloadSvgData()
        {
            if (_dicSvgEntries != null)
            {
                _dicSvgEntries.Clear();
                _dicSvgEntries = null;
            }

            _entries = null;
        }

        public bool ReadSvgContent(ushort glyphIndex, System.Text.StringBuilder outputStBuilder)
        {
            if (_dicSvgEntries == null)
            {
                //create index
                _dicSvgEntries = new Dictionary<ushort, int>();
                for (int i = 0; i < _entries.Length; ++i)
                {
                    _dicSvgEntries.Add(_entries[i].startGlyphID, i);
                }
            }

            if (_dicSvgEntries.TryGetValue(glyphIndex, out int entryIndex))
            {
                SvgDocumentEntry docEntry = _entries[entryIndex];

                if (docEntry.compressed)
                {
                    throw new OpenFontNotSupportedException();
                    //TODO: decompress this
                }

                outputStBuilder.Append(System.Text.Encoding.UTF8.GetString(docEntry.svgBuffer));
                return true;
            }
            return false;
        }
#if DEBUG
        static void dbugSaveAsHtml(string filename, string originalGlyphSvg)
        {
            //xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\"
            //test save the svg
            //save as html document for test 
            System.Text.StringBuilder stbuilder = new System.Text.StringBuilder();
            stbuilder.Append("<html><body>");

            //TODO: add exact SVG reader here
            //to view in WebBrowser -> we do Y-flip
            string modified = originalGlyphSvg.Replace("xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\">",
                 "xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"800\" height=\"1600\"><g transform=\"scale(1,-1)\">"
                 ).Replace("</svg>", "</g></svg>");
            stbuilder.Append(modified);
            stbuilder.Append("</body></html>");
            File.WriteAllText(filename, stbuilder.ToString());
        }
#endif
        struct SvgDocumentEntry
        {
            public ushort startGlyphID;
            public ushort endGlyphID;
            public uint svgDocOffset;
            public uint svgDocLength;

            public byte[] svgBuffer;
            public bool compressed;

#if DEBUG
            public override string ToString()
            {
                return "startGlyphID:" + startGlyphID + "," +
                        "endGlyphID:" + endGlyphID + "," +
                        "svgDocOffset:" + svgDocOffset + "," +
                        "svgDocLength:" + svgDocLength;
            }
#endif
        }

    }
}