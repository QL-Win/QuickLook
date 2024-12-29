//MIT, 2019-present, WinterDev
using System.IO;
using System.Collections.Generic;

using Typography.OpenFont.Tables;
using Typography.OpenFont.Trimmable;

//see https://www.w3.org/TR/WOFF2/

namespace Typography.OpenFont.WebFont
{
    //NOTE: Web Font file structure is not part of 'Open Font Format'.

    class Woff2Header
    {
        //WOFF2 Header
        //UInt32 signature              0x774F4632 'wOF2'
        //UInt32 flavor                 The "sfnt version" of the input font.
        //UInt32 length                 Total size of the WOFF file.
        //UInt16 numTables              Number of entries in directory of font tables.
        //UInt16 reserved               Reserved; set to 0.
        //UInt32 totalSfntSize          Total size needed for the uncompressed font data, including the sfnt header,
        //                              directory, and font tables(including padding).
        //UInt32  totalCompressedSize   Total length of the compressed data block.
        //UInt16  majorVersion          Major version of the WOFF file.
        //UInt16  minorVersion          Minor version of the WOFF file.
        //UInt32  metaOffset            Offset to metadata block, from beginning of WOFF file.
        //UInt32  metaLength            Length of compressed metadata block.
        //UInt32  metaOrigLength        Uncompressed size of metadata block.
        //UInt32  privOffset            Offset to private data block, from beginning of WOFF file.
        //UInt32  privLength            Length of private data block.

        public uint flavor;
        public uint length;
        public uint numTables;

        //public ushort reserved;
        public uint totalSfntSize;

        public uint totalCompressedSize; //***
        public ushort majorVersion;
        public ushort minorVersion;
        public uint metaOffset;
        public uint metaLength;
        public uint metaOriginalLength;
        public uint privOffset;
        public uint privLength;
    }

    class Woff2TableDirectory
    {
        //TableDirectoryEntry
        //UInt8         flags           table type and flags
        //UInt32        tag	            4-byte tag(optional)
        //UIntBase128   origLength      length of original table
        //UIntBase128   transformLength transformed length(if applicable)

        public uint origLength;
        public uint transformLength;

        //translated values
        public string Name { get; set; } //translate from tag

        public byte PreprocessingTransformation { get; set; }
        public long ExpectedStartAt { get; set; }
#if DEBUG

        public override string ToString()
        {
            return Name + " " + PreprocessingTransformation;
        }

#endif
    }

    public delegate bool BrotliDecompressStreamFunc(byte[] compressedInput, Stream decompressStream);

    public static class Woff2DefaultBrotliDecompressFunc
    {
        public static BrotliDecompressStreamFunc DecompressHandler;
    }

    class TransformedGlyf : UnreadTableEntry
    {
        private static TripleEncodingTable s_encTable = TripleEncodingTable.GetEncTable();

        public TransformedGlyf(TableHeader header, Woff2TableDirectory tableDir) : base(header)
        {
            HasCustomContentReader = true;
            TableDir = tableDir;
        }

        public Woff2TableDirectory TableDir { get; }

        public override T CreateTableEntry<T>(BinaryReader reader, T expectedResult)
        {
            if (!(expectedResult is Glyf glyfTable)) throw new System.NotSupportedException();

            ReconstructGlyfTable(reader, TableDir, glyfTable);

            return expectedResult;
        }

        struct TempGlyph
        {
            public readonly ushort glyphIndex;
            public readonly short numContour;

            public ushort instructionLen;
            public bool compositeHasInstructions;

            public TempGlyph(ushort glyphIndex, short contourCount)
            {
                this.glyphIndex = glyphIndex;
                this.numContour = contourCount;

                instructionLen = 0;
                compositeHasInstructions = false;
            }

#if DEBUG

            public override string ToString()
            {
                return glyphIndex + " " + numContour;
            }

#endif
        }

