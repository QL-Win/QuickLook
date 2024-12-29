//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats
    //Tuple variation stores are used in the 'gvar' and 'cvar' tables,
    //and organize sets of variation data into groupings,
    //each of which is associated with a particular region of applicability within the variation space.

    //Within the 'gvar' table, there is a separate variation store for each glyph.
    //Within the 'cvar' table, there is one variation store providing variations for all CVT values.


    //There is a minor difference in the top-level structure of the store in these two contexts.

    //Within the 'cvar' table, 
    //it is the entire 'cvar' table that comprises the specific variation store format, 
    //with a header that begins with major/minor version fields.

    //The specific variation store format for glyph-specific data within the 'gvar' table is 
    //the **GlyphVariationData table(one per glyph ID)**,which does not include any version fields.

    //In other respects, the 'cvar' table and GlyphVariationData table formats are the same.

    //There is also a minor difference in certain data that can occur in a GlyphVariationData table versus a 'cvar' table.
    //Differences between the 'gvar' and 'cvar' tables will be summarized later in this section


    //In terms of logical information content, 
    //the GlyphVariationData and 'cvar' tables consist of a set of logical, tuple variation data tables,
    //each for a particular region of the variation space.
    //In physical layout, however, the logical tuple variation tables are divided 
    //into separate parts that get stored separately: a header portion, and a serialized-data portion.

    //In terms of overall structure, the GlyphVariationData table and the 'cvar' table each begin with a header, 
    //which is followed by serialized data. 
    //The header includes an array with all of the tuple variation headers.
    //The serialized data include deltas and other data that will be explained below.

    //---------------------------------------------------
    //  GlyphVariationData table / 'cvar' table
    //          ---- header -----------
    //         (include tuple variation headers)
    //
    //          ---- serialized data---
    //          (adjustment deltas and other data)
    //
    //---------------------------------------------------
    //_fig: High-level organization of tuple variation stores_

    //Tuple Records

    //The tuple variation store formats make reference to regions within the font’s variation space using tuple records.
    //These references identify positions in terms of normalized coordinates, which use F2DOT14 values.

    //Tuple record(F2DOT14):
    //Type          Name                 Description
    //F2DOT14     coordinates[axisCount] Coordinate array specifying a position within the font’s variation space.
    //                                   The number of elements must match the axisCount specified in the 'fvar' table.



    //----------------------------------------------------------------
    //Tuple Variation Store Header

    //The two variants of a tuple variation store header,
    //the GlyphVariationData table header and the 'cvar' header,
    //are only slightly different.The formats of each are as follows:


    //GlyphVariationData header: 
    //Type      Name                    Description
    //uint16    tupleVariationCount     A packed field. The high 4 bits are flags (see below), and the low 12 bits are the number of tuple variation tables for this glyph.The count can be any number between 1 and 4095.
    //Offset16  dataOffset              Offset from the start of the GlyphVariationData table to the serialized data.
    //TupleVariationHeader tupleVariationHeaders[tupleVariationCount]  Array of tuple variation headers.

    //'cvar' table header:
    //Type      Name                    Description
    //uint16    majorVersion            Major version number of the 'cvar' table — set to 1.
    //uint16    minorVersion            Minor version number of the 'cvar' table — set to 0.
    //uint16    tupleVariationCount     A packed field.The high 4 bits are flags (see below), and the low 12 bits are the number of tuple variation tables. The count can be any number between 1 and 4095.
    //Offset16  dataOffset              Offset from the start of the 'cvar' table to the serialized data.
    //TupleVariationHeader tupleVariationHeaders[tupleVariationCount]  Array of tuple variation headers.

    //The tupleVariationCount field contains a packed value that includes flags and the number of logical tuple variation tables — which is also the number of physical tuple variation headers.The format of the tupleVariationCount value is as follows:
    //Mask      Name                    Description
    //0x8000 	SHARED_POINT_NUMBERS    Flag indicating that some or all tuple variation tables reference a shared set of “point” numbers.
    //                              These shared numbers are represented as packed point number data at the start of the serialized data.
    //0x7000 	Reserved                Reserved for future use — set to 0.
    //0x0FFF 	COUNT_MASK              Mask     for the low bits to give the number of tuple variation tables.

    //If the sharedPointNumbers flag is set, 
    //then the serialized data following the header begins with packed “point” number data.

    //In the context of a GlyphVariationData table within the 'gvar' table,
    //these identify outline point numbers for which deltas are explicitly provided.

    //In the context of the 'cvar' table, these are interpreted as CVT indices rather than point indices. 
    //The format of packed point number data is described below.

    //TupleVariationHeader

    //The GlyphVariationData and 'cvar' header formats 
    //include an array of tuple variation headers.
    //The TupleVariationHeader format is as follows.

    class TupleVariationHeader
    {
        //TupleVariationHeader:
        //Type      Name                    Description
        //uint16    variationDataSize       The size in bytes of the serialized data for this tuple variation table.
        //uint16    tupleIndex              A packed field.
        //                                  The high 4 bits are flags(see below).
        //                                  The low 12 bits are an index into a shared tuple records array.
        //Tuple     peakTuple               Peak tuple record for this tuple variation table — optional, determined by flags in the tupleIndex value.
        //                                  Note that this must always be included in the 'cvar' table.
        //Tuple     intermediateStartTuple  Intermediate start tuple record for this tuple variation table — optional, determined by flags in the tupleIndex value.
        //Tuple     intermediateEndTuple    Intermediate end tuple record for this tuple variation table — optional, determined by flags in the tupleIndex value.

        public ushort variableDataSize;

        public int flags;
        public ushort indexToSharedTupleRecArray;

        public TupleRecord peakTuple;
        public TupleRecord intermediateStartTuple;
        public TupleRecord intermediateEndTuple;


        public static TupleVariationHeader Read(BinaryReader reader, int axisCount)
        {
            TupleVariationHeader header = new TupleVariationHeader();

            header.variableDataSize = reader.ReadUInt16();
            ushort tupleIndex = reader.ReadUInt16();
            int flags = (tupleIndex >> 12) & 0xF; //The high 4 bits are flags(see below).
            header.flags = flags; //The high 4 bits are flags(see below).
            header.indexToSharedTupleRecArray = (ushort)(tupleIndex & 0x0FFF); // The low 12 bits are an index into a shared tuple records array.


            if ((flags & ((int)TupleIndexFormat.EMBEDDED_PEAK_TUPLE >> 12)) == ((int)TupleIndexFormat.EMBEDDED_PEAK_TUPLE >> 12))
            {
                //TODO:...
                header.peakTuple = TupleRecord.ReadTupleRecord(reader, axisCount);
            }
            if ((flags & ((int)TupleIndexFormat.INTERMEDIATE_REGION >> 12)) == ((int)TupleIndexFormat.INTERMEDIATE_REGION >> 12))
            {
                //TODO:...
                header.intermediateStartTuple = TupleRecord.ReadTupleRecord(reader, axisCount);
                header.intermediateEndTuple = TupleRecord.ReadTupleRecord(reader, axisCount);
            }

            return header;
        }



        //---------
        public ushort[] PrivatePoints;
        public short[] PackedDeltasXY;


        //Note that the size of the TupleVariationHeader is variable, 
        //depending on whether peak or intermediate tuple records are included. (See below for more information.)

        //The variationDataSize value indicates the size of serialized data
        //for the given tuple variation table that is contained in the serialized data.
        //**It does not include the size of the TupleVariationHeader.**

        //Every tuple variation table has an associated peak tuple record.
        //Most tuple variation tables use non-intermediate regions, 
        //and so require only the peak tuple record to define the region.
        //- In the 'cvar' table, there is only one variation store, 
        //  and so any given region will only need to be referenced once. 
        //- Within the 'gvar' table, however, there is a GlyphVariationData table for each glyph ID, 
        //  and so any region may be referenced numerous times; 
        //  in fact, most regions will be referenced within the GlyphVariationData tables for most glyphs.
        //To provide a more efficient representation,
        //the tuple variation store formats allow for an array of tuple records,
        //stored outside the tuple variation store structures, 
        //that can be shared across many tuple variation stores.
        //This is used only within the 'gvar' table; it is not needed or supported in the 'cvar' table.
        //The formats alternately allow for a peak tuple record that is non-shared,
        //specific to the given tuple variation table, to be embedded directly within a TupleVariationHeader.
        //This is optional within the 'gvar' table, 
        //but required in the 'cvar' table, which does not use shared peak tuple records.

        //The tupleIndex field contains a packed value that includes flags and
        //an index into a shared tuple records array(not used in the 'cvar' table). 
        //The format of the tupleIndex field is as follows.
    }

    [Flags]
    enum TupleIndexFormat
    {
        //tupleIndex format:
        //Mask    Name                  Description
        //0x8000  EMBEDDED_PEAK_TUPLE   Flag indicating that this tuple variation header includes an embedded peak tuple record,
        //                              immediately after the tupleIndex field.
        //                              If set, the low 12 bits of the tupleIndex value are ignored.
        //                              Note that this must always be set within the 'cvar' table.
        //0x4000  INTERMEDIATE_REGION   Flag indicating that this tuple variation table applies to an intermediate region within the variation space. 
        //                              If set, the header includes the two intermediate - region, start and end tuple records, 
        //                              immediately after the peak tuple record(if present).
        //0x2000  PRIVATE_POINT_NUMBERS Flag indicating that the serialized data for this tuple variation table includes packed “point” number data.
        //                              If set, this tuple variation table uses that number data;
        //                              if clear, this tuple variation table uses shared number data found at the start of the serialized data
        //                              for this glyph variation data or 'cvar' table.
        //0x1000  Reserved              Reserved for future use — set to 0.
        //0x0FFF  TUPLE_INDEX_MASK      Mask for the low 12 bits to give the shared tuple records index.

        EMBEDDED_PEAK_TUPLE = 0x8000,
        INTERMEDIATE_REGION = 0x4000,
        PRIVATE_POINT_NUMBERS = 0x2000,
        Reserved = 0x1000,
        TUPLE_INDEX_MASK = 0x0FFF


        //Note that the intermediateRegion flag is independent of the embeddedPeakTuple flag or
        //the shared tuple records index. 
        //Every tuple variation table has a peak n-tuple indicated either by an embedded tuple record (always true in the 'cvar' table) or 
        //by an index into a shared tuple records array (only in the 'gvar' table). 
        //An intermediate-region tuple variation table additionally has start and end n-tuples that also get used in the interpolation process; 
        //these are always represented using embedded tuple records.

        //Also note that the privatePointNumbers flag is independent of the sharedPointNumbers flag in the tupleVariationCount field of 
        //the GlyphVariationData or 'cvar' header. 
        //A GlyphVariationData or 'cvar' table may have shared point number data used by multiple tuple variation tables, 
        //but any given tuple variation table may have private point number data that it uses instead.

        //As noted, the size of tuple variation headers is variable. The next TupleVariationHeader can be calculated as follows:

        //    const TupleVariationHeader*
        //    NextHeader( const TupleVariationHeader* currentHeader, int axisCount )
        //    {
        //        int bump = 2 * sizeof( uint16 );
        //        int tupleIndex = currentHeader->tupleIndex;
        //        if ( tupleIndex & embeddedPeakTuple )
        //            bump += axisCount * sizeof( F2DOT14 );
        //        if ( tupleIndex & intermediateRegion )
        //            bump += 2 * axisCount * sizeof( F2DOT14 );
        //        return (const TupleVariationHeader*)((char*)currentHeader + bump);
        //    }
    }

    readonly struct TupleRecord
    {
        public readonly float[] coords;
        public TupleRecord(float[] coords) => this.coords = coords;
#if DEBUG
        public override string ToString() => coords?.Length.ToString() ?? "0";
#endif
        public static TupleRecord ReadTupleRecord(BinaryReader reader, int count)
        {
            float[] coords = new float[count];
            for (int n = 0; n < coords.Length; ++n)
            {
                coords[n] = reader.ReadF2Dot14();
            }
            return new TupleRecord(coords);
        }
    }

    //----------------------------------------------------------------
    //Serialized Data

    //After the GlyphVariationData or 'cvar' header(including the TupleVariationHeader array) is 
    //a block of serialized data.The offset to this block of data is provided in the header.


    //The serialized data block begins with shared “point” number data, 
    //followed by the variation data for the tuple variation tables.
    //The shared point number data is optional:
    //it is present if the corresponding flag is set in the tupleVariationCount field of the header. 
    //If present, the shared number data is represented as packed point numbers, described below.

    //---------------------------------------------------
    //          Serialized data block
    //          ---- Shared "point" numbers -----------
    //         (optional per flag in the header)
    //
    //          ---- Per-tuple-variation data---  
    //
    //---------------------------------------------------
    //_fig: Organization of serialized data_

    //The remaining data contains runs of data specific to individual tuple variation tables,
    //in order of the tuple variation headers.
    //Each TupleVariationHeader indicates the data size for the corresponding run of data for that tuple variation table.

    //The per-tuple-variation-table data optionally begins with private “point” numbers, 
    //present if the privatePointNumbers flag is set in the tupleIndex field of the TupleVariationHeader.
    //Private point numbers are represented as packed point numbers, described below.

    //After the private point number data(if present), 
    //the tuple variation data will include packed delta data.
    //The format for packed deltas is given below.
    //- Within the 'gvar' table,
    //  there are packed deltas for X coordinates, followed by packed deltas for Y coordinates.
    //---------------------------------------------------
    //          Per-tuple-variation data (gvar)
    //          ---- Private point numbers -----------
    //         (optional per flag in tupleVariationHeader)
    //
    //          ---- X coordinate packed deltas---  
    //          
    //          ---- Y coordinate packed deltas---  
    //---------------------------------------------------
    //_fig: Organization 'gvar' per-tuple variation data_


    //- Within the 'cvar' table, there is one set of packed deltas
    //---------------------------------------------------
    //          Per-tuple-variation data (gvar)
    //          ---- Private point numbers -----------
    //         (optional per flag in tupleVariationHeader)
    //
    //          ---- X coordinate packed deltas---  
    //          
    //          ---- Y coordinate packed deltas---  
    //---------------------------------------------------
    //_fig: Organization 'cvar' per-tuple variation data_


    //The data size indicated in the TupleVariationHeader includes the size of the private point number data, 
    //if present, plus the size of the packed deltas.



    //---------------------------------------------------
    //Packed “Point” Numbers

    //Tuple variation data specify deltas to be applied to specific items: 
    //X and Y coordinates for glyph outline points within the 'gvar' table, and CVT values in the 'cvar' table.

    //For a given glyph, deltas may be provided for any or all of a glyph’s points, 
    //including “phantom” points generated within the rasterizer that represent glyph side bearing points.
    //(See the chapter Instructing TrueType Glyphs for more background on phantom points.) 

    //Similarly, within the 'cvar' table, deltas may be provided for any or all CVTs.
    //The set of glyph points or CVTs for which deltas are provided is specified by packed point numbers.

    //**Note: If a glyph is a composite glyph,
    //then “point” numbers are component indices for the components that make up the composite glyph.
    //See the 'gvar' table chapter for complete details. 

    //Likewise, in the context of the 'cvar' table, “point” numbers are indices for CVT entries.


    //Note: Within the 'gvar' table, 
    //if deltas are not provided explicitly for some points,
    //  then inferred delta values may need to be calculated — see the 'gvar' table chapter for details.
    //This does not apply to the 'cvar' table, however:
    //  if deltas are not provided for some CVT values,
    //  then no adjustments are made to those CVTs in connection with the particular tuple variation table.

    //Packed point numbers are stored as a count followed by one or more runs of point number data.


    //The count may be stored in one or two bytes. 
    //After reading the first byte, the need for a second byte can be determined.
    //The count bytes are processed as follows:

    //   If the first byte is 0, then ...
    //      a second count byte is not used.
    //      This value has a special meaning: 
    //      the tuple variation data provides deltas for all glyph points (including the “phantom” points), or for all CVTs.

    //   If the first byte is non-zero and the high bit is clear (value is 1 to 127), then ...
    //      a second count byte is **not used**.
    //      The point count is equal to the value of the first byte.

    //   If the high bit of the first byte is set, then ...
    //      a second byte is used.
    //      The count is read from interpreting the two bytes as a big-endian uint16 value with the high-order bit masked out.


    //Thus, if the count fits in 7 bits, 
    //it is stored in a single byte,
    //with the value 0 having a special interpretation.

    //If the count does not fit in 7 bits,
    //then the count is stored in the first two bytes with the high bit of the first byte set as a flag that is not part of the count — the count uses 15 bits.

    //For example, a count of 0x00 indicates that deltas are provided for all point numbers / all CVTs,
    //with no additional point number data required; 
    //a count of 0x32 indicates that there are a total of 50 point numbers specified; 
    //a count of 0x81 0x22 indicates that there are a total of 290 (= 0x0122) point numbers specified.


    //Point number data runs follow after the count.
    //Each data run begins with a control byte that specifies 
    //the number of point numbers defined in the run,
    //and a flag bit indicating the format of the run data. 
    //The control byte’s high bit specifies whether the run is represented in 8-bit or 16-bit values.
    //The low 7 bits specify the number of elements in the run minus 1. 
    //The format of the control byte is as follows:

    //Mask  Name                    Description
    //0x80 	POINTS_ARE_WORDS        Flag indicating the data type used for point numbers in this run.
    //                              If set, the point numbers are stored as unsigned 16-bit values (uint16);
    //                              if clear, the point numbers are stored as unsigned bytes (uint8).
    //0x7F 	POINT_RUN_COUNT_MASK    Mask for the low 7 bits of the control byte to give the number of point number elements, minus 1.


    //For example, a control byte of 0x02 indicates that the run has three elements represented as uint8 values; 
    //a control byte of 0xD4 indicates that the run has 0x54 + 1 = 85 elements represented as uint16 values.


    //In the first point run,..
    //  the first point number is represented directly (that is, as a difference from zero).
    //  Each subsequent point number in that run is stored as the difference between it and the previous point number. 
    //In subsequent runs,...
    //  all elements, including the first, represent a difference from the last point number.

    //Since the values in the packed data are all unsigned, 
    //point numbers will be given in increasing order.
    //Since the packed representation can include zero values, 
    //it is possible for a given point number to be repeated in the derived point number list. 
    //In that case, there will be multiple delta values in the deltas data associated with that point number.
    //All of these deltas must be applied cumulatively to the given point.


    //Packed Deltas

    //Tuple variation data specify deltas to be applied to glyph point coordinates or to CVT values.
    //As in the case of point number data, deltas are stored in a packed format.

    //Packed delta data does not include the total number of delta values within the data.
    //Logically, there are deltas for every point number or CVT index specified in the point-number data. 
    //Thus, the count of logical deltas is equal to the count of point numbers specified for that tuple variation table. 
    //But since the deltas are represented in a packed format, 
    //the actual count of stored values is typically less than the logical count.
    //The data is read until the expected logic count of deltas is obtained.

    // Note: In the 'gvar' table,
    // there will be two logical deltas for each point number:
    // one that applies to the X coordinate, and one that applies to the Y coordinate.
    // Therefore, the total logical delta count is two times the point number count.
    // The packed deltas are arranged with all of the deltas for X coordinates first, followed by the deltas for Y coordinates.

    //Packed deltas are stored as a series of runs.
    //Each delta run consists of a control byte followed by the actual delta values of that run. 
    //The control byte is a packed value with flags in the high two bits and a count in the low six bits. 

    //The flags specify the data size of the delta values in the run.
    //The format of the control byte is as follows:

    //Mask  Name                    Description
    //0x80 	DELTAS_ARE_ZERO         Flag indicating that this run contains no data (no explicit delta values are stored), and that all of the deltas for this run are zero.
    //0x40 	DELTAS_ARE_WORDS        Flag indicating the data type for delta values in the run. If set, the run contains 16-bit signed deltas (int16); if clear, the run contains 8-bit signed deltas (int8).
    //0x3F 	DELTA_RUN_COUNT_MASK    Mask for the low 6 bits to provide the number of delta values in the run, minus one.

    //...
    //...



    //-------------------------------------------------
    //Differences Between 'gvar' and 'cvar' Tables

    //The following is a summary of key differences between tuple variation stores in the 'gvar' and 'cvar' tables.

    //- The 'gvar' table is a parent table for tuple variation stores, 
    //  and contains one tuple variation store(the glyph variation data table) for each glyph ID.
    //  In contrast, the entire 'cvar' table is comprised of a single,
    //  slightly-extended(with version fields) tuple variation store.

    //- Because the 'gvar' table contains multiple tuple variation stores, 
    //  sharing of data between tuple variation stores is possible,
    //  and is used for shared tuple records.
    //  Because the 'cvar' table has a single tuple variation store, no possibility of shared data arises.

    //- The tupleIndex field of TupleVariationHeader structures within a tuple variation store includes a flag 
    //  that indicates whether the structure instance includes an embedded peak tuple record.
    //  In the 'gvar' table, this is optional.In the 'cvar' table, a peak tuple record is mandatory.

    //- The serialized data includes packed “point” numbers.
    //  In the 'gvar' table, these refer to glyph contour point numbers or,
    //  in the case of a composite glyph, to component indices.
    //  In the context of the 'cvar' table, these are indices for CVT entries.

    //- In the 'gvar' table, 
    //  point numbers cover the points or components defined in a 'glyf' entry plus four additional “phantom” points that 
    //  represent the glyph’s horizontal and vertical advance and side bearings. 
    //  (See the chapter, Instructing TrueType Glyphs for more background on phantom points.) 
    //  The last four point numbers for any glyph, including composite glyphs, are for the phantom points.

    //- In the 'gvar' table, 
    //  if deltas are not provided for some points and the point indices are not represented in the point number data,
    //  then interpolated deltas for those points will in some cases be inferred.
    //  This is not done in the 'cvar' table, however.

    //- In the 'gvar' table, 
    //  the serialized data for a given region has two logical deltas for each point number:
    //  one for the X coordinate, and one for the Y coordinate.
    //  Hence the total number of deltas is twice the count of control points.
    //  In the 'cvar' table, however, there is only one delta for each point number.



}