//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/gvar


    class GlyphVariableData
    {
        public List<ushort> _sharedPoints;
        public TupleVariationHeader[] tupleHeaders;

    }
    class GVar : TableEntry
    {
        public const string _N = "gvar";
        public override string Name => _N;

        public ushort axisCount;
        internal TupleRecord[] _sharedTuples;

        internal GlyphVariableData[] _glyphVarDataArr; //TODO: lazy load !

        public GVar()
        {

        }
        //
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //'gvar' header

            //The glyph variations table header format is as follows:

            //'gvar' header:
            //Type              Name                Description
            //uint16            majorVersion        Major version number of the glyph variations table — set to 1.
            //uint16            minorVersion        Minor version number of the glyph variations table — set to 0.
            //uint16            axisCount           The number of variation axes for this font.
            //                                      This must be the same number as axisCount in the 'fvar' table.
            //uint16            sharedTupleCount    The number of shared tuple records.
            //                                      Shared tuple records can be referenced within glyph variation data tables for multiple glyphs,
            //                                      as opposed to other tuple records stored directly within a glyph variation data table.
            //Offset32          sharedTuplesOffset  Offset from the start of this table to the shared tuple records.
            //uint16            glyphCount          The number of glyphs in this font.This must match the number of glyphs stored elsewhere in the font.
            //uint16            flags               Bit-field that gives the format of the offset array that follows.
            //                                      If bit 0 is clear, the offsets are uint16; 
            //                                      if bit 0 is set, the offsets are uint32.
            //Offset32          glyphVariationDataArrayOffset   Offset from the start of this table to the array of GlyphVariationData tables.
            //
            //Offset16-
            //-or- Offset32     glyphVariationDataOffsets[glyphCount + 1] Offsets from the start of the GlyphVariationData array to each GlyphVariationData table.
            //     
            //-------------


            long beginAt = reader.BaseStream.Position;

            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
#if DEBUG
            if (majorVersion != 1 && minorVersion != 0)
            {
                //WARN
            }
#endif

            axisCount = reader.ReadUInt16(); //This must be the same number as axisCount in the 'fvar' table

            ushort sharedTupleCount = reader.ReadUInt16();
            uint sharedTuplesOffset = reader.ReadUInt32();
            ushort glyphCount = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();
            uint glyphVariationDataArrayOffset = reader.ReadUInt32();

            uint[] glyphVariationDataOffsets = null;
            if ((flags & 0x1) == 0)
            {
                //bit 0 is clear-> use Offset16
                glyphVariationDataOffsets = reader.ReadUInt16ArrayAsUInt32Array(glyphCount);
                //
                //***If the short format (Offset16) is used for offsets, 
                //the value stored is the offset divided by 2.
                //Hence, the actual offset for the location of the GlyphVariationData table within the font 
                //will be the value stored in the offsets array multiplied by 2.

                for (int i = 0; i < glyphVariationDataOffsets.Length; ++i)
                {
                    glyphVariationDataOffsets[i] *= 2;
                }
            }
            else
            {
                //Offset32
                glyphVariationDataOffsets = reader.ReadUInt32Array(glyphCount);
            }

            reader.BaseStream.Position = beginAt + sharedTuplesOffset;
            ReadSharedTupleArray(reader, sharedTupleCount);


            //GlyphVariationData array ... 
            long glyphVariableData_startAt = beginAt + glyphVariationDataArrayOffset;
            reader.BaseStream.Position = glyphVariableData_startAt;

            _glyphVarDataArr = new GlyphVariableData[glyphVariationDataOffsets.Length];

            for (int i = 0; i < glyphVariationDataOffsets.Length; ++i)
            {
                reader.BaseStream.Position = glyphVariableData_startAt + glyphVariationDataOffsets[i];
                _glyphVarDataArr[i] = ReadGlyphVariationData(reader);
            }

        }
        void ReadSharedTupleArray(BinaryReader reader, ushort sharedTupleCount)
        {
            //-------------
            //Shared tuples array
            //-------------
            //The shared tuples array provides a set of variation-space positions
            //that can be referenced by variation data for any glyph. 
            //The shared tuples array follows the GlyphVariationData offsets array
            //at the end of the 'gvar' header.
            //This data is simply an array of tuple records, each representing a position in the font’s variation space.

            //Shared tuples array:
            //Type            Name                            Description
            //TupleRecord     sharedTuples[sharedTupleCount]  Array of tuple records shared across all glyph variation data tables.

            //Tuple records that are in the shared array or
            //that are contained directly within a given glyph variation data table 
            //use 2.14 values to represent normalized coordinate values.
            //See the Common Table Formats chapter for details.

            //

            //Tuple Records
            //https://docs.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats
            //The tuple variation store formats make reference to regions within the font’s variation space using tuple records. 
            //These references identify positions in terms of normalized coordinates, which use F2DOT14 values.
            //Tuple record(F2DOT14):
            //Type Name    Description
            //F2DOT14     coordinates[axisCount]  Coordinate array specifying a position within the font’s variation space.
            //                                    The number of elements must match the axisCount specified in the 'fvar' table.


            TupleRecord[] tupleRecords = new TupleRecord[sharedTupleCount];
            for (int t = 0; t < sharedTupleCount; ++t)
            {
                tupleRecords[t] = TupleRecord.ReadTupleRecord(reader, axisCount);
            }

            _sharedTuples = tupleRecords;
        }


        static void ReadPackedPoints(BinaryReader reader, List<ushort> packPoints)
        {
            byte b0 = reader.ReadByte();
            if (b0 == 0)
            {
                //If the first byte is 0, then a second count byte is not used. 
                //This value has a special meaning: the tuple variation data provides deltas for all glyph points (including the “phantom” points), or for all CVTs.

            }
            else if (b0 > 0 && b0 <= 127)
            {
                //If the first byte is non-zero and the high bit is clear (value is 1 to 127), 
                //then a second count byte is not used. 
                //The point count is equal to the value of the first byte. 
                ReadPackedPoints(reader, b0, packPoints);
            }
            else
            {
                //If the high bit of the first byte is set, then a second byte is used.
                //The count is read from interpreting the two bytes as a big-endian uint16 value with the high-order bit masked out.  

                //Thus, if the count fits in 7 bits, it is stored in a single byte, with the value 0 having a special interpretation.
                //If the count does not fit in 7 bits, then the count is stored in the first two bytes with the high bit of the first byte set as a flag 
                //that is not part of the count — the count uses 15 bits.

                byte b1 = reader.ReadByte();
                ReadPackedPoints(reader, ((b0 & 0x7F) << 8) | b1, packPoints);
            }
        }
        static void ReadPackedPoints(BinaryReader reader, int point_count, List<ushort> packPoints)
        {

            int point_read = 0;
            //for (int n = 0; n < point_count ; ++n)
            while (point_read < point_count)
            {
                //Point number data runs follow after the count.

                //Each data run begins with a control byte that specifies the number of point numbers defined in the run,
                //and a flag bit indicating the format of the run data. 
                //The control byte’s high bit specifies whether the run is represented in 8-bit or 16-bit values. 
                //The low 7 bits specify the number of elements in the run minus 1.
                //The format of the control byte is as follows:

                byte controlByte = reader.ReadByte();

                //Mask 	Name 	                Description
                //0x80 	POINTS_ARE_WORDS 	    Flag indicating the data type used for point numbers in this run.
                //                              If set, the point numbers are stored as unsigned 16-bit values (uint16); 
                //                              if clear, the point numbers are stored as unsigned bytes (uint8).
                //0x7F 	POINT_RUN_COUNT_MASK 	Mask for the low 7 bits of the control byte to give the number of point number elements, minus 1.


                int point_run_count = (controlByte & 0x7F) + 1;
                //In the first point run, the first point number is represented directly (that is, as a difference from zero). 
                //Each subsequent point number in that run is stored as the difference between it and the previous point number. 
                //In subsequent runs, all elements, including the first, represent a difference from the last point number.

                if (((controlByte & 0x80) == 0x80)) //point_are_uint16
                {
                    for (int i = 0; i < point_run_count; ++i)
                    {
                        point_read++;
                        packPoints.Add(reader.ReadUInt16());
                    }
                }
                else
                {
                    for (int i = 0; i < point_run_count; ++i)
                    {
                        point_read++;
                        packPoints.Add(reader.ReadByte());
                    }
                }
            }
        }

        GlyphVariableData ReadGlyphVariationData(BinaryReader reader)
        {
            //https://docs.microsoft.com/en-gb/typography/opentype/spec/otvarcommonformats#tuple-records
            //------------
            //The glyphVariationData table array
            //The glyphVariationData table array follows the 'gvar' header and shared tuples array.
            //Each glyphVariationData table describes the variation data for a single glyph in the font.

            //GlyphVariationData header:
            //Type                  Name                    Description
            //uint16                tupleVariationCount     A packed field.
            //                                              The high 4 bits are flags, 
            //                                              and the low 12 bits are the number of tuple variation tables for this glyph.
            //                                              The number of tuple variation tables can be any number between 1 and 4095.
            //Offset16              dataOffset              Offset from the start of the GlyphVariationData table to the serialized data
            //TupleVariationHeader  tupleVariationHeaders[tupleCount]   Array of tuple variation headers.

            GlyphVariableData glyphVarData = new GlyphVariableData();

            long beginAt = reader.BaseStream.Position;
            ushort tupleVariationCount = reader.ReadUInt16();
            ushort dataOffset = reader.ReadUInt16();


            //The tupleVariationCount field contains a packed value that includes flags and the number of 
            //logical tuple variation tables — which is also the number of physical tuple variation headers.
            //The format of the tupleVariationCount value is as follows:
            //Table 4
            //Mask 	    Name 	            Description
            //0x8000 	SHARED_POINT_NUMBERS 	Flag indicating that some or all tuple variation tables reference a shared set of “point” numbers.
            //                                  These shared numbers are represented as packed point number data at the start of the serialized data.***
            //0x7000 	Reserved 	        Reserved for future use — set to 0.
            //0x0FFF 	COUNT_MASK 	        Mask for the low bits to give the number of tuple variation tables.            


            int tupleCount = tupleVariationCount & 0xFFF;//low 12 bits are the number of tuple variation tables for this glyph

            TupleVariationHeader[] tupleHaders = new TupleVariationHeader[tupleCount];
            glyphVarData.tupleHeaders = tupleHaders;

            for (int i = 0; i < tupleCount; ++i)
            {
                tupleHaders[i] = TupleVariationHeader.Read(reader, axisCount);
            }

            //read glyph serialized data (https://docs.microsoft.com/en-gb/typography/opentype/spec/otvarcommonformats#serialized-data)

            reader.BaseStream.Position = beginAt + dataOffset;
            //
            //If the sharedPointNumbers flag is set,
            //then the serialized data following the header begins with packed “point” number data. 

            //In the context of a GlyphVariationData table within the 'gvar' table, 
            //these identify outline point numbers for which deltas are explicitly provided.
            //In the context of the 'cvar' table, these are interpreted as CVT indices rather than point indices. 
            //The format of packed point number data is described below.
            //....

            int flags = tupleVariationCount >> 12;//The high 4 bits are flags, 
            if ((flags & 0x8) == 0x8)//check the flags has SHARED_POINT_NUMBERS or not
            {

                //The serialized data block begins with shared “point” number data, 
                //followed by the variation data for the tuple variation tables.
                //The shared point number data is optional:
                //it is present if the corresponding flag is set in the tupleVariationCount field of the header.
                //If present, the shared number data is represented as packed point numbers, described below.

                //https://docs.microsoft.com/en-gb/typography/opentype/spec/otvarcommonformats#packed-point-numbers

                //...
                //Packed point numbers are stored as a count followed by one or more runs of point number data.

                //The count may be stored in one or two bytes.
                //After reading the first byte, the need for a second byte can be determined. 
                //The count bytes are processed as follows: 

                glyphVarData._sharedPoints = new List<ushort>();
                ReadPackedPoints(reader, glyphVarData._sharedPoints);
            }

            for (int i = 0; i < tupleCount; ++i)
            {
                TupleVariationHeader header = tupleHaders[i];

                ushort dataSize = header.variableDataSize;
                long expect_endAt = reader.BaseStream.Position + dataSize;

#if DEBUG
                if (expect_endAt > reader.BaseStream.Length)
                {

                }
#endif
                //The variationDataSize value indicates the size of serialized data for the given tuple variation table that is contained in the serialized data. 
                //It does not include the size of the TupleVariationHeader.

                if ((header.flags & ((int)TupleIndexFormat.PRIVATE_POINT_NUMBERS >> 12)) == ((int)TupleIndexFormat.PRIVATE_POINT_NUMBERS) >> 12)
                {
                    List<ushort> privatePoints = new List<ushort>();
                    ReadPackedPoints(reader, privatePoints);
                    header.PrivatePoints = privatePoints.ToArray();
                }
                else if (header.flags != 0)
                {

                }

                //Packed Deltas
                //Packed deltas are stored as a series of runs. Each delta run consists of a control byte followed by the actual delta values of that run.
                //The control byte is a packed value with flags in the high two bits and a count in the low six bits.
                //The flags specify the data size of the delta values in the run. The format of the control byte is as follows:
                //Packed Deltas
                //Mask 	Name 	            Description
                //0x80 	DELTAS_ARE_ZERO 	Flag indicating that this run contains no data (no explicit delta values are stored), and that all of the deltas for this run are zero.
                //0x40 	DELTAS_ARE_WORDS 	Flag indicating the data type for delta values in the run. If set, the run contains 16-bit signed deltas (int16); if clear, the run contains 8-bit signed deltas (int8).
                //0x3F 	DELTA_RUN_COUNT_MASK 	Mask for the low 6 bits to provide the number of delta values in the run, minus one.

                List<short> packedDeltasXY = new List<short>();
                while (reader.BaseStream.Position < expect_endAt)
                {
                    byte controlByte = reader.ReadByte();
                    int number_in_run = (controlByte & 0x3F) + 1;

                    int flags01 = (controlByte >> 6) << 6;

                    if (flags01 == 0x80)
                    {
                        for (int nn = 0; nn < number_in_run; ++nn)
                        {
                            packedDeltasXY.Add(0);
                        }
                    }
                    else if (flags01 == 0x40)
                    {
                        //DELTAS_ARE_WORDS Flag indicating the data type for delta values in the run.If set,
                        //the run contains 16 - bit signed deltas(int16);
                        //if clear, the run contains 8 - bit signed deltas(int8).

                        for (int nn = 0; nn < number_in_run; ++nn)
                        {
                            packedDeltasXY.Add(reader.ReadInt16());
                        }
                    }
                    else if (flags01 == 0)
                    {
                        for (int nn = 0; nn < number_in_run; ++nn)
                        {
                            packedDeltasXY.Add(reader.ReadByte());
                        }
                    }
                    else
                    {

                    }
                }
                //---
                header.PackedDeltasXY = packedDeltasXY.ToArray();

#if DEBUG
                //ensure!
                if ((packedDeltasXY.Count % 2) != 0)
                {
                    System.Diagnostics.Debugger.Break();
                }
                //ensure!
                if (reader.BaseStream.Position != expect_endAt)
                {
                    System.Diagnostics.Debugger.Break();
                }
#endif
            }

            return glyphVarData;
        }
    }
}