        static void ReconstructGlyfTable(BinaryReader reader, Woff2TableDirectory woff2TableDir, Glyf glyfTable)
        {
            //fill the information to glyfTable
            //reader.BaseStream.Position += woff2TableDir.transformLength;
            //For greater compression effectiveness,
            //the glyf table is split into several substreams, to group like data together.

            //The transformed table consists of a number of fields specifying the size of each of the substreams,
            //followed by the substreams in sequence.

            //During the decoding process the reverse transformation takes place,
            //where data from various separate substreams are recombined to create a complete glyph record
            //for each entry of the original glyf table.

            //Transformed glyf Table
            //Data-Type Semantic                Description and value type(if applicable)
            //Fixed     version                 = 0x00000000
            //UInt16    numGlyphs               Number of glyphs
            //UInt16    indexFormatOffset      format for loca table,
            //                                 should be consistent with indexToLocFormat of
            //                                 the original head table(see[OFF] specification)

            //UInt32    nContourStreamSize      Size of nContour stream in bytes
            //UInt32    nPointsStreamSize       Size of nPoints stream in bytes
            //UInt32    flagStreamSize          Size of flag stream in bytes
            //UInt32    glyphStreamSize         Size of glyph stream in bytes(a stream of variable-length encoded values, see description below)
            //UInt32    compositeStreamSize     Size of composite stream in bytes(a stream of variable-length encoded values, see description below)
            //UInt32    bboxStreamSize          Size of bbox data in bytes representing combined length of bboxBitmap(a packed bit array) and bboxStream(a stream of Int16 values)
            //UInt32    instructionStreamSize   Size of instruction stream(a stream of UInt8 values)

            //Int16     nContourStream[]        Stream of Int16 values representing number of contours for each glyph record
            //255UInt16 nPointsStream[]         Stream of values representing number of outline points for each contour in glyph records
            //UInt8     flagStream[]            Stream of UInt8 values representing flag values for each outline point.
            //Vary      glyphStream[]           Stream of bytes representing point coordinate values using variable length encoding format(defined in subclause 5.2)
            //Vary      compositeStream[]       Stream of bytes representing component flag values and associated composite glyph data
            //UInt8     bboxBitmap[]            Bitmap(a numGlyphs-long bit array) indicating explicit bounding boxes
            //Int16     bboxStream[]            Stream of Int16 values representing glyph bounding box data
            //UInt8     instructionStream[]	    Stream of UInt8 values representing a set of instructions for each corresponding glyph

            reader.BaseStream.Position = woff2TableDir.ExpectedStartAt;

            long start = reader.BaseStream.Position;

            uint version = reader.ReadUInt32();
            ushort numGlyphs = reader.ReadUInt16();
            ushort indexFormatOffset = reader.ReadUInt16();

            uint nContourStreamSize = reader.ReadUInt32(); //in bytes
            uint nPointsStreamSize = reader.ReadUInt32(); //in bytes
            uint flagStreamSize = reader.ReadUInt32(); //in bytes
            uint glyphStreamSize = reader.ReadUInt32(); //in bytes
            uint compositeStreamSize = reader.ReadUInt32(); //in bytes
            uint bboxStreamSize = reader.ReadUInt32(); //in bytes
            uint instructionStreamSize = reader.ReadUInt32(); //in bytes

            long expected_nCountStartAt = reader.BaseStream.Position;
            long expected_nPointStartAt = expected_nCountStartAt + nContourStreamSize;
            long expected_FlagStreamStartAt = expected_nPointStartAt + nPointsStreamSize;
            long expected_GlyphStreamStartAt = expected_FlagStreamStartAt + flagStreamSize;
            long expected_CompositeStreamStartAt = expected_GlyphStreamStartAt + glyphStreamSize;

            long expected_BboxStreamStartAt = expected_CompositeStreamStartAt + compositeStreamSize;
            long expected_InstructionStreamStartAt = expected_BboxStreamStartAt + bboxStreamSize;
            long expected_EndAt = expected_InstructionStreamStartAt + instructionStreamSize;

            //---------------------------------------------
            Glyph[] glyphs = new Glyph[numGlyphs];
            TempGlyph[] allGlyphs = new TempGlyph[numGlyphs];
            List<ushort> compositeGlyphs = new List<ushort>();
            int contourCount = 0;
            for (ushort i = 0; i < numGlyphs; ++i)
            {
                short numContour = reader.ReadInt16();
                allGlyphs[i] = new TempGlyph(i, numContour);
                if (numContour > 0)
                {
                    contourCount += numContour;
                    //>0 => simple glyph
                    //-1 = compound
                    //0 = empty glyph
                }
                else if (numContour < 0)
                {
                    //composite glyph, resolve later
                    compositeGlyphs.Add(i);
                }
                else
                {
                }
            }

            //--------------------------------------------------------------------------------------------
            //glyphStream
            //5.2.Decoding of variable-length X and Y coordinates

            //Simple glyph data structure defines all contours that comprise a glyph outline,
            //which are presented by a sequence of on- and off-curve coordinate points.

            //These point coordinates are encoded as delta values representing the incremental values
            //between the previous and current corresponding X and Y coordinates of a point,
            //the first point of each outline is relative to (0, 0) point.

            //To minimize the size of the dataset of point coordinate values,
            //each point is presented as a (flag, xCoordinate, yCoordinate) triplet.

            //The flag value is stored in a separate data stream
            //and the coordinate values are stored as part of the glyph data stream using a variable-length encoding format
            //consuming a total of 2 - 5 bytes per point.

            //Decoding of Simple Glyphs:

            //For a simple glyph(when nContour > 0), the process continues as follows:
            //    1) Read numberOfContours 255UInt16 values from the nPoints stream.
            //    Each of these is the number of points of that contour.
            //    Convert this into the endPtsOfContours[] array by computing the cumulative sum, then subtracting one.
            //    For example, if the values in the stream are[2, 4], then the endPtsOfContours array is [1, 5].Also,
            //      the sum of all the values in the array is the total number of points in the glyph, nPoints.
            //      In the example given, the value of nPoints is 6.

            //    2) Read nPoints UInt8 values from the flags stream.Each corresponds to one point in the reconstructed glyph outline.
            //       The interpretation of the flag byte is described in details in subclause 5.2.

            //    3) For each point(i.e.nPoints times), read a number of point coordinate bytes from the glyph stream.
            //       The number of point coordinate bytes is a function of the flag byte read in the previous step:
            //       for (flag < 0x7f) in the range 0 to 83 inclusive, it is one byte.
            //       In the range 84 to 119 inclusive, it is two bytes.
            //       In the range 120 to 123 inclusive, it is three bytes,
            //       and in the range 124 to 127 inclusive, it is four bytes.
            //       Decode these bytes according to the procedure specified in the subclause 5.2 to reconstruct delta-x and delta-y values of the glyph point coordinates.
            //       Store these delta-x and delta-y values in the reconstructed glyph using the standard TrueType glyph encoding[OFF] subclause 5.3.3.

            //    4) Read one 255UInt16 value from the glyph stream, which is instructionLength, the number of instruction bytes.
            //    5) Read instructionLength bytes from instructionStream, and store these in the reconstituted glyph as instructions.
            //--------
#if DEBUG
            if (reader.BaseStream.Position != expected_nPointStartAt)
            {
                System.Diagnostics.Debug.WriteLine("ERR!!");
            }
#endif
            //
            //1) nPoints stream,  npoint for each contour

            ushort[] pntPerContours = new ushort[contourCount];
            for (int i = 0; i < contourCount; ++i)
            {
                // Each of these is the number of points of that contour.
                pntPerContours[i] = Woff2Utils.Read255UInt16(reader);
            }
#if DEBUG
            if (reader.BaseStream.Position != expected_FlagStreamStartAt)
            {
                System.Diagnostics.Debug.WriteLine("ERR!!");
            }
#endif
            //2) flagStream, flags value for each point
            //each byte in flags stream represents one point
            byte[] flagStream = reader.ReadBytes((int)flagStreamSize);

#if DEBUG
            if (reader.BaseStream.Position != expected_GlyphStreamStartAt)
            {
                System.Diagnostics.Debug.WriteLine("ERR!!");
            }
#endif

            //***
            //some composite glyphs have instructions=> so we must check all composite glyphs
            //before read the glyph stream
            //**
            using (MemoryStream compositeMS = new MemoryStream())
            {
                reader.BaseStream.Position = expected_CompositeStreamStartAt;
                compositeMS.Write(reader.ReadBytes((int)compositeStreamSize), 0, (int)compositeStreamSize);
                compositeMS.Position = 0;

                int j = compositeGlyphs.Count;
                ByteOrderSwappingBinaryReader compositeReader = new ByteOrderSwappingBinaryReader(compositeMS);
                for (ushort i = 0; i < j; ++i)
                {
                    ushort compositeGlyphIndex = compositeGlyphs[i];
                    allGlyphs[compositeGlyphIndex].compositeHasInstructions = CompositeHasInstructions(compositeReader, compositeGlyphIndex);
                }
                reader.BaseStream.Position = expected_GlyphStreamStartAt;
            }
            //--------
            int curFlagsIndex = 0;
            int pntContourIndex = 0;
            for (int i = 0; i < allGlyphs.Length; ++i)
            {
                glyphs[i] = BuildSimpleGlyphStructure(reader,
                    ref allGlyphs[i],
                    glyfTable._emptyGlyph,
                    pntPerContours, ref pntContourIndex,
                    flagStream, ref curFlagsIndex);
            }

#if DEBUG
            if (pntContourIndex != pntPerContours.Length)
            {
            }
            if (curFlagsIndex != flagStream.Length)
            {
            }
#endif
            //--------------------------------------------------------------------------------------------
            //compositeStream
            //--------------------------------------------------------------------------------------------
#if DEBUG
            if (expected_CompositeStreamStartAt != reader.BaseStream.Position)
            {
                //***

                reader.BaseStream.Position = expected_CompositeStreamStartAt;
            }
#endif
            {
                //now we read the composite stream again
                //and create composite glyphs
                int j = compositeGlyphs.Count;
                for (ushort i = 0; i < j; ++i)
                {
                    int compositeGlyphIndex = compositeGlyphs[i];
                    glyphs[compositeGlyphIndex] = ReadCompositeGlyph(glyphs, reader, i, glyfTable._emptyGlyph);
                }
            }

            //--------------------------------------------------------------------------------------------
            //bbox stream
            //--------------------------------------------------------------------------------------------

            //Finally, for both simple and composite glyphs,
            //if the corresponding bit in the bounding box bit vector is set,
            //then additionally read 4 Int16 values from the bbox stream,
            //representing xMin, yMin, xMax, and yMax, respectively,
            //and record these into the corresponding fields of the reconstructed glyph.
            //For simple glyphs, if the corresponding bit in the bounding box bit vector is not set,
            //then derive the bounding box by computing the minimum and maximum x and y coordinates in the outline, and storing that.

            //A composite glyph MUST have an explicitly supplied bounding box.
            //The motivation is that computing bounding boxes is more complicated,
            //and would require resolving references to component glyphs taking into account composite glyph instructions and
            //the specified scales of individual components, which would conflict with a purely streaming implementation of font decoding.

            //A decoder MUST check for presence of the bounding box info as part of the composite glyph record
            //and MUST NOT load a font file with the composite bounding box data missing.
#if DEBUG
            if (expected_BboxStreamStartAt != reader.BaseStream.Position)
            {
            }
#endif
            int bitmapCount = (numGlyphs + 7) / 8;
            byte[] bboxBitmap = ExpandBitmap(reader.ReadBytes(bitmapCount));
            for (ushort i = 0; i < numGlyphs; ++i)
            {
                TempGlyph tempGlyph = allGlyphs[i];
                Glyph glyph = glyphs[i];

                byte hasBbox = bboxBitmap[i];
                if (hasBbox == 1)
                {
                    //read bbox from the bboxstream
                    glyph.Bounds = new Bounds(
                        reader.ReadInt16(),
                        reader.ReadInt16(),
                        reader.ReadInt16(),
                        reader.ReadInt16());
                }
                else
                {
                    //no bbox
                    //
                    if (tempGlyph.numContour < 0)
                    {
                        //composite must have bbox
                        //if not=> err
                        throw new System.NotSupportedException();
                    }
                    else if (tempGlyph.numContour > 0)
                    {
                        //simple glyph
                        //use simple calculation
                        //...For simple glyphs, if the corresponding bit in the bounding box bit vector is not set,
                        //then derive the bounding box by computing the minimum and maximum x and y coordinates in the outline, and storing that.
                        glyph.Bounds = FindSimpleGlyphBounds(glyph);
                    }
                }
            }
            //--------------------------------------------------------------------------------------------
            //instruction stream
#if DEBUG
            if (reader.BaseStream.Position < expected_InstructionStreamStartAt)
            {
            }
            else if (expected_InstructionStreamStartAt == reader.BaseStream.Position)
            {
            }
            else
            {
            }
#endif

            reader.BaseStream.Position = expected_InstructionStreamStartAt;
            //--------------------------------------------------------------------------------------------

            for (ushort i = 0; i < numGlyphs; ++i)
            {
                TempGlyph tempGlyph = allGlyphs[i];
                if (tempGlyph.instructionLen > 0)
                {
                    glyphs[i].GlyphInstructions = reader.ReadBytes(tempGlyph.instructionLen);
                }
            }

#if DEBUG
            if (reader.BaseStream.Position != expected_EndAt)
            {
            }
#endif

            glyfTable.Glyphs = glyphs;
        }

