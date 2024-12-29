//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables.BitmapFonts
{

    //from 
    //https://docs.microsoft.com/en-us/typography/opentype/spec/eblc
    //https://docs.microsoft.com/en-us/typography/opentype/spec/ebdt
    //https://docs.microsoft.com/en-us/typography/opentype/spec/cblc
    //https://docs.microsoft.com/en-us/typography/opentype/spec/cbdt

    struct SbitLineMetrics
    {
        public sbyte ascender;
        public sbyte descender;
        public byte widthMax;

        public sbyte caretSlopeNumerator;
        public sbyte caretSlopeDenominator;
        public sbyte caretOffset;

        public sbyte minOriginSB;
        public sbyte minAdvanceSB;

        public sbyte maxBeforeBL;
        public sbyte minAfterBL;

        public sbyte pad1;
        public sbyte pad2;

    }
    class BitmapSizeTable
    {
        public uint indexSubTableArrayOffset;
        public uint indexTablesSize;
        public uint numberOfIndexSubTables;
        public uint colorRef;

        public SbitLineMetrics hori;
        public SbitLineMetrics vert;

        public ushort startGlyphIndex;
        public ushort endGlyphIndex;

        public byte ppemX;
        public byte ppemY;
        public byte bitDepth;

        //bitDepth
        //Value   Description
        //1	      black/white
        //2	      4 levels of gray
        //4	      16 levels of gray
        //8	      256 levels of gray

        public sbyte flags;

        //-----
        //reconstructed 
        public IndexSubTableBase[] indexSubTables;
        //
        static void ReadSbitLineMetrics(BinaryReader reader, ref SbitLineMetrics lineMetric)
        {
            //read 12 bytes ...

            lineMetric.ascender = (sbyte)reader.ReadByte();
            lineMetric.descender = (sbyte)reader.ReadByte();
            lineMetric.widthMax = reader.ReadByte();

            lineMetric.caretSlopeNumerator = (sbyte)reader.ReadByte();
            lineMetric.caretSlopeDenominator = (sbyte)reader.ReadByte();
            lineMetric.caretOffset = (sbyte)reader.ReadByte();

            lineMetric.minOriginSB = (sbyte)reader.ReadByte();
            lineMetric.minAdvanceSB = (sbyte)reader.ReadByte();

            lineMetric.maxBeforeBL = (sbyte)reader.ReadByte();
            lineMetric.minAfterBL = (sbyte)reader.ReadByte();

            lineMetric.pad1 = (sbyte)reader.ReadByte();
            lineMetric.pad2 = (sbyte)reader.ReadByte();
        }

        public static BitmapSizeTable ReadBitmapSizeTable(BinaryReader reader)
        {

            //EBLC's BitmapSize Table   (https://docs.microsoft.com/en-us/typography/opentype/spec/eblc)         
            //Type          Name                        Description
            //Offset32      indexSubTableArrayOffset    Offset to IndexSubtableArray, from beginning of EBLC.
            //uint32        indexTablesSize             Number of bytes in corresponding index subtables and array.
            //uint32        numberOfIndexSubTables      There is an IndexSubtable for each range or format change.
            //uint32        colorRef                    Not used; set to 0.
            //SbitLineMetrics    hori                   Line metrics for text rendered horizontally.
            //SbitLineMetrics    vert                   Line metrics for text rendered vertically.
            //uint16            startGlyphIndex         Lowest glyph index for this size.
            //uint16            endGlyphIndex           Highest glyph index for this size.
            //uint8             ppemX                   Horizontal pixels per em.
            //uint8             ppemY                   Vertical pixels per em.
            //uint8             bitDepth                The Microsoft rasterizer v.1.7 or greater supports the following bitDepth values, as described below: 1, 2, 4, and 8.
            //int8              flags                   Vertical or horizontal(see Bitmap Flags, below).


            //CBLC's BitmapSize Table  (https://docs.microsoft.com/en-us/typography/opentype/spec/cblc)          
            //Type                Name                      Description
            //Offset32            indexSubTableArrayOffset  Offset to index subtable from beginning of CBLC.
            //uint32              indexTablesSize           Number of bytes in corresponding index subtables and array.
            //uint32              numberofIndexSubTables    There is an index subtable for each range or format change.
            //uint32              colorRef                  Not used; set to 0.
            //SbitLineMetrics     hori                      Line metrics for text rendered horizontally.
            //SbitLineMetrics     vert                      Line metrics for text rendered vertically.
            //uint16              startGlyphIndex           Lowest glyph index for this size.
            //uint16              endGlyphIndex             Highest glyph index for this size.
            //uint8               ppemX                     Horizontal pixels per em.
            //uint8               ppemY                     Vertical pixels per em.
            //uint8               bitDepth                  In addtition to already defined bitDepth values 1, 2, 4, and 8
            //                                              supported by existing implementations, the value of 32 is used to 
            //                                              identify color bitmaps with 8 bit per pixel RGBA channels.
            //int8                flags                     Vertical or horizontal(see the Bitmap Flags section of the EBLC table chapter).


            //The indexSubTableArrayOffset is the offset from the beginning of 
            //the CBLC table to the indexSubTableArray.

            //Each strike has one of these arrays to support various formats and 
            //discontiguous ranges of bitmaps.The indexTablesSize is 
            //the total number of bytes in the indexSubTableArray and
            //the associated indexSubTables.
            //The numberOfIndexSubTables is a count of the indexSubTables for this strike.


            //The rest of the CBLC table structure is identical to one already defined for EBLC.


            BitmapSizeTable bmpSizeTable = new BitmapSizeTable();

            bmpSizeTable.indexSubTableArrayOffset = reader.ReadUInt32();
            bmpSizeTable.indexTablesSize = reader.ReadUInt32();
            bmpSizeTable.numberOfIndexSubTables = reader.ReadUInt32();
            bmpSizeTable.colorRef = reader.ReadUInt32();

            BitmapSizeTable.ReadSbitLineMetrics(reader, ref bmpSizeTable.hori);
            BitmapSizeTable.ReadSbitLineMetrics(reader, ref bmpSizeTable.vert);


            bmpSizeTable.startGlyphIndex = reader.ReadUInt16();
            bmpSizeTable.endGlyphIndex = reader.ReadUInt16();
            bmpSizeTable.ppemX = reader.ReadByte();
            bmpSizeTable.ppemY = reader.ReadByte();
            bmpSizeTable.bitDepth = reader.ReadByte();
            bmpSizeTable.flags = (sbyte)reader.ReadByte();

            return bmpSizeTable;
        }
    }



    readonly struct IndexSubTableArray
    {
        public readonly ushort firstGlyphIndex;
        public readonly ushort lastGlyphIndex;
        public readonly uint additionalOffsetToIndexSubtable;
        public IndexSubTableArray(ushort firstGlyphIndex, ushort lastGlyphIndex, uint additionalOffsetToIndexSubtable)
        {
            this.firstGlyphIndex = firstGlyphIndex;
            this.lastGlyphIndex = lastGlyphIndex;
            this.additionalOffsetToIndexSubtable = additionalOffsetToIndexSubtable;
        }
#if DEBUG
        public override string ToString()
        {
            return "[" + firstGlyphIndex + "-" + lastGlyphIndex + "]";
        }
#endif
    }

    readonly struct IndexSubHeader
    {
        public readonly ushort indexFormat;
        public readonly ushort imageFormat;
        public readonly uint imageDataOffset;

        public IndexSubHeader(ushort indexFormat,
            ushort imageFormat, uint imageDataOffset)
        {
            this.indexFormat = indexFormat;
            this.imageFormat = imageFormat;
            this.imageDataOffset = imageDataOffset;
        }

#if DEBUG
        public override string ToString()
        {
            return indexFormat + "," + imageFormat;
        }
#endif
    }
    abstract class IndexSubTableBase
    {
        public IndexSubHeader header;

        public abstract int SubTypeNo { get; }
        public ushort firstGlyphIndex;
        public ushort lastGlyphIndex;

        public static IndexSubTableBase CreateFrom(BitmapSizeTable bmpSizeTable, BinaryReader reader)
        {
            //read IndexSubHeader
            //IndexSubHeader
            //Type      Name            Description
            //uint16    indexFormat     Format of this IndexSubTable.
            //uint16    imageFormat     Format of EBDT image data.
            //Offset32  imageDataOffset Offset to image data in EBDT table.

            //There are currently five different formats used for the IndexSubTable, 
            //depending upon the size and type of bitmap data in the glyph ID range. 

            //Apple 'bloc' tables support only formats 1 through 3.

            //The choice of which IndexSubTable format to use is up to the font manufacturer, 
            //but should be made with the aim of minimizing the size of the font file.
            //Ranges of glyphs with variable metrics — that is,
            //where glyphs may differ from each other in bounding box height, width, side bearings or 
            //advance — must use format 1, 3 or 4.

            //Ranges of glyphs with constant metrics can save space by using format 2 or 5,
            //which keep a single copy of the metrics information in the IndexSubTable rather
            //than a copy per glyph in the EBDT table.

            //In some monospaced fonts it makes sense to store extra white space around 
            //some of the glyphs to keep all metrics identical, thus permitting the use of format 2 or 5.

            IndexSubHeader header = new IndexSubHeader(
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt32()
                );

            switch (header.indexFormat)
            {
                case 1:

                    //IndexSubTable1: variable - metrics glyphs with 4 - byte offsets
                    //Type                  Name            Description
                    //IndexSubHeader        header          Header info.
                    //Offset32              offsetArray[]   offsetArray[glyphIndex] + imageDataOffset = glyphData sizeOfArray = (lastGlyph - firstGlyph + 1) + 1 + 1 pad if needed
                    {
                        int nElem = (bmpSizeTable.endGlyphIndex - bmpSizeTable.startGlyphIndex + 1);
                        uint[] offsetArray = Utils.ReadUInt32Array(reader, nElem);
                        //check 16 bit align padd
                        IndexSubTable1 subTable = new IndexSubTable1();
                        subTable.header = header;
                        subTable.offsetArray = offsetArray;
                        return subTable;
                    }
                case 2:
                    //IndexSubTable2: all glyphs have identical metrics
                    //Type                 Name Description
                    //IndexSubHeader       header  Header info.
                    //uint32               imageSize   All the glyphs are of the same size.
                    //BigGlyphMetrics      bigMetrics  All glyphs have the same metrics; glyph data may be compressed, byte-aligned, or bit-aligned.
                    {
                        IndexSubTable2 subtable = new IndexSubTable2();
                        subtable.header = header;
                        subtable.imageSize = reader.ReadUInt32();
                        BigGlyphMetrics.ReadBigGlyphMetric(reader, ref subtable.BigGlyphMetrics);
                        return subtable;
                    }

                case 3:
                    //IndexSubTable3: variable - metrics glyphs with 2 - byte offsets
                    //Type                 Name         Description
                    //IndexSubHeader       header       Header info.
                    //Offset16             offsetArray[]   offsetArray[glyphIndex] + imageDataOffset = glyphData sizeOfArray = (lastGlyph - firstGlyph + 1) + 1 + 1 pad if needed
                    {
                        int nElem = (bmpSizeTable.endGlyphIndex - bmpSizeTable.startGlyphIndex + 1);
                        ushort[] offsetArray = Utils.ReadUInt16Array(reader, nElem);
                        //check 16 bit align padd
                        IndexSubTable3 subTable = new IndexSubTable3();
                        subTable.header = header;
                        subTable.offsetArray = offsetArray;
                        return subTable;
                    }
                case 4:
                    //IndexSubTable4: variable - metrics glyphs with sparse glyph codes
                    //Type                Name      Description
                    //IndexSubHeader      header    Header info.
                    //uint32              numGlyphs Array length.
                    //GlyphIdOffsetPair   glyphArray[numGlyphs + 1]   One per glyph.
                    {
                        IndexSubTable4 subTable = new IndexSubTable4();
                        subTable.header = header;

                        uint numGlyphs = reader.ReadUInt32();
                        GlyphIdOffsetPair[] glyphArray = subTable.glyphArray = new GlyphIdOffsetPair[numGlyphs + 1];
                        for (int i = 0; i <= numGlyphs; ++i) //***
                        {
                            glyphArray[i] = new GlyphIdOffsetPair(reader.ReadUInt16(), reader.ReadUInt16());
                        }
                        return subTable;
                    }
                case 5:
                    //IndexSubTable5: constant - metrics glyphs with sparse glyph codes
                    //Type                Name     Description
                    //IndexSubHeader      header  Header info.
                    //uint32              imageSize   All glyphs have the same data size.
                    //BigGlyphMetrics     bigMetrics  All glyphs have the same metrics.
                    //uint32              numGlyphs   Array length.
                    //uint16              glyphIdArray[numGlyphs]     One per glyph, sorted by glyph ID.
                    {
                        IndexSubTable5 subTable = new IndexSubTable5();
                        subTable.header = header;

                        subTable.imageSize = reader.ReadUInt32();
                        BigGlyphMetrics.ReadBigGlyphMetric(reader, ref subTable.BigGlyphMetrics);
                        subTable.glyphIdArray = Utils.ReadUInt16Array(reader, (int)reader.ReadUInt32());
                        return subTable;
                    }

            }

            //The size of the EBDT image data can be calculated from the IndexSubTable information.
            //For the constant-metrics formats(2 and 5) the image data size is constant,
            //and is given in the imageSize field.For the variable metrics formats(1, 3, and 4) 
            //image data must be stored contiguously and in glyph ID order,
            //so the image data size may be calculated by subtracting the offset for
            //the current glyph from the offset of the next glyph.

            //Because of this, it is necessary to store one extra element in the offsetArray pointing
            //just past the end of the range’s image data.
            //This will allow the correct calculation of the image data size for the last glyph in the range.

            //Contiguous, or nearly contiguous,
            //ranges of glyph IDs are handled best by formats 1, 2, and 3 which
            //store an offset for every glyph ID in the range.
            //Very sparse ranges of glyph IDs should use format 4 or 5 which explicitly 
            //call out the glyph IDs represented in the range.
            //A small number of missing glyphs can be efficiently represented in formats 1 or 3 by having 
            //the offset for the missing glyph be followed by the same offset for 
            //the next glyph, thus indicating a data size of zero.

            //The only difference between formats 1 and 3 is 
            //the size of the offsetArray elements: format 1 uses uint32s while format 3 uses uint16s. 
            //Therefore format 1 can cover a greater range(> 64k bytes) 
            //while format 3 saves more space in the EBLC table.
            //Since the offsetArray elements are added to the imageDataOffset base address in the IndexSubHeader, 
            //a very large set of glyph bitmap data could be addressed by splitting it into multiple ranges,
            //each less than 64k bytes in size, 
            //allowing the use of the more efficient format 3.

            //The EBLC table specification requires 16 - bit alignment for all subtables. 
            //This occurs naturally for IndexSubTable formats 1, 2, and 4, 
            //but may not for formats 3 and 5, 
            //since they include arrays of type uint16.
            //When there is an odd number of elements in these arrays 
            //**it is necessary to add an extra padding element to maintain proper alignment.


            return null;
        }

        public abstract void BuildGlyphList(List<Glyph> glyphList);
    }
    /// <summary>
    /// IndexSubTable1: variable - metrics glyphs with 4 - byte offsets
    /// </summary>
    class IndexSubTable1 : IndexSubTableBase
    {
        public override int SubTypeNo => 1;
        public uint[] offsetArray;

        public override void BuildGlyphList(List<Glyph> glyphList)
        {
            int n = 0;
            for (ushort i = firstGlyphIndex; i <= lastGlyphIndex; ++i)
            {
                glyphList.Add(new Glyph(i, header.imageDataOffset + offsetArray[n], 0, header.imageFormat));
                n++;
            }
        }
    }
    /// <summary>
    ///  IndexSubTable2: all glyphs have identical metrics
    /// </summary>
    class IndexSubTable2 : IndexSubTableBase
    {
        public override int SubTypeNo => 2;
        public uint imageSize;
        public BigGlyphMetrics BigGlyphMetrics = new BigGlyphMetrics();
        public override void BuildGlyphList(List<Glyph> glyphList)
        {
            uint incrementalOffset = 0;//TODO: review this
            for (ushort n = firstGlyphIndex; n <= lastGlyphIndex; ++n)
            {
                glyphList.Add(new Glyph(n, header.imageDataOffset + incrementalOffset, imageSize, header.imageFormat));
                incrementalOffset += imageSize;
            }
        }
    }
    /// <summary>
    /// IndexSubTable3: variable - metrics glyphs with 2 - byte offsets
    /// </summary>
    class IndexSubTable3 : IndexSubTableBase
    {
        public override int SubTypeNo => 3;
        public ushort[] offsetArray;
        public override void BuildGlyphList(List<Glyph> glyphList)
        {
            int n = 0;
            for (ushort i = firstGlyphIndex; i <= lastGlyphIndex; ++i)
            {
                glyphList.Add(new Glyph(i, header.imageDataOffset + offsetArray[n++], 0, header.imageFormat));
            }
        }
    }
    /// <summary>
    /// IndexSubTable4: variable - metrics glyphs with sparse glyph codes
    /// </summary>
    class IndexSubTable4 : IndexSubTableBase
    {
        public override int SubTypeNo => 4;
        public GlyphIdOffsetPair[] glyphArray;
        public override void BuildGlyphList(List<Glyph> glyphList)
        {
            for (int i = 0; i < glyphArray.Length; ++i)
            {
                GlyphIdOffsetPair pair = glyphArray[i];
                glyphList.Add(new Glyph(pair.glyphId, header.imageDataOffset + pair.offset, 0, header.imageFormat));
            }
        }
    }
    /// <summary>
    /// IndexSubTable5: constant - metrics glyphs with sparse glyph codes
    /// </summary>
    class IndexSubTable5 : IndexSubTableBase
    {
        public override int SubTypeNo => 5;
        public uint imageSize;
        public BigGlyphMetrics BigGlyphMetrics = new BigGlyphMetrics();

        public ushort[] glyphIdArray;
        public override void BuildGlyphList(List<Glyph> glyphList)
        {
            uint incrementalOffset = 0;//TODO: review this
            for (int i = 0; i < glyphIdArray.Length; ++i)
            {
                glyphList.Add(new Glyph(glyphIdArray[i], header.imageDataOffset + incrementalOffset, imageSize, header.imageFormat));
                incrementalOffset += imageSize;
            }
        }

    }
    //GlyphIdOffsetPair record:
    //Type      Name        Description
    //uint16    glyphID     Glyph ID of glyph present.
    //Offset16  offset      Location in EBDT.

    readonly struct GlyphIdOffsetPair
    {
        public readonly ushort glyphId;
        public readonly ushort offset;
        public GlyphIdOffsetPair(ushort glyphId, ushort offset)
        {
            this.glyphId = glyphId;
            this.offset = offset;
        }
    }


    //BigGlyphMetrics
    //Type    Name
    //uint8   height
    //uint8   width
    //int8    horiBearingX
    //int8    horiBearingY
    //uint8   horiAdvance
    //int8    vertBearingX
    //int8    vertBearingY
    //uint8   vertAdvance

    struct BigGlyphMetrics
    {
        public byte height;
        public byte width;

        public sbyte horiBearingX;
        public sbyte horiBearingY;
        public byte horiAdvance;

        public sbyte vertBearingX;
        public sbyte vertBearingY;
        public byte vertAdvance;

        public const int SIZE = 8; //size of BigGlyphMetrics

        public static void ReadBigGlyphMetric(BinaryReader reader, ref BigGlyphMetrics output)
        {

            output.height = reader.ReadByte();
            output.width = reader.ReadByte();

            output.horiBearingX = (sbyte)reader.ReadByte();
            output.horiBearingY = (sbyte)reader.ReadByte();
            output.horiAdvance = reader.ReadByte();

            output.vertBearingX = (sbyte)reader.ReadByte();
            output.vertBearingY = (sbyte)reader.ReadByte();
            output.vertAdvance = reader.ReadByte();
        }
    }

    //SmallGlyphMetrics
    //Type    Name
    //uint8   height
    //uint8   width
    //int8    bearingX
    //int8    bearingY
    //uint8   advance
    struct SmallGlyphMetrics
    {
        public byte height;
        public byte width;
        public sbyte bearingX;
        public sbyte bearingY;
        public byte advance;

        public const int SIZE = 5; //size of SmallGlyphMetrics
        public static void ReadSmallGlyphMetric(BinaryReader reader, out SmallGlyphMetrics output)
        {
            output = new SmallGlyphMetrics();
            output.height = reader.ReadByte();
            output.width = reader.ReadByte();

            output.bearingX = (sbyte)reader.ReadByte();
            output.bearingY = (sbyte)reader.ReadByte();
            output.advance = reader.ReadByte();
        }
    }


    //------------

    abstract class GlyphBitmapDataFormatBase
    {
        public abstract int FormatNumber { get; }
        public abstract void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph);
        public abstract void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, System.IO.Stream outputStream);
    }


    /// <summary>
    /// Format 1: small metrics, byte-aligned data
    /// </summary>
    class GlyphBitmapDataFmt1 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 1;
        public SmallGlyphMetrics smallGlyphMetrics;
        //Glyph bitmap format 1 consists of small metrics records(either horizontal or vertical
        //depending on the flags field of the BitmapSize table within the EBLC table) 
        //followed by byte aligned bitmap data.

        //The bitmap data begins with the most significant bit of the
        //first byte corresponding to the top-left pixel of the bounding box,
        //proceeding through succeeding bits moving left to right. 
        //The data for each row is padded to a byte boundary, 
        //so the next row begins with the most significant bit of a new byte.

        //1 bits correspond to black, and 0 bits to white.

        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Format 2: small metrics, bit-aligned data
    /// </summary>
    class GlyphBitmapDataFmt2 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 2;


        //Glyph bitmap format 2 is the same as format 1 except
        //that the bitmap data is bit aligned.

        //This means that the data for a new row will begin with the bit immediately
        //following the last bit of the previous row.
        //The start of each glyph must be byte aligned, 
        //so the last row of a glyph may require padding.

        //This format takes a little more time to parse, but saves file space compared to format 1.
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }

    //format 3 Obsolete
    //format 4: not support in OpenFont

    //Format 5: metrics in EBLC, bit-aligned image data only
    class GlyphBitmapDataFmt5 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 5;

        //Glyph bitmap format 5 is similar to format 2 except
        //that no metrics information is included, just the bit aligned data. 
        //This format is for use with EBLC indexSubTable format 2 or format 5, 
        //which will contain the metrics information for all glyphs. It works well for Kanji fonts.

        //The rasterizer recalculates 
        //sbit metrics for Format 5 bitmap data, 
        //allowing Windows to report correct ABC widths,
        //even if the bitmaps have white space on either side of the bitmap image.
        //This allows fonts to store monospaced bitmap glyphs in the efficient Format 5
        //without breaking Windows GetABCWidths call.
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Format 6: big metrics, byte-aligned data
    /// </summary>
    class GlyphBitmapDataFmt6 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 6;
        public BigGlyphMetrics bigMetrics;

        //Format 6: big metrics, byte-aligned data
        //Type            Name                  Description
        //BigGlyphMetrics bigMetrics            Metrics information for the glyph
        //uint8           imageData[variable]   Byte-aligned bitmap data

        //Glyph bitmap format 6 is the same as format 1 except that is uses big glyph metrics instead of small.
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Format7: big metrics, bit-aligned data
    /// </summary>
    class GlyphBitmapDataFmt7 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 7;

        public BigGlyphMetrics bigMetrics;

        //    
        //Type                Name                  Description
        //BigGlyphMetrics     bigMetrics            Metrics information for the glyph
        //uint8               imageData[variable]   Bit-aligned bitmap data 
        //Glyph bitmap format 7 is the same as format 2 except that is uses big glyph metrics instead of small.
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }


    //EbdtComponent Record

    //The EbdtComponent record is used in glyph bitmap data formats 8 and 9.
    //Type Name    Description
    //uint16  glyphID Component glyph ID
    //int8 xOffset     Position of component left
    //int8 yOffset     Position of component top

    //The EbdtComponent record contains the glyph ID of the component, which can be used to look up the location of component glyph data in the EBLC table, as well as xOffset and yOffset values, which specify where to position the top-left corner of the component in the composite.Nested composites (a composite of composites) are allowed, and the number of nesting levels is determined by implementation stack space.

    struct EbdtComponent
    {
        public ushort glyphID;
        public sbyte xOffset;
        public sbyte yOffset;
    }


    /// <summary>
    /// Format 8: small metrics, component data
    /// </summary>
    class GlyphBitmapDataFmt8 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 8;

        public SmallGlyphMetrics smallMetrics;
        public byte pad;
        public EbdtComponent[] components;
        //Format 8: small metrics, component data
        //Type              Name            Description
        //SmallGlyphMetrics smallMetrics    Metrics information for the glyph
        //uint8             pad             Pad to 16-bit boundary
        //uint16            numComponents   Number of components
        //EbdtComponent     components[numComponents] Array of EbdtComponent records
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Format 9: 
    /// </summary>
    class GlyphBitmapDataFmt9 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 9;
        public BigGlyphMetrics bigMetrics;
        public EbdtComponent[] components;

        //Format 9: big metrics, component data
        //Type              Name            Description
        //BigGlyphMetrics   bigMetrics      Metrics information for the glyph
        //uint16            numComponents   Number of components
        //EbdtComponent     components[numComponents] Array of EbdtComponent records
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            throw new NotImplementedException();
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }

    //Glyph bitmap formats 8 and 9 are used for composite bitmaps.
    //For accented characters and other composite glyphs
    //it may be more efficient to store
    //a copy of each component separately, 
    //and then use a composite description to construct the finished glyph.

    //The composite formats allow for any number of components, 
    //and allow the components to be positioned anywhere in the finished glyph.
    //Format 8 uses small metrics, and format 9 uses big metrics.


    //------------
    //for CBDT...

    /// <summary>
    ///  Format 17: small metrics, PNG image data
    /// </summary>
    class GlyphBitmapDataFmt17 : GlyphBitmapDataFormatBase
    {
        public override int FormatNumber => 17;

        //Format 17: small metrics, PNG image data
        //Type                Name          Description
        //smallGlyphMetrics   glyphMetrics  Metrics information for the glyph
        //uint32              dataLen       Length of data in bytes
        //uint8               data[dataLen] Raw PNG data 

        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            SmallGlyphMetrics.ReadSmallGlyphMetric(reader, out SmallGlyphMetrics smallGlyphMetric);

            bitmapGlyph.BitmapGlyphAdvanceWidth = smallGlyphMetric.advance;
            bitmapGlyph.Bounds = new Bounds(0, 0, smallGlyphMetric.width, smallGlyphMetric.height);

            //then 
            //byte[] buff = reader.ReadBytes((int)dataLen);
            //System.IO.File.WriteAllBytes("testBitmapGlyph_" + glyph.GlyphIndex + ".png", buff);
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            //only read raw png data
            reader.BaseStream.Position += SmallGlyphMetrics.SIZE;
            uint dataLen = reader.ReadUInt32();
            byte[] rawPngData = reader.ReadBytes((int)dataLen);
            outputStream.Write(rawPngData, 0, rawPngData.Length);
        }
    }

    /// <summary>
    /// Format 18: big metrics, PNG image data
    /// </summary>
    class GlyphBitmapDataFmt18 : GlyphBitmapDataFormatBase
    {
        //Format 18: big metrics, PNG image data
        //Type              Name            Description
        //bigGlyphMetrics   glyphMetrics    Metrics information for the glyph
        //uint32            dataLen         Length of data in bytes
        //uint8             data[dataLen]   Raw PNG data
        public override int FormatNumber => 18;

        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            BigGlyphMetrics bigGlyphMetric = new BigGlyphMetrics();
            BigGlyphMetrics.ReadBigGlyphMetric(reader, ref bigGlyphMetric);
            uint dataLen = reader.ReadUInt32();

            bitmapGlyph.BitmapGlyphAdvanceWidth = bigGlyphMetric.horiAdvance;
            bitmapGlyph.Bounds = new Bounds(0, 0, bigGlyphMetric.width, bigGlyphMetric.height);
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            reader.BaseStream.Position += BigGlyphMetrics.SIZE;
            uint dataLen = reader.ReadUInt32();
            byte[] rawPngData = reader.ReadBytes((int)dataLen);
            outputStream.Write(rawPngData, 0, rawPngData.Length);
        }
    }

    class GlyphBitmapDataFmt19 : GlyphBitmapDataFormatBase
    {
        //Format 19: metrics in CBLC table, PNG image data
        //Type    Name          Description
        //uint32  dataLen       Length of data in bytes
        //uint8   data[dataLen] Raw PNG data
        public override int FormatNumber => 19;
        public override void FillGlyphInfo(BinaryReader reader, Glyph bitmapGlyph)
        {
            //no glyph info to fill
            //TODO::....
        }
        public override void ReadRawBitmap(BinaryReader reader, Glyph bitmapGlyph, Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }
}