        static Bounds FindSimpleGlyphBounds(Glyph glyph)
        {
            GlyphPointF[] glyphPoints = glyph.GlyphPoints;

            int j = glyphPoints.Length;
            float xmin = float.MaxValue;
            float ymin = float.MaxValue;
            float xmax = float.MinValue;
            float ymax = float.MinValue;

            for (int i = 0; i < j; ++i)
            {
                GlyphPointF p = glyphPoints[i];
                if (p.X < xmin) xmin = p.X;
                if (p.X > xmax) xmax = p.X;
                if (p.Y < ymin) ymin = p.Y;
                if (p.Y > ymax) ymax = p.Y;
            }

            return new Bounds(
               (short)System.Math.Round(xmin),
               (short)System.Math.Round(ymin),
               (short)System.Math.Round(xmax),
               (short)System.Math.Round(ymax));
        }

        static byte[] ExpandBitmap(byte[] orgBBoxBitmap)
        {
            byte[] expandArr = new byte[orgBBoxBitmap.Length * 8];

            int index = 0;
            for (int i = 0; i < orgBBoxBitmap.Length; ++i)
            {
                byte b = orgBBoxBitmap[i];
                expandArr[index++] = (byte)((b >> 7) & 0x1);
                expandArr[index++] = (byte)((b >> 6) & 0x1);
                expandArr[index++] = (byte)((b >> 5) & 0x1);
                expandArr[index++] = (byte)((b >> 4) & 0x1);
                expandArr[index++] = (byte)((b >> 3) & 0x1);
                expandArr[index++] = (byte)((b >> 2) & 0x1);
                expandArr[index++] = (byte)((b >> 1) & 0x1);
                expandArr[index++] = (byte)((b >> 0) & 0x1);
            }
            return expandArr;
        }

        static Glyph BuildSimpleGlyphStructure(BinaryReader glyphStreamReader,
            ref TempGlyph tmpGlyph,
            Glyph emptyGlyph,
            ushort[] pntPerContours, ref int pntContourIndex,
            byte[] flagStream, ref int flagStreamIndex)
        {
            //reading from glyphstream***
            //Building a SimpleGlyph
            //    1) Read numberOfContours 255UInt16 values from the nPoints stream.
            //    Each of these is the number of points of that contour.
            //    Convert this into the endPtsOfContours[] array by computing the cumulative sum, then subtracting one.
            //    For example, if the values in the stream are[2, 4], then the endPtsOfContours array is [1, 5].Also,
            //      the sum of all the values in the array is the total number of points in the glyph, nPoints.
            //      In the example given, the value of nPoints is 6.

            //    2) Read nPoints UInt8 values from the flags stream.Each corresponds to one point in the reconstructed glyph outline.
            //       The interpretation of the flag byte is described in details in subclause 5.2.

            //    3) For each point(i.e.nPoints times), read a number of point coordinate bytes from the glyph stream.
            //       The number of point coordinate bytes is a function of the flag byte read in the previous step:
            //       for (flag < 0x7f)
            //       in the range 0 to 83 inclusive, it is one byte.
            //       In the range 84 to 119 inclusive, it is two bytes.
            //       In the range 120 to 123 inclusive, it is three bytes,
            //       and in the range 124 to 127 inclusive, it is four bytes.
            //       Decode these bytes according to the procedure specified in the subclause 5.2 to reconstruct delta-x and delta-y values of the glyph point coordinates.
            //       Store these delta-x and delta-y values in the reconstructed glyph using the standard TrueType glyph encoding[OFF] subclause 5.3.3.

            //    4) Read one 255UInt16 value from the glyph stream, which is instructionLength, the number of instruction bytes.
            //    5) Read instructionLength bytes from instructionStream, and store these in the reconstituted glyph as instructions.

            if (tmpGlyph.numContour == 0) return emptyGlyph;
            if (tmpGlyph.numContour < 0)
            {
                //composite glyph,
                //check if this has instruction or not
                if (tmpGlyph.compositeHasInstructions)
                {
                    tmpGlyph.instructionLen = Woff2Utils.Read255UInt16(glyphStreamReader);
                }
                return null;//skip composite glyph (resolve later)
            }

            //-----
            int curX = 0;
            int curY = 0;

            int numContour = tmpGlyph.numContour;

            var _endContours = new ushort[numContour];
            ushort pointCount = 0;

            //create contours
            for (ushort i = 0; i < numContour; ++i)
            {
                ushort numPoint = pntPerContours[pntContourIndex++];//increament pntContourIndex AFTER
                pointCount += numPoint;
                _endContours[i] = (ushort)(pointCount - 1);
            }

            //collect point for our contours
            var _glyphPoints = new GlyphPointF[pointCount];
            int n = 0;
            for (int i = 0; i < numContour; ++i)
            {
                //read point detail
                //step 3)

                //foreach contour
                //read 1 byte flags for each contour

                //1) The most significant bit of a flag indicates whether the point is on- or off-curve point,
                //2) the remaining seven bits of the flag determine the format of X and Y coordinate values and
                //specify 128 possible combinations of indices that have been assigned taking into consideration
                //typical statistical distribution of data found in TrueType fonts.

                //When X and Y coordinate values are recorded using nibbles(either 4 bits per coordinate or 12 bits per coordinate)
                //the bits are packed in the byte stream with most significant bit of X coordinate first,
                //followed by the value for Y coordinate (most significant bit first).
                //As a result, the size of the glyph dataset is significantly reduced,
                //and the grouping of the similar values(flags, coordinates) in separate and contiguous data streams allows
                //more efficient application of the entropy coding applied as the second stage of encoding process.

                int endContour = _endContours[i];
                for (; n <= endContour; ++n)
                {
                    byte f = flagStream[flagStreamIndex++]; //increment the flagStreamIndex AFTER read

                    //int f1 = (f >> 7); // most significant 1 bit -> on/off curve

                    int xyFormat = f & 0x7F; // remainging 7 bits x,y format

                    TripleEncodingRecord enc = s_encTable[xyFormat]; //0-128

                    byte[] packedXY = glyphStreamReader.ReadBytes(enc.ByteCount - 1); //byte count include 1 byte flags, so actual read=> byteCount-1
                                                                                      //read x and y

                    int x = 0;
                    int y = 0;

                    switch (enc.XBits)
                    {
                        default:
                            throw new System.NotSupportedException();//???
                        case 0: //0,8,
                            x = 0;
                            y = enc.Ty(packedXY[0]);
                            break;

                        case 4: //4,4
                            x = enc.Tx(packedXY[0] >> 4);
                            y = enc.Ty(packedXY[0] & 0xF);
                            break;

                        case 8: //8,0 or 8,8
                            x = enc.Tx(packedXY[0]);
                            y = (enc.YBits == 8) ?
                                    enc.Ty(packedXY[1]) :
                                    0;
                            break;

                        case 12: //12,12
                                 //x = enc.Tx((packedXY[0] << 8) | (packedXY[1] >> 4));
                                 //y = enc.Ty(((packedXY[1] & 0xF)) | (packedXY[2] >> 4));
                            x = enc.Tx((packedXY[0] << 4) | (packedXY[1] >> 4));
                            y = enc.Ty(((packedXY[1] & 0xF) << 8) | (packedXY[2]));
                            break;

                        case 16: //16,16
                            x = enc.Tx((packedXY[0] << 8) | packedXY[1]);
                            y = enc.Ty((packedXY[2] << 8) | packedXY[3]);
                            break;
                    }

                    //incremental point format***
                    _glyphPoints[n] = new GlyphPointF(curX += x, curY += y, (f >> 7) == 0); // most significant 1 bit -> on/off curve
                }
            }

            //----
            //step 4) Read one 255UInt16 value from the glyph stream, which is instructionLength, the number of instruction bytes.
            tmpGlyph.instructionLen = Woff2Utils.Read255UInt16(glyphStreamReader);
            //step 5) resolve it later

            return new Glyph(_glyphPoints,
               _endContours,
               new Bounds(), //calculate later
               null,  //load instruction later
               tmpGlyph.glyphIndex);
        }

        static bool CompositeHasInstructions(BinaryReader reader, ushort compositeGlyphIndex)
        {
            //To find if a composite has instruction or not.

            //This method is similar to  ReadCompositeGlyph() (below)
            //but this dose not create actual composite glyph.

            Glyf.CompositeGlyphFlags flags;
            do
            {
                flags = (Glyf.CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();
                short arg1 = 0;
                short arg2 = 0;
                ushort arg1and2 = 0;

                if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.ARG_1_AND_2_ARE_WORDS))
                {
                    arg1 = reader.ReadInt16();
                    arg2 = reader.ReadInt16();
                }
                else
                {
                    arg1and2 = reader.ReadUInt16();
                }
                //-----------------------------------------
                float xscale = 1;
                float scale01 = 0;
                float scale10 = 0;
                float yscale = 1;

                bool useMatrix = false;
                //-----------------------------------------
                bool hasScale = false;
                if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_A_SCALE))
                {
                    //If the bit WE_HAVE_A_SCALE is set,
                    //the scale value is read in 2.14 format-the value can be between -2 to almost +2.
                    //The glyph will be scaled by this value before grid-fitting.
                    xscale = yscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    hasScale = true;
                }
                else if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_AN_X_AND_Y_SCALE))
                {
                    xscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    yscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    hasScale = true;
                }
                else if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_A_TWO_BY_TWO))
                {
                    //The bit WE_HAVE_A_TWO_BY_TWO allows for linear transformation of the X and Y coordinates by specifying a 2 × 2 matrix.
                    //This could be used for scaling and 90-degree*** rotations of the glyph components, for example.

                    //2x2 matrix

                    //The purpose of USE_MY_METRICS is to force the lsb and rsb to take on a desired value.
                    //For example, an i-circumflex (U+00EF) is often composed of the circumflex and a dotless-i.
                    //In order to force the composite to have the same metrics as the dotless-i,
                    //set USE_MY_METRICS for the dotless-i component of the composite.
                    //Without this bit, the rsb and lsb would be calculated from the hmtx entry for the composite
                    //(or would need to be explicitly set with TrueType instructions).

                    //Note that the behavior of the USE_MY_METRICS operation is undefined for rotated composite components.
                    useMatrix = true;
                    hasScale = true;
                    xscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    scale01 = reader.ReadF2Dot14(); /* Format 2.14 */
                    scale10 = reader.ReadF2Dot14();/* Format 2.14 */
                    yscale = reader.ReadF2Dot14(); /* Format 2.14 */
                }
            } while (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.MORE_COMPONENTS));

            //
            return Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_INSTRUCTIONS);
        }

        static Glyph ReadCompositeGlyph(Glyph[] createdGlyphs, BinaryReader reader, ushort compositeGlyphIndex, Glyph emptyGlyph)
        {
            //Decoding of Composite Glyphs
            //For a composite glyph(nContour == -1), the following steps take the place of (Building Simple Glyph, steps 1 - 5 above):

            //1a.Read a UInt16 from compositeStream.
            //  This is interpreted as a component flag word as in the TrueType spec.
            //  Based on the flag values, there are between 4 and 14 additional argument bytes,
            //  interpreted as glyph index, arg1, arg2, and optional scale or affine matrix.

            //2a.Read the number of argument bytes as determined in step 2a from the composite stream,
            //and store these in the reconstructed glyph.
            //If the flag word read in step 2a has the FLAG_MORE_COMPONENTS bit(bit 5) set, go back to step 2a.

            //3a.If any of the flag words had the FLAG_WE_HAVE_INSTRUCTIONS bit(bit 8) set,
            //then read the instructions from the glyph and store them in the reconstructed glyph,
            //using the same process as described in steps 4 and 5 above (see Building Simple Glyph).

            Glyph finalGlyph = null;
            Glyf.CompositeGlyphFlags flags;
            do
            {
                flags = (Glyf.CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();
                if (createdGlyphs[glyphIndex] == null)
                {
                    // This glyph is not read yet, resolve it first!
                    long storedOffset = reader.BaseStream.Position;
                    Glyph missingGlyph = ReadCompositeGlyph(createdGlyphs, reader, glyphIndex, emptyGlyph);
                    createdGlyphs[glyphIndex] = missingGlyph;
                    reader.BaseStream.Position = storedOffset;
                }

                Glyph newGlyph = Glyph.TtfOutlineGlyphClone(createdGlyphs[glyphIndex], compositeGlyphIndex);

                short arg1 = 0;
                short arg2 = 0;
                ushort arg1and2 = 0;

                if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.ARG_1_AND_2_ARE_WORDS))
                {
                    arg1 = reader.ReadInt16();
                    arg2 = reader.ReadInt16();
                }
                else
                {
                    arg1and2 = reader.ReadUInt16();
                }
                //-----------------------------------------
                float xscale = 1;
                float scale01 = 0;
                float scale10 = 0;
                float yscale = 1;

                bool useMatrix = false;
                //-----------------------------------------
                bool hasScale = false;
                if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_A_SCALE))
                {
                    //If the bit WE_HAVE_A_SCALE is set,
                    //the scale value is read in 2.14 format-the value can be between -2 to almost +2.
                    //The glyph will be scaled by this value before grid-fitting.
                    xscale = yscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    hasScale = true;
                }
                else if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_AN_X_AND_Y_SCALE))
                {
                    xscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    yscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    hasScale = true;
                }
                else if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_A_TWO_BY_TWO))
                {
                    //The bit WE_HAVE_A_TWO_BY_TWO allows for linear transformation of the X and Y coordinates by specifying a 2 × 2 matrix.
                    //This could be used for scaling and 90-degree*** rotations of the glyph components, for example.

                    //2x2 matrix

                    //The purpose of USE_MY_METRICS is to force the lsb and rsb to take on a desired value.
                    //For example, an i-circumflex (U+00EF) is often composed of the circumflex and a dotless-i.
                    //In order to force the composite to have the same metrics as the dotless-i,
                    //set USE_MY_METRICS for the dotless-i component of the composite.
                    //Without this bit, the rsb and lsb would be calculated from the hmtx entry for the composite
                    //(or would need to be explicitly set with TrueType instructions).

                    //Note that the behavior of the USE_MY_METRICS operation is undefined for rotated composite components.
                    useMatrix = true;
                    hasScale = true;
                    xscale = reader.ReadF2Dot14(); /* Format 2.14 */
                    scale01 = reader.ReadF2Dot14(); /* Format 2.14 */
                    scale10 = reader.ReadF2Dot14(); /* Format 2.14 */
                    yscale = reader.ReadF2Dot14(); /* Format 2.14 */

                    if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.UNSCALED_COMPONENT_OFFSET))
                    {
                    }
                    else
                    {
                    }
                    if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.USE_MY_METRICS))
                    {
                    }
                }

                //--------------------------------------------------------------------
                if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.ARGS_ARE_XY_VALUES))
                {
                    //Argument1 and argument2 can be either x and y offsets to be added to the glyph or two point numbers.
                    //x and y offsets to be added to the glyph
                    //When arguments 1 and 2 are an x and a y offset instead of points and the bit ROUND_XY_TO_GRID is set to 1,
                    //the values are rounded to those of the closest grid lines before they are added to the glyph.
                    //X and Y offsets are described in FUnits.

                    if (useMatrix)
                    {
                        //use this matrix
                        Glyph.TtfTransformWith2x2Matrix(newGlyph, xscale, scale01, scale10, yscale);
                        Glyph.TtfOffsetXY(newGlyph, arg1, arg2);
                    }
                    else
                    {
                        if (hasScale)
                        {
                            if (xscale == 1.0 && yscale == 1.0)
                            {
                            }
                            else
                            {
                                Glyph.TtfTransformWith2x2Matrix(newGlyph, xscale, 0, 0, yscale);
                            }
                            Glyph.TtfOffsetXY(newGlyph, arg1, arg2);
                        }
                        else
                        {
                            if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.ROUND_XY_TO_GRID))
                            {
                                //TODO: implement round xy to grid***
                                //----------------------------
                            }
                            //just offset***
                            Glyph.TtfOffsetXY(newGlyph, arg1, arg2);
                        }
                    }
                }
                else
                {
                    //two point numbers.
                    //the first point number indicates the point that is to be matched to the new glyph.
                    //The second number indicates the new glyph's “matched” point.
                    //Once a glyph is added,its point numbers begin directly after the last glyphs (endpoint of first glyph + 1)
                }

                //
                if (finalGlyph == null)
                {
                    finalGlyph = newGlyph;
                }
                else
                {
                    //merge
                    Glyph.TtfAppendGlyph(finalGlyph, newGlyph);
                }
            } while (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.MORE_COMPONENTS));

            //
            if (Glyf.HasFlag(flags, Glyf.CompositeGlyphFlags.WE_HAVE_INSTRUCTIONS))
            {
                //read this later
                //ushort numInstr = reader.ReadUInt16();
                //byte[] insts = reader.ReadBytes(numInstr);
                //finalGlyph.GlyphInstructions = insts;
            }

            return finalGlyph ?? emptyGlyph;
        }

        readonly struct TripleEncodingRecord
        {
            public readonly byte ByteCount;
            public readonly byte XBits;
            public readonly byte YBits;
            public readonly ushort DeltaX;
            public readonly ushort DeltaY;
            public readonly sbyte Xsign;
            public readonly sbyte Ysign;

            public TripleEncodingRecord(
                byte byteCount,
                byte xbits, byte ybits,
                ushort deltaX, ushort deltaY,
                sbyte xsign, sbyte ysign)
            {
                ByteCount = byteCount;
                XBits = xbits;
                YBits = ybits;
                DeltaX = deltaX;
                DeltaY = deltaY;
                Xsign = xsign;
                Ysign = ysign;
                //#if DEBUG
                //                debugIndex = -1;
                //#endif
            }

#if DEBUG

            //public int debugIndex;
            public override string ToString()
            {
                return ByteCount + " " + XBits + " " + YBits + " " + DeltaX + " " + DeltaY + " " + Xsign + " " + Ysign;
            }

#endif

            /// <summary>
            /// translate X
            /// </summary>
            /// <param name="orgX"></param>
            /// <returns></returns>
            public int Tx(int orgX) => (orgX + DeltaX) * Xsign;

            /// <summary>
            /// translate Y
            /// </summary>
            /// <param name="orgY"></param>
            /// <returns></returns>
            public int Ty(int orgY) => (orgY + DeltaY) * Ysign;
        }

        class TripleEncodingTable
        {
            private static TripleEncodingTable s_encTable;

            private List<TripleEncodingRecord> _records = new List<TripleEncodingRecord>();

            public static TripleEncodingTable GetEncTable()
            {
                if (s_encTable == null)
                {
                    s_encTable = new TripleEncodingTable();
                }
                return s_encTable;
            }

            private TripleEncodingTable()
            {
                BuildTable();

#if DEBUG
                if (_records.Count != 128)
                {
                    throw new System.Exception();
                }
                dbugValidateTable();
#endif
            }

#if DEBUG

            void dbugValidateTable()
            {
#if DEBUG
                for (int xyFormat = 0; xyFormat < 128; ++xyFormat)
                {
                    TripleEncodingRecord tripleRec = _records[xyFormat];
                    if (xyFormat < 84)
                    {
                        //0-83 inclusive
                        if ((tripleRec.ByteCount - 1) != 1)
                        {
                            throw new System.NotSupportedException();
                        }
                    }
                    else if (xyFormat < 120)
                    {
                        //84-119 inclusive
                        if ((tripleRec.ByteCount - 1) != 2)
                        {
                            throw new System.NotSupportedException();
                        }
                    }
                    else if (xyFormat < 124)
                    {
                        //120-123 inclusive
                        if ((tripleRec.ByteCount - 1) != 3)
                        {
                            throw new System.NotSupportedException();
                        }
                    }
                    else if (xyFormat < 128)
                    {
                        //124-127 inclusive
                        if ((tripleRec.ByteCount - 1) != 4)
                        {
                            throw new System.NotSupportedException();
                        }
                    }
                }

#endif
            }

#endif
            public TripleEncodingRecord this[int index] => _records[index];

            void BuildTable()
            {
                // Each of the 128 index values define the following properties and specified in details in the table below:

                // Byte count(total number of bytes used for this set of coordinate values including one byte for 'flag' value).
                // Number of bits used to represent X coordinate value(X bits).
                // Number of bits used to represent Y coordinate value(Y bits).
                // An additional incremental amount to be added to X bits value(delta X).
                // An additional incremental amount to be added to Y bits value(delta Y).
                // The sign of X coordinate value(X sign).
                // The sign of Y coordinate value(Y sign).

                //Please note that “Byte Count” field reflects total size of the triplet(flag, xCoordinate, yCoordinate),
                //including ‘flag’ value that is encoded in a separate stream.

                //Triplet Encoding
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign

                //(set 1.1)
                //0     2            0       8       N/A       0     N/A     -
                //1                                            0             +
                //2                                           256            -
                //3                                           256            +
                //4                                           512            -
                //5                                           512            +
                //6                                           768            -
                //7                                           768            +
                //8                                           1024           -
                //9                                           1024           +
                BuildRecords(2, 0, 8, null, new ushort[] { 0, 256, 512, 768, 1024 }); //2*5

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 1.2)
                //10    2            8       0        0       N/A     -     N/A
                //11                                  0               +
                //12                                256               -
                //13                                256               +
                //14                                512               -
                //15                                512               +
                //16                                768               -
                //17                                768               +
                //18                                1024              -
                //19                                1024              +
                BuildRecords(2, 8, 0, new ushort[] { 0, 256, 512, 768, 1024 }, null); //2*5

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 2.1)
                //20    2           4       4        1        1       -      -
                //21                                          1       +      -
                //22                                          1       -      +
                //23                                          1       +      +
                //24                                          17      -      -
                //25                                          17      +      -
                //26                                          17      -      +
                //27                                          17      +      +
                //28                                          33      -      -
                //29                                          33      +      -
                //30                                          33      -      +
                //31                                          33      +      +
                //32                                          49      -      -
                //33                                          49      +      -
                //34                                          49      -      +
                //35                                          49      +      +
                BuildRecords(2, 4, 4, new ushort[] { 1 }, new ushort[] { 1, 17, 33, 49 });// 4*4 => 16 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 2.2)
                //36    2           4       4       17        1       -      -
                //37                                          1       +      -
                //38                                          1       -      +
                //39                                          1       +      +
                //40                                          17      -      -
                //41                                          17      +      -
                //42                                          17      -      +
                //43                                          17      +      +
                //44                                          33      -      -
                //45                                          33      +      -
                //46                                          33      -      +
                //47                                          33      +      +
                //48                                          49      -      -
                //49                                          49      +      -
                //50                                          49      -      +
                //51                                          49      +      +
                BuildRecords(2, 4, 4, new ushort[] { 17 }, new ushort[] { 1, 17, 33, 49 });// 4*4 => 16 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 2.3)
                //52    2           4          4     33        1      -      -
                //53                                           1      +      -
                //54                                           1      -      +
                //55                                           1      +      +
                //56                                          17      -      -
                //57                                          17      +      -
                //58                                          17      -      +
                //59                                          17      +      +
                //60                                          33      -      -
                //61                                          33      +      -
                //62                                          33      -      +
                //63                                          33      +      +
                //64                                          49      -      -
                //65                                          49      +      -
                //66                                          49      -      +
                //67                                          49      +      +
                BuildRecords(2, 4, 4, new ushort[] { 33 }, new ushort[] { 1, 17, 33, 49 });// 4*4 => 16 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 2.4)
                //68    2           4         4     49         1      -      -
                //69                                           1      +      -
                //70                                           1      -      +
                //71                                           1      +      +
                //72                                          17      -      -
                //73                                          17      +      -
                //74                                          17      -     +
                //75                                          17      +     +
                //76                                          33      -     -
                //77                                          33      +     -
                //78                                          33      -     +
                //79                                          33      +     +
                //80                                          49      -     -
                //81                                          49      +     -
                //82                                          49      -     +
                //83                                          49      +     +
                BuildRecords(2, 4, 4, new ushort[] { 49 }, new ushort[] { 1, 17, 33, 49 });// 4*4 => 16 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 3.1)
                //84    3             8       8         1      1      -     -
                //85                                           1      +     -
                //86                                           1      -     +
                //87                                           1      +     +
                //88                                         257      -     -
                //89                                         257      +     -
                //90                                         257      -     +
                //91                                         257      +     +
                //92                                         513      -     -
                //93                                         513      +     -
                //94                                         513      -     +
                //95                                         513      +     +
                BuildRecords(3, 8, 8, new ushort[] { 1 }, new ushort[] { 1, 257, 513 });// 4*3 => 12 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 3.2)
                //96    3               8       8      257      1     -      -
                //97                                            1     +      -
                //98                                            1     -      +
                //99                                            1     +      +
                //100                                         257     -      -
                //101                                         257     +      -
                //102                                         257     -      +
                //103                                         257     +      +
                //104                                         513     -      -
                //105                                         513     +      -
                //106                                         513     -      +
                //107                                         513     +      +
                BuildRecords(3, 8, 8, new ushort[] { 257 }, new ushort[] { 1, 257, 513 });// 4*3 => 12 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 3.3)
                //108   3              8        8       513     1     -      -
                //109                                           1     +      -
                //110                                           1     -      +
                //111                                           1     +      +
                //112                                         257     -      -
                //113                                         257     +      -
                //114                                         257     -      +
                //115                                         257     +      +
                //116                                         513     -      -
                //117                                         513     +      -
                //118                                         513     -      +
                //119                                         513     +      +
                BuildRecords(3, 8, 8, new ushort[] { 513 }, new ushort[] { 1, 257, 513 });// 4*3 => 12 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 4)
                //120   4               12     12         0      0    -      -
                //121                                                 +      -
                //122                                                 -      +
                //123                                                 +      +
                BuildRecords(4, 12, 12, new ushort[] { 0 }, new ushort[] { 0 }); // 4*1 => 4 records

                //---------------------------------------------------------------------
                //Index ByteCount   Xbits   Ybits   DeltaX  DeltaY  Xsign   Ysign
                //(set 5)
                //124   5               16      16      0       0     -      -
                //125                                                 +      -
                //126                                                 -      +
                //127                                                 +      +
                BuildRecords(5, 16, 16, new ushort[] { 0 }, new ushort[] { 0 });// 4*1 => 4 records
            }

            void AddRecord(byte byteCount, byte xbits, byte ybits, ushort deltaX, ushort deltaY, sbyte xsign, sbyte ysign)
            {
                var rec = new TripleEncodingRecord(byteCount, xbits, ybits, deltaX, deltaY, xsign, ysign);
#if DEBUG
                //rec.debugIndex = _records.Count;
#endif
                _records.Add(rec);
            }

            void BuildRecords(byte byteCount, byte xbits, byte ybits, ushort[] deltaXs, ushort[] deltaYs)
            {
                if (deltaXs == null)
                {
                    //(set 1.1)
                    for (int y = 0; y < deltaYs.Length; ++y)
                    {
                        AddRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, -1);
                        AddRecord(byteCount, xbits, ybits, 0, deltaYs[y], 0, 1);
                    }
                }
                else if (deltaYs == null)
                {
                    //(set 1.2)
                    for (int x = 0; x < deltaXs.Length; ++x)
                    {
                        AddRecord(byteCount, xbits, ybits, deltaXs[x], 0, -1, 0);
                        AddRecord(byteCount, xbits, ybits, deltaXs[x], 0, 1, 0);
                    }
                }
                else
                {
                    //set 2.1, - set5
                    for (int x = 0; x < deltaXs.Length; ++x)
                    {
                        ushort deltaX = deltaXs[x];

                        for (int y = 0; y < deltaYs.Length; ++y)
                        {
                            ushort deltaY = deltaYs[y];

                            AddRecord(byteCount, xbits, ybits, deltaX, deltaY, -1, -1);
                            AddRecord(byteCount, xbits, ybits, deltaX, deltaY, 1, -1);
                            AddRecord(byteCount, xbits, ybits, deltaX, deltaY, -1, 1);
                            AddRecord(byteCount, xbits, ybits, deltaX, deltaY, 1, 1);
                        }
                    }
                }
            }
        }
    }

    class TransformedLoca : UnreadTableEntry
    {
        public TransformedLoca(TableHeader header, Woff2TableDirectory tableDir) : base(header)
        {
            HasCustomContentReader = true;
            TableDir = tableDir;
        }

        public Woff2TableDirectory TableDir { get; }

        public override T CreateTableEntry<T>(BinaryReader reader, T expectedResult)
        {
            GlyphLocations loca = expectedResult as GlyphLocations;
            if (loca == null) throw new System.NotSupportedException();

            //nothing todo here :)
            return expectedResult;
        }
    }

    class Woff2Reader
    {
        private Woff2Header _header;

        public BrotliDecompressStreamFunc DecompressHandler;

        public Woff2Reader()
        {
#if DEBUG
            dbugVerifyKnownTables();
#endif
        }

#if DEBUG

        private static bool s_dbugPassVeriKnownTables;

        static void dbugVerifyKnownTables()
        {
            if (s_dbugPassVeriKnownTables)
            {
                return;
            }
            //--------------
            Dictionary<string, bool> uniqueNames = new Dictionary<string, bool>();
            foreach (string name in s_knownTableTags)
            {
                if (!uniqueNames.ContainsKey(name))
                {
                    uniqueNames.Add(name, true);
                }
                else
                {
                    throw new System.Exception();
                }
            }
        }

#endif

        public PreviewFontInfo ReadPreview(BinaryReader reader)
        {
            _header = ReadHeader(reader);
            if (_header == null) return null;  //=> return here and notify user too.
            Woff2TableDirectory[] woff2TablDirs = ReadTableDirectories(reader);
            if (DecompressHandler == null)
            {
                //if no Brotli decoder=> return here and notify user too.
                if (Woff2DefaultBrotliDecompressFunc.DecompressHandler != null)
                {
                    DecompressHandler = Woff2DefaultBrotliDecompressFunc.DecompressHandler;
                }
                else
                {
                    //return here and notify user too.
                    return null;
                }
            }

            //try read each compressed tables
            byte[] compressedBuffer = reader.ReadBytes((int)_header.totalCompressedSize);
            if (compressedBuffer.Length != _header.totalCompressedSize)
            {
                //error!
                return null; //can't read this, notify user too.
            }
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                if (!DecompressHandler(compressedBuffer, decompressedStream))
                {
                    //...Most notably,
                    //the data for the font tables is compressed in a SINGLE data stream comprising all the font tables.

                    //if not pass set to null
                    //decompressedBuffer = null;
                    return null;
                }
                //from decoded stream we read each table
                decompressedStream.Position = 0;//reset pos

                using (ByteOrderSwappingBinaryReader reader2 = new ByteOrderSwappingBinaryReader(decompressedStream))
                {
                    TableEntryCollection tableEntryCollection = CreateTableEntryCollection(woff2TablDirs);
                    OpenFontReader openFontReader = new OpenFontReader();
                    return openFontReader.ReadPreviewFontInfo(tableEntryCollection, reader2);
                }
            }
        }

        internal bool Read(Typeface typeface, BinaryReader reader, RestoreTicket ticket)
        {
            _header = ReadHeader(reader);
            if (_header == null) { return false; }  //=> return here and notify user too.

            Woff2TableDirectory[] woff2TablDirs = ReadTableDirectories(reader);
            if (DecompressHandler == null)
            {
                //if no Brotli decoder=> return here and notify user too.
                if (Woff2DefaultBrotliDecompressFunc.DecompressHandler != null)
                {
                    DecompressHandler = Woff2DefaultBrotliDecompressFunc.DecompressHandler;
                }
                else
                {
                    //return here and notify user too.
                    return false;
                }
            }

            byte[] compressedBuffer = reader.ReadBytes((int)_header.totalCompressedSize);
            if (compressedBuffer.Length != _header.totalCompressedSize)
            {
                //error!
                return false; //can't read this, notify user too.
            }

            using (MemoryStream decompressedStream = new MemoryStream())
            {
                if (!DecompressHandler(compressedBuffer, decompressedStream))
                {
                    //...Most notably,
                    //the data for the font tables is compressed in a SINGLE data stream comprising all the font tables.

                    //if not pass set to null
                    //decompressedBuffer = null;
                    return false;
                }
                //from decoded stream we read each table
                decompressedStream.Position = 0;//reset pos

                using (ByteOrderSwappingBinaryReader reader2 = new ByteOrderSwappingBinaryReader(decompressedStream))
                {
                    TableEntryCollection tableEntryCollection = CreateTableEntryCollection(woff2TablDirs);
                    OpenFontReader openFontReader = new OpenFontReader();
                    return openFontReader.ReadTableEntryCollection(typeface, ticket, tableEntryCollection, reader2);
                }
            }
        }

        Woff2Header ReadHeader(BinaryReader reader)
        {
            //WOFF2 Header
            //UInt32  signature             0x774F4632 'wOF2'
            //UInt32  flavor                The "sfnt version" of the input font.
            //UInt32  length                Total size of the WOFF file.
            //UInt16  numTables             Number of entries in directory of font tables.
            //UInt16  reserved              Reserved; set to 0.
            //UInt32  totalSfntSize         Total size needed for the uncompressed font data, including the sfnt header,
            //                              directory, and font tables(including padding).
            //UInt32  totalCompressedSize   Total length of the compressed data block.
            //UInt16  majorVersion          Major version of the WOFF file.
            //UInt16  minorVersion          Minor version of the WOFF file.
            //UInt32  metaOffset            Offset to metadata block, from beginning of WOFF file.
            //UInt32  metaLength            Length of compressed metadata block.
            //UInt32  metaOrigLength        Uncompressed size of metadata block.
            //UInt32  privOffset            Offset to private data block, from beginning of WOFF file.
            //UInt32  privLength            Length of private data block.

            Woff2Header header = new Woff2Header();
            byte b0 = reader.ReadByte();
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();
            byte b3 = reader.ReadByte();
            if (!(b0 == 0x77 && b1 == 0x4f && b2 == 0x46 && b3 == 0x32))
            {
                return null;
            }

            header.flavor = reader.ReadUInt32BE(); // sfnt version
            string flavorName = Utils.TagToString(header.flavor);

            header.length = reader.ReadUInt32BE();
            header.numTables = reader.ReadUInt16BE();
            ushort reserved = reader.ReadUInt16BE();
            header.totalSfntSize = reader.ReadUInt32BE();
            header.totalCompressedSize = reader.ReadUInt32BE();

            header.majorVersion = reader.ReadUInt16BE();
            header.minorVersion = reader.ReadUInt16BE();

            header.metaOffset = reader.ReadUInt32BE();
            header.metaLength = reader.ReadUInt32BE();
            header.metaOriginalLength = reader.ReadUInt32BE();

            header.privOffset = reader.ReadUInt32BE();
            header.privLength = reader.ReadUInt32BE();

            return header;
        }

        Woff2TableDirectory[] ReadTableDirectories(BinaryReader reader)
        {
            uint tableCount = (uint)_header.numTables; //?
            var tableDirs = new Woff2TableDirectory[tableCount];

            long expectedTableStartAt = 0;

            for (int i = 0; i < tableCount; ++i)
            {
                //TableDirectoryEntry
                //UInt8         flags           table type and flags
                //UInt32        tag	            4-byte tag(optional)
                //UIntBase128   origLength      length of original table
                //UIntBase128   transformLength transformed length(if applicable)

                Woff2TableDirectory table = new Woff2TableDirectory();
                byte flags = reader.ReadByte();
                //The interpretation of the flags field is as follows.

                //Bits[0..5] contain an index to the "known tag" table,
                //which represents tags likely to appear in fonts.If the tag is not present in this table,
                //then the value of this bit field is 63.

                //interprete flags
                int knowTable = flags & 0x1F; //5 bits => known table or not

                table.Name = (knowTable < 63) ? s_knownTableTags[knowTable] : Utils.TagToString(reader.ReadUInt32()); //other tag

                //Bits 6 and 7 indicate the preprocessing transformation version number(0 - 3) that was applied to each table.

                //For all tables in a font, except for 'glyf' and 'loca' tables,
                //transformation version 0 indicates the null transform where the original table data is passed directly
                //to the Brotli compressor for inclusion in the compressed data stream.

                //For 'glyf' and 'loca' tables,
                //transformation version 3 indicates the null transform where the original table data was passed directly
                //to the Brotli compressor without applying any pre - processing defined in subclause 5.1 and subclause 5.3.

                //The transformed table formats and their associated transformation version numbers are
                //described in details in clause 5 of this specification.

                table.PreprocessingTransformation = (byte)((flags >> 5) & 0x3); //2 bits, preprocessing transformation

                table.ExpectedStartAt = expectedTableStartAt;
                //
                if (!ReadUIntBase128(reader, out table.origLength))
                {
                    //can't read 128=> error
                }

                switch (table.PreprocessingTransformation)
                {
                    default:
                        break;

                    case 0:
                        {
                            if (table.Name == Glyf._N)
                            {
                                if (!ReadUIntBase128(reader, out table.transformLength))
                                {
                                    //can't read 128=> error
                                }
                                expectedTableStartAt += table.transformLength;//***
                            }
                            else if (table.Name == GlyphLocations._N)
                            {
                                //BUT by spec, transform 'loca' MUST has transformLength=0
                                if (!ReadUIntBase128(reader, out table.transformLength))
                                {
                                    //can't read 128=> error
                                }
                                expectedTableStartAt += table.transformLength;//***
                            }
                            else
                            {
                                expectedTableStartAt += table.origLength;
                            }
                        }
                        break;

                    case 1:
                        {
                            expectedTableStartAt += table.origLength;
                        }
                        break;

                    case 2:
                        {
                            expectedTableStartAt += table.origLength;
                        }
                        break;

                    case 3:
                        {
                            expectedTableStartAt += table.origLength;
                        }
                        break;
                }
                tableDirs[i] = table;
            }

            return tableDirs;
        }

        static TableEntryCollection CreateTableEntryCollection(Woff2TableDirectory[] woffTableDirs)
        {
            TableEntryCollection tableEntryCollection = new TableEntryCollection();
            for (int i = 0; i < woffTableDirs.Length; ++i)
            {
                Woff2TableDirectory woffTableDir = woffTableDirs[i];
                UnreadTableEntry unreadTableEntry = null;

                if (woffTableDir.Name == Glyf._N && woffTableDir.PreprocessingTransformation == 0)
                {
                    //this is transformed glyf table,
                    //we need another techqniue
                    TableHeader tableHeader = new TableHeader(woffTableDir.Name, 0,
                                       (uint)woffTableDir.ExpectedStartAt,
                                       woffTableDir.transformLength);
                    unreadTableEntry = new TransformedGlyf(tableHeader, woffTableDir);
                }
                else if (woffTableDir.Name == GlyphLocations._N && woffTableDir.PreprocessingTransformation == 0)
                {
                    //this is transformed glyf table,
                    //we need another techqniue
                    TableHeader tableHeader = new TableHeader(woffTableDir.Name, 0,
                                       (uint)woffTableDir.ExpectedStartAt,
                                       woffTableDir.transformLength);
                    unreadTableEntry = new TransformedLoca(tableHeader, woffTableDir);
                }
                else
                {
                    TableHeader tableHeader = new TableHeader(woffTableDir.Name, 0,
                                          (uint)woffTableDir.ExpectedStartAt,
                                          woffTableDir.origLength);
                    unreadTableEntry = new UnreadTableEntry(tableHeader);
                }
                tableEntryCollection.AddEntry(unreadTableEntry);
            }

            return tableEntryCollection;
        }

        private static readonly string[] s_knownTableTags = new string[]
        {
             //Known Table Tags
            //Flag  Tag         Flag  Tag       Flag  Tag        Flag    Tag
            //0	 => cmap,	    16 =>EBLC,	    32 =>CBDT,	     48 =>gvar,
            //1  => head,	    17 =>gasp,	    33 =>CBLC,	     49 =>hsty,
            //2	 => hhea,	    18 =>hdmx,	    34 =>COLR,	     50 =>just,
            //3	 => hmtx,	    19 =>kern,	    35 =>CPAL,	     51 =>lcar,
            //4	 => maxp,	    20 =>LTSH,	    36 =>SVG ,	     52 =>mort,
            //5	 => name,	    21 =>PCLT,	    37 =>sbix,	     53 =>morx,
            //6	 => OS/2,	    22 =>VDMX,	    38 =>acnt,	     54 =>opbd,
            //7	 => post,	    23 =>vhea,	    39 =>avar,	     55 =>prop,
            //8	 => cvt ,	    24 =>vmtx,	    40 =>bdat,	     56 =>trak,
            //9	 => fpgm,	    25 =>BASE,	    41 =>bloc,	     57 =>Zapf,
            //10 =>	glyf,	    26 =>GDEF,	    42 =>bsln,	     58 =>Silf,
            //11 =>	loca,	    27 =>GPOS,	    43 =>cvar,	     59 =>Glat,
            //12 =>	prep,	    28 =>GSUB,	    44 =>fdsc,	     60 =>Gloc,
            //13 =>	CFF ,	    29 =>EBSC,	    45 =>feat,	     61 =>Feat,
            //14 =>	VORG,	    30 =>JSTF,	    46 =>fmtx,	     62 =>Sill,
            //15 =>	EBDT,	    31 =>MATH,	    47 =>fvar,	     63 =>arbitrary tag follows,...
            //-------------------------------------------------------------------

            //-- TODO:implement missing table too!
            Cmap._N, //0
            Head._N, //1
            HorizontalHeader._N,//2
            HorizontalMetrics._N,//3
            MaxProfile._N,//4
            NameEntry._N,//5
            OS2Table._N, //6
            PostTable._N,//7
            CvtTable._N,//8
            FpgmTable._N,//9
            Glyf._N,//10
            GlyphLocations._N,//11
            PrepTable._N,//12
            CFFTable._N,//13
            "VORG",//14
            EBDT._N,//15,

            //---------------
            EBLC._N,//16
            Gasp._N,//17
            HorizontalDeviceMetrics._N,//18
            Kern._N,//19
            "LTSH",//20
            "PCLT",//21
            VerticalDeviceMetrics._N,//22
            VerticalHeader._N,//23
            VerticalMetrics._N,//24
            BASE._N,//25
            GDEF._N,//26
            GPOS._N,//27
            GSUB._N,//28
            EBSC._N, //29
            "JSTF", //30
            MathTable._N,//31
             //---------------

            //Known Table Tags (copy,same as above)
            //Flag  Tag         Flag  Tag       Flag  Tag        Flag    Tag
            //0	 => cmap,	    16 =>EBLC,	    32 =>CBDT,	     48 =>gvar,
            //1  => head,	    17 =>gasp,	    33 =>CBLC,	     49 =>hsty,
            //2	 => hhea,	    18 =>hdmx,	    34 =>COLR,	     50 =>just,
            //3	 => hmtx,	    19 =>kern,	    35 =>CPAL,	     51 =>lcar,
            //4	 => maxp,	    20 =>LTSH,	    36 =>SVG ,	     52 =>mort,
            //5	 => name,	    21 =>PCLT,	    37 =>sbix,	     53 =>morx,
            //6	 => OS/2,	    22 =>VDMX,	    38 =>acnt,	     54 =>opbd,
            //7	 => post,	    23 =>vhea,	    39 =>avar,	     55 =>prop,
            //8	 => cvt ,	    24 =>vmtx,	    40 =>bdat,	     56 =>trak,
            //9	 => fpgm,	    25 =>BASE,	    41 =>bloc,	     57 =>Zapf,
            //10 =>	glyf,	    26 =>GDEF,	    42 =>bsln,	     58 =>Silf,
            //11 =>	loca,	    27 =>GPOS,	    43 =>cvar,	     59 =>Glat,
            //12 =>	prep,	    28 =>GSUB,	    44 =>fdsc,	     60 =>Gloc,
            //13 =>	CFF ,	    29 =>EBSC,	    45 =>feat,	     61 =>Feat,
            //14 =>	VORG,	    30 =>JSTF,	    46 =>fmtx,	     62 =>Sill,
            //15 =>	EBDT,	    31 =>MATH,	    47 =>fvar,	     63 =>arbitrary tag follows,...
            //-------------------------------------------------------------------

            CBDT._N, //32
            CBLC._N,//33
            COLR._N,//34
            CPAL._N,//35,
            SvgTable._N,//36
            "sbix",//37
            "acnt",//38
            "avar",//39
            "bdat",//40
            "bloc",//41
            "bsln",//42
            "cvar",//43
            "fdsc",//44
            "feat",//45
            "fmtx",//46
            "fvar",//47
             //---------------

            "gvar",//48
            "hsty",//49
            "just",//50
            "lcar",//51
            "mort",//52
            "morx",//53
            "opbd",//54
            "prop",//55
            "trak",//56
            "Zapf",//57
            "Silf",//58
            "Glat",//59
            "Gloc",//60
            "Feat",//61
            "Sill",//62
            "...." //63 arbitrary tag follows
        };

        static bool ReadUIntBase128(BinaryReader reader, out uint result)
        {
            //UIntBase128 Data Type

            //UIntBase128 is a different variable length encoding of unsigned integers,
            //suitable for values up to 2^(32) - 1.

            //A UIntBase128 encoded number is a sequence of bytes for which the most significant bit
            //is set for all but the last byte,
            //and clear for the last byte.

            //The number itself is base 128 encoded in the lower 7 bits of each byte.
            //Thus, a decoding procedure for a UIntBase128 is:
            //start with value = 0.
            //Consume a byte, setting value = old value times 128 + (byte bitwise - and 127).
            //Repeat last step until the most significant bit of byte is false.

            //UIntBase128 encoding format allows a possibility of sub-optimal encoding,
            //where e.g.the same numerical value can be represented with variable number of bytes(utilizing leading 'zeros').
            //For example, the value 63 could be encoded as either one byte 0x3F or two(or more) bytes: [0x80, 0x3f].
            //An encoder must not allow this to happen and must produce shortest possible encoding.
            //A decoder MUST reject the font file if it encounters a UintBase128 - encoded value with leading zeros(a value that starts with the byte 0x80),
            //if UintBase128 - encoded sequence is longer than 5 bytes,
            //or if a UintBase128 - encoded value exceeds 232 - 1.

            //The "C-like" pseudo - code describing how to read the UIntBase128 format is presented below:
            //bool ReadUIntBase128(data, * result)
            //            {
            //                UInt32 accum = 0;

            //                for (i = 0; i < 5; i++)
            //                {
            //                    UInt8 data_byte = data.getNextUInt8();

            //                    // No leading 0's
            //                    if (i == 0 && data_byte = 0x80) return false;

            //                    // If any of top 7 bits are set then << 7 would overflow
            //                    if (accum & 0xFE000000) return false;

            //                    *accum = (accum << 7) | (data_byte & 0x7F);

            //                    // Spin until most significant bit of data byte is false
            //                    if ((data_byte & 0x80) == 0)
            //                    {
            //                        *result = accum;
            //                        return true;
            //                    }
            //                }
            //                // UIntBase128 sequence exceeds 5 bytes
            //                return false;
            //            }

            uint accum = 0;
            result = 0;
            for (int i = 0; i < 5; ++i)
            {
                byte data_byte = reader.ReadByte();
                // No leading 0's
                if (i == 0 && data_byte == 0x80) return false;

                // If any of top 7 bits are set then << 7 would overflow
                if ((accum & 0xFE000000) != 0) return false;
                //
                accum = (uint)(accum << 7) | (uint)(data_byte & 0x7F);
                // Spin until most significant bit of data byte is false
                if ((data_byte & 0x80) == 0)
                {
                    result = accum;
                    return true;
                }
                //
            }
            // UIntBase128 sequence exceeds 5 bytes
            return false;
        }
    }

    class Woff2Utils
    {
        private const byte ONE_MORE_BYTE_CODE1 = 255;
        private const byte ONE_MORE_BYTE_CODE2 = 254;
        private const byte WORD_CODE = 253;
        private const byte LOWEST_UCODE = 253;

        public static short[] ReadInt16Array(BinaryReader reader, int count)
        {
            short[] arr = new short[count];
            for (int i = 0; i < count; ++i)
            {
                arr[i] = reader.ReadInt16();
            }
            return arr;
        }

        public static ushort Read255UInt16(BinaryReader reader)
        {
            //255UInt16 Variable-length encoding of a 16-bit unsigned integer for optimized intermediate font data storage.
            //255UInt16 Data Type
            //255UInt16 is a variable-length encoding of an unsigned integer
            //in the range 0 to 65535 inclusive.
            //This data type is intended to be used as intermediate representation of various font values,
            //which are typically expressed as UInt16 but represent relatively small values.
            //Depending on the encoded value, the length of the data field may be one to three bytes,
            //where the value of the first byte either represents the small value itself or is treated as a code that defines the format of the additional byte(s).
            //The "C-like" pseudo-code describing how to read the 255UInt16 format is presented below:
            //   Read255UShort(data )
            //    {
            //                UInt8 code;
            //                UInt16 value, value2;

            //                const oneMoreByteCode1    = 255;
            //                const oneMoreByteCode2    = 254;
            //                const wordCode            = 253;
            //                const lowestUCode         = 253;

            //                code = data.getNextUInt8();
            //                if (code == wordCode)
            //                {
            //                    /* Read two more bytes and concatenate them to form UInt16 value*/
            //                    value = data.getNextUInt8();
            //                    value <<= 8;
            //                    value &= 0xff00;
            //                    value2 = data.getNextUInt8();
            //                    value |= value2 & 0x00ff;
            //                }
            //                else if (code == oneMoreByteCode1)
            //                {
            //                    value = data.getNextUInt8();
            //                    value = (value + lowestUCode);
            //                }
            //                else if (code == oneMoreByteCode2)
            //                {
            //                    value = data.getNextUInt8();
            //                    value = (value + lowestUCode * 2);
            //                }
            //                else
            //                {
            //                    value = code;
            //                }
            //                return value;
            //            }
            //Note that the encoding is not unique.For example,
            //the value 506 can be encoded as [255, 253], [254, 0], and[253, 1, 250].
            //An encoder may produce any of these, and a decoder MUST accept them all.An encoder should choose shorter encodings,
            //and must be consistent in choice of encoding for the same value, as this will tend to compress better.

            byte code = reader.ReadByte();
            if (code == WORD_CODE)
            {
                /* Read two more bytes and concatenate them to form UInt16 value*/
                //int value = (reader.ReadByte() << 8) & 0xff00;
                //int value2 = reader.ReadByte();
                //return (ushort)(value | (value2 & 0xff));
                int value = reader.ReadByte();
                value <<= 8;
                value &= 0xff00;
                int value2 = reader.ReadByte();
                value |= value2 & 0x00ff;

                return (ushort)value;
            }
            else if (code == ONE_MORE_BYTE_CODE1)
            {
                return (ushort)(reader.ReadByte() + LOWEST_UCODE);
            }
            else if (code == ONE_MORE_BYTE_CODE2)
            {
                return (ushort)(reader.ReadByte() + (LOWEST_UCODE * 2));
            }
            else
            {
                return code;
            }
        }
    }
}
