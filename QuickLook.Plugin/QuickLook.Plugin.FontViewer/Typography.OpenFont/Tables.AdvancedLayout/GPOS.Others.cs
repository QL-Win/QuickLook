//Apache2, 2016-present, WinterDev
using System;
using System.IO;
using System.Text;

//https://docs.microsoft.com/en-us/typography/opentype/spec/gpos

namespace Typography.OpenFont.Tables
{
    partial class GPOS
    {

        static PosRuleSetTable[] CreateMultiplePosRuleSetTables(long initPos, ushort[] offsets, BinaryReader reader)
        {
            int j = offsets.Length;
            PosRuleSetTable[] results = new PosRuleSetTable[j];
            for (int i = 0; i < j; ++i)
            {
                results[i] = PosRuleSetTable.CreateFrom(reader, initPos + offsets[i]);
            }
            return results;
        }

        static PosLookupRecord[] CreateMultiplePosLookupRecords(BinaryReader reader, int count)
        {

            PosLookupRecord[] results = new PosLookupRecord[count];
            for (int n = 0; n < count; ++n)
            {
                results[n] = PosLookupRecord.CreateFrom(reader);
            }
            return results;
        }


        class PairSetTable
        {
            internal PairSet[] _pairSets;
            public void ReadFrom(BinaryReader reader, ushort v1format, ushort v2format)
            {
                ushort rowCount = reader.ReadUInt16();
                _pairSets = new PairSet[rowCount];
                for (int i = 0; i < rowCount; ++i)
                {
                    //GlyphID 	    SecondGlyph 	GlyphID of second glyph in the pair-first glyph is listed in the Coverage table
                    //ValueRecord 	Value1 	        Positioning data for the first glyph in the pair
                    //ValueRecord 	Value2 	        Positioning data for the second glyph in the pair
                    ushort secondGlyph = reader.ReadUInt16();
                    ValueRecord v1 = ValueRecord.CreateFrom(reader, v1format);
                    ValueRecord v2 = ValueRecord.CreateFrom(reader, v2format);
                    //
                    _pairSets[i] = new PairSet(secondGlyph, v1, v2);
                }
            }
            public bool FindPairSet(ushort secondGlyphIndex, out PairSet foundPairSet)
            {
                int j = _pairSets.Length;
                for (int i = 0; i < j; ++i)
                {
                    //TODO: binary search?
                    if (_pairSets[i].secondGlyph == secondGlyphIndex)
                    {
                        //found
                        foundPairSet = _pairSets[i];
                        return true;
                    }
                }
                //
                foundPairSet = new PairSet();//empty
                return false;
            }
        }


        readonly struct PairSet
        {
            public readonly ushort secondGlyph;//GlyphID of second glyph in the pair-first glyph is listed in the Coverage table
            public readonly ValueRecord value1;//Positioning data for the first glyph in the pair
            public readonly ValueRecord value2;//Positioning data for the second glyph in the pair   
            public PairSet(ushort secondGlyph, ValueRecord v1, ValueRecord v2)
            {
                this.secondGlyph = secondGlyph;
                this.value1 = v1;
                this.value2 = v2;
            }
#if DEBUG
            public override string ToString()
            {
                return "second_glyph:" + secondGlyph;
            }
#endif
        }


        class ValueRecord
        {
            //ValueRecord (all fields are optional)
            //Value 	Type 	    Description
            //--------------------------------
            //int16 	XPlacement 	Horizontal adjustment for placement-in design units
            //int16 	YPlacement 	Vertical adjustment for placement, in design units
            //int16 	XAdvance 	Horizontal adjustment for advance, in design units (only used for horizontal writing)
            //int16 	YAdvance 	Vertical adjustment for advance, in design units (only used for vertical writing)
            //Offset16 	XPlaDevice 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for horizontal placement, from beginning of PosTable (may be NULL)
            //Offset16 	YPlaDevice 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for vertical placement, from beginning of PosTable (may be NULL)
            //Offset16 	XAdvDevice 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for horizontal advance, from beginning of PosTable (may be NULL)
            //Offset16 	YAdvDevice 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for vertical advance, from beginning of PosTable (may be NULL)

            public short XPlacement;
            public short YPlacement;
            public short XAdvance;
            public short YAdvance;
            public ushort XPlaDevice;
            public ushort YPlaDevice;
            public ushort XAdvDevice;
            public ushort YAdvDevice;

            ushort valueFormat;
            public void ReadFrom(BinaryReader reader, ushort valueFormat)
            {
                this.valueFormat = valueFormat;
                if (HasFormat(valueFormat, FMT_XPlacement))
                {
                    this.XPlacement = reader.ReadInt16();
                }
                if (HasFormat(valueFormat, FMT_YPlacement))
                {
                    this.YPlacement = reader.ReadInt16();
                }
                if (HasFormat(valueFormat, FMT_XAdvance))
                {
                    this.XAdvance = reader.ReadInt16();
                }
                if (HasFormat(valueFormat, FMT_YAdvance))
                {
                    this.YAdvance = reader.ReadInt16();
                }
                if (HasFormat(valueFormat, FMT_XPlaDevice))
                {
                    this.XPlaDevice = reader.ReadUInt16();
                }
                if (HasFormat(valueFormat, FMT_YPlaDevice))
                {
                    this.YPlaDevice = reader.ReadUInt16();
                }
                if (HasFormat(valueFormat, FMT_XAdvDevice))
                {
                    this.XAdvDevice = reader.ReadUInt16();
                }
                if (HasFormat(valueFormat, FMT_YAdvDevice))
                {
                    this.YAdvDevice = reader.ReadUInt16();
                }
            }
            static bool HasFormat(ushort value, int flags)
            {
                return (value & flags) == flags;
            }
            //Mask 	Name 	Description
            //0x0001 	XPlacement 	Includes horizontal adjustment for placement
            //0x0002 	YPlacement 	Includes vertical adjustment for placement
            //0x0004 	XAdvance 	Includes horizontal adjustment for advance
            //0x0008 	YAdvance 	Includes vertical adjustment for advance
            //0x0010 	XPlaDevice 	Includes Device table (non-variable font) / VariationIndex table (variable font) for horizontal placement
            //0x0020 	YPlaDevice 	Includes Device table (non-variable font) / VariationIndex table (variable font) for vertical placement
            //0x0040 	XAdvDevice 	Includes Device table (non-variable font) / VariationIndex table (variable font) for horizontal advance
            //0x0080 	YAdvDevice 	Includes Device table (non-variable font) / VariationIndex table (variable font) for vertical advance
            //0xFF00 	Reserved 	For future use (set to zero)

            //check bits
            const int FMT_XPlacement = 1;
            const int FMT_YPlacement = 1 << 1;
            const int FMT_XAdvance = 1 << 2;
            const int FMT_YAdvance = 1 << 3;
            const int FMT_XPlaDevice = 1 << 4;
            const int FMT_YPlaDevice = 1 << 5;
            const int FMT_XAdvDevice = 1 << 6;
            const int FMT_YAdvDevice = 1 << 7;

            public static ValueRecord CreateFrom(BinaryReader reader, ushort valueFormat)
            {
                if (valueFormat == 0)
                    return null;//empty

                var v = new ValueRecord();
                v.ReadFrom(reader, valueFormat);
                return v;
            }

#if DEBUG
            public override string ToString()
            {
                StringBuilder stbuilder = new StringBuilder();
                bool appendComma = false;
                if (XPlacement != 0)
                {
                    stbuilder.Append("XPlacement=" + XPlacement);
                    appendComma = true;
                }



                if (YPlacement != 0)
                {
                    if (appendComma) { stbuilder.Append(','); }
                    stbuilder.Append(" YPlacement=" + YPlacement);
                    appendComma = true;
                }
                if (XAdvance != 0)
                {
                    if (appendComma) { stbuilder.Append(','); }
                    stbuilder.Append(" XAdvance=" + XAdvance);
                    appendComma = true;
                }
                if (YAdvance != 0)
                {
                    if (appendComma) { stbuilder.Append(','); }
                    stbuilder.Append(" YAdvance=" + YAdvance);
                    appendComma = true;
                }
                return stbuilder.ToString();
            }
#endif
        }


        /// <summary>
        /// To describe an anchor point
        /// </summary>
        class AnchorPoint
        {
            //Anchor Table

            //A GPOS table uses anchor points to position one glyph with respect to another.
            //Each glyph defines an anchor point, and the text-processing client attaches the glyphs by aligning their corresponding anchor points.

            //To describe an anchor point, an Anchor table can use one of three formats. 
            //The first format uses design units to specify a location for the anchor point.
            //The other two formats refine the location of the anchor point using contour points (Format 2) or Device tables (Format 3). 
            //In a variable font, the third format uses a VariationIndex table (a variant of a Device table) to 
            //reference variation data for adjustment of the anchor position for the current variation instance, as needed. 

            public ushort format;
            public short xcoord;
            public short ycoord;
            /// <summary>
            /// an index to a glyph contour point (AnchorPoint)
            /// </summary>
            public ushort refGlyphContourPoint;
            public ushort xdeviceTableOffset;
            public ushort ydeviceTableOffset;
            public static AnchorPoint CreateFrom(BinaryReader reader, long beginAt)
            {
                AnchorPoint anchorPoint = new AnchorPoint();
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);

                switch (anchorPoint.format = reader.ReadUInt16())
                {
                    default: throw new OpenFontNotSupportedException();
                    case 1:
                        {
                            // AnchorFormat1 table: Design units only
                            //AnchorFormat1 consists of a format identifier (AnchorFormat) and a pair of design unit coordinates (XCoordinate and YCoordinate)
                            //that specify the location of the anchor point. 
                            //This format has the benefits of small size and simplicity,
                            //but the anchor point cannot be hinted to adjust its position for different device resolutions.
                            //Value 	Type 	        Description
                            //uint16 	AnchorFormat 	Format identifier, = 1
                            //int16 	XCoordinate 	Horizontal value, in design units
                            //int16 	YCoordinate 	Vertical value, in design units
                            anchorPoint.xcoord = reader.ReadInt16();
                            anchorPoint.ycoord = reader.ReadInt16();
                        }
                        break;
                    case 2:
                        {
                            //Anchor Table: Format 2

                            //Like AnchorFormat1, AnchorFormat2 specifies a format identifier (AnchorFormat) and
                            //a pair of design unit coordinates for the anchor point (Xcoordinate and Ycoordinate).

                            //For fine-tuning the location of the anchor point,
                            //AnchorFormat2 also provides an index to a glyph contour point (AnchorPoint) 
                            //that is on the outline of a glyph (AnchorPoint).***
                            //Hinting can be used to move the AnchorPoint. In the rendered text,
                            //the AnchorPoint will provide the final positioning data for a given ppem size.

                            //Example 16 at the end of this chapter uses AnchorFormat2.


                            //AnchorFormat2 table: Design units plus contour point
                            //Value 	Type 	        Description
                            //uint16 	AnchorFormat 	Format identifier, = 2
                            //int16 	XCoordinate 	Horizontal value, in design units
                            //int16 	YCoordinate 	Vertical value, in design units
                            //uint16 	AnchorPoint 	Index to glyph contour point

                            anchorPoint.xcoord = reader.ReadInt16();
                            anchorPoint.ycoord = reader.ReadInt16();
                            anchorPoint.refGlyphContourPoint = reader.ReadUInt16();

                        }
                        break;
                    case 3:
                        {

                            //Anchor Table: Format 3

                            //Like AnchorFormat1, AnchorFormat3 specifies a format identifier (AnchorFormat) and 
                            //locates an anchor point (Xcoordinate and Ycoordinate).
                            //And, like AnchorFormat 2, it permits fine adjustments in variable fonts to the coordinate values. 
                            //However, AnchorFormat3 uses Device tables, rather than a contour point, for this adjustment.

                            //With a Device table, a client can adjust the position of the anchor point for any font size and device resolution.
                            //AnchorFormat3 can specify offsets to Device tables for the the X coordinate (XDeviceTable) 
                            //and the Y coordinate (YDeviceTable). 
                            //If only one coordinate requires adjustment, 
                            //the offset to the Device table may be set to NULL for the other coordinate.

                            //In variable fonts, AnchorFormat3 must be used to reference variation data to adjust anchor points for different variation instances,
                            //if needed.
                            //In this case, AnchorFormat3 specifies an offset to a VariationIndex table,
                            //which is a variant of the Device table used for variations.
                            //If no VariationIndex table is used for a particular anchor point X or Y coordinate, 
                            //then that value is used for all variation instances.
                            //While separate VariationIndex table references are required for each value that requires variation,
                            //two or more values that require the same variation-data values can have offsets that point to the same VariationIndex table, and two or more VariationIndex tables can reference the same variation data entries.

                            //Example 17 at the end of the chapter shows an AnchorFormat3 table.


                            //AnchorFormat3 table: Design units plus Device or VariationIndex tables
                            //Value 	Type 	        Description
                            //uint16 	AnchorFormat 	Format identifier, = 3
                            //int16 	XCoordinate 	Horizontal value, in design units
                            //int16 	YCoordinate 	Vertical value, in design units
                            //Offset16 	XDeviceTable 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for X coordinate, from beginning of Anchor table (may be NULL)
                            //Offset16 	YDeviceTable 	Offset to Device table (non-variable font) / VariationIndex table (variable font) for Y coordinate, from beginning of Anchor table (may be NULL)

                            anchorPoint.xcoord = reader.ReadInt16();
                            anchorPoint.ycoord = reader.ReadInt16();
                            anchorPoint.xdeviceTableOffset = reader.ReadUInt16();
                            anchorPoint.ydeviceTableOffset = reader.ReadUInt16();
                        }
                        break;
                }
                return anchorPoint;

            }
#if DEBUG
            public override string ToString()
            {
                switch (format)
                {
                    default: return "";
                    case 1:
                        return format + "(" + xcoord + "," + ycoord + ")";
                    case 2:
                        return format + "(" + xcoord + "," + ycoord + "), ref_point=" + refGlyphContourPoint;
                    case 3:
                        return format + "(" + xcoord + "," + ycoord + "), xy_device(" + xdeviceTableOffset + "," + ydeviceTableOffset + ")";
                }

            }
#endif
        }


        class MarkArrayTable
        {
            //Mark Array
            //The MarkArray table defines the class and the anchor point for a mark glyph. 
            //Three GPOS subtables-MarkToBase, MarkToLigature, and MarkToMark Attachment
            //use the MarkArray table to specify data for attaching marks.

            //The MarkArray table contains a count of the number of mark records (MarkCount) and an array of those records (MarkRecord).
            //Each mark record defines the class of the mark and an offset to the Anchor table that contains data for the mark.

            //A class value can be 0 (zero), but the MarkRecord must explicitly assign that class value (this differs from the ClassDef table, 
            //in which all glyphs not assigned class values automatically belong to Class 0).
            //The GPOS subtables that refer to MarkArray tables use the class assignments for indexing zero-based arrays that contain data for each mark class.

            // MarkArray table
            //-------------------
            //Value 	Type 	                Description
            //uint16 	MarkCount 	            Number of MarkRecords
            //struct 	MarkRecord[MarkCount] 	Array of MarkRecords in Coverage order
            //
            //MarkRecord
            //Value 	Type 	                Description
            //-------------------
            //uint16 	Class 	                Class defined for this mark
            //Offset16 	MarkAnchor 	            Offset to Anchor table-from beginning of MarkArray table
            internal MarkRecord[] _records;
            internal AnchorPoint[] _anchorPoints;
            public AnchorPoint GetAnchorPoint(int index)
            {
                return _anchorPoints[index];
            }
            public ushort GetMarkClass(int index)
            {
                return _records[index].markClass;
            }
            void ReadFrom(BinaryReader reader)
            {
                long markTableBeginAt = reader.BaseStream.Position;
                ushort markCount = reader.ReadUInt16();
                _records = new MarkRecord[markCount];
                for (int i = 0; i < markCount; ++i)
                {
                    //1 mark : 1 anchor
                    _records[i] = new MarkRecord(
                        reader.ReadUInt16(),//mark class
                        reader.ReadUInt16()); //offset to anchor table
                }
                //---------------------------
                //read anchor
                _anchorPoints = new AnchorPoint[markCount];
                for (int i = 0; i < markCount; ++i)
                {
                    MarkRecord markRec = _records[i];
                    //bug?
                    if (markRec.offset < 0)
                    {
                        //TODO: review here
                        //found err on Tahoma
                        continue;
                    }
                    //read table detail
                    _anchorPoints[i] = AnchorPoint.CreateFrom(reader, markTableBeginAt + markRec.offset);
                }

            }
#if DEBUG
            public int dbugGetAnchorCount()
            {
                return _anchorPoints.Length;
            }
#endif
            public static MarkArrayTable CreateFrom(BinaryReader reader, long beginAt)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //
                var markArrTable = new MarkArrayTable();
                markArrTable.ReadFrom(reader);
                return markArrTable;
            }
        }

        readonly struct MarkRecord
        {
            /// <summary>
            /// Class defined for this mark,. A mark class is identified by a specific integer, called a class value
            /// </summary>
            public readonly ushort markClass;
            /// <summary>
            /// Offset to Anchor table-from beginning of MarkArray table
            /// </summary>
            public readonly ushort offset;
            public MarkRecord(ushort markClass, ushort offset)
            {
                this.markClass = markClass;
                this.offset = offset;
            }
#if DEBUG
            public override string ToString()
            {
                return "class " + markClass + ",offset=" + offset;
            }
#endif
        }

        class Mark2ArrayTable
        {
            ///Mark2Array table
            //Value 	Type 	        Description
            //uint16 	Mark2Count 	    Number of Mark2 records
            //struct 	Mark2Record[Mark2Count] 	Array of Mark2 records-in Coverage order

            //Each Mark2Record contains an array of offsets to Anchor tables (Mark2Anchor).
            //The array of zero-based offsets, measured from the beginning of the Mark2Array table,
            //defines the entire set of Mark2 attachment points used to attach Mark1 glyphs to a specific Mark2 glyph.
            //The Anchor tables in the Mark2Anchor array are ordered by Mark1 class value.

            //A Mark2Record declares one Anchor table for each mark class (including Class 0)
            //identified in the MarkRecords of the MarkArray.
            //Each Anchor table specifies one Mark2 attachment point used to attach all
            //the Mark1 glyphs in a particular class to the Mark2 glyph.

            //Mark2Record
            //Value 	Type 	                    Description
            //Offset16 	Mark2Anchor[ClassCount] 	Array of offsets (one per class) to Anchor tables-from beginning of Mark2Array table-zero-based array

            public static Mark2ArrayTable CreateFrom(BinaryReader reader, long beginAt, ushort classCount)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //---
                ushort mark2Count = reader.ReadUInt16();
                ushort[] offsets = Utils.ReadUInt16Array(reader, mark2Count * classCount);
                //read mark2 anchors
                AnchorPoint[] anchors = new AnchorPoint[mark2Count * classCount];
                for (int i = 0; i < mark2Count * classCount; ++i)
                {
                    anchors[i] = AnchorPoint.CreateFrom(reader, beginAt + offsets[i]);
                }
                return new Mark2ArrayTable(classCount, anchors);
            }

            public AnchorPoint GetAnchorPoint(int index, int markClassId)
            {
                return _anchorPoints[index * _classCount + markClassId];
            }

            public Mark2ArrayTable(ushort classCount, AnchorPoint[] anchorPoints)
            {
                _classCount = classCount;
                _anchorPoints = anchorPoints;
            }

            internal readonly ushort _classCount;
            internal readonly AnchorPoint[] _anchorPoints;
        }

        class BaseArrayTable
        {
            //BaseArray table
            //Value 	Type 	                Description
            //uint16 	BaseCount 	            Number of BaseRecords
            //struct 	BaseRecord[BaseCount] 	Array of BaseRecords-in order of BaseCoverage Index

            //A BaseRecord declares one Anchor table for each mark class (including Class 0)
            //identified in the MarkRecords of the MarkArray.
            //Each Anchor table specifies one attachment point used to attach all the marks in a particular class to the base glyph.
            //A BaseRecord contains an array of offsets to Anchor tables (BaseAnchor).
            //The zero-based array of offsets defines the entire set of attachment points each base glyph uses to attach marks.
            //The offsets to Anchor tables are ordered by mark class.

            // Note: Anchor tables are not tagged with class value identifiers.
            //Instead, the index value of an Anchor table in the array defines the class value represented by the Anchor table.

            internal BaseRecord[] _records;

            public BaseRecord GetBaseRecords(int index)
            {
                return _records[index];
            }
            public static BaseArrayTable CreateFrom(BinaryReader reader, long beginAt, ushort classCount)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //---
                var baseArrTable = new BaseArrayTable();
                ushort baseCount = reader.ReadUInt16();
                baseArrTable._records = new BaseRecord[baseCount];
                // Read all baseAnchorOffsets in one go
                ushort[] baseAnchorOffsets = Utils.ReadUInt16Array(reader, classCount * baseCount);
                for (int i = 0; i < baseCount; ++i)
                {
                    AnchorPoint[] anchors = new AnchorPoint[classCount];
                    BaseRecord baseRec = new BaseRecord(anchors);

                    //each base has anchor point for mark glyph'class
                    for (int n = 0; n < classCount; ++n)
                    {
                        ushort offset = baseAnchorOffsets[i * classCount + n];
                        if (offset <= 0)
                        {
                            //TODO: review here 
                            //bug?
                            continue;
                        }
                        anchors[n] = AnchorPoint.CreateFrom(reader, beginAt + offset);
                    }

                    baseArrTable._records[i] = baseRec;
                }
                return baseArrTable;
            }

#if DEBUG
            public int dbugGetRecordCount()
            {
                return _records.Length;
            }
#endif
        }

        readonly struct BaseRecord
        {
            //BaseRecord
            //Value 	Type 	Description
            //Offset16 	BaseAnchor[ClassCount] 	Array of offsets (one per class) to 
            //Anchor tables-from beginning of BaseArray table-ordered by class-zero-based

            public readonly AnchorPoint[] anchors;

            public BaseRecord(AnchorPoint[] anchors)
            {
                this.anchors = anchors;
            }
#if DEBUG
            public override string ToString()
            {
                StringBuilder stbuilder = new StringBuilder();
                if (anchors != null)
                {
                    int i = 0;
                    foreach (AnchorPoint a in anchors)
                    {
                        if (i > 0)
                        {
                            stbuilder.Append(',');
                        }
                        if (a == null)
                        {
                            stbuilder.Append("null");
                        }
                        else
                        {
                            stbuilder.Append(a.ToString());
                        }
                    }
                }
                return stbuilder.ToString();
            }
#endif
        }


        // LigatureArray table
        //Value 	Type 	Description
        //USHORT 	LigatureCount 	Number of LigatureAttach table offsets
        //Offset 	LigatureAttach
        //[LigatureCount] 	Array of offsets to LigatureAttach tables-from beginning of LigatureArray table-ordered by LigatureCoverage Index

        //Each LigatureAttach table consists of an array (ComponentRecord) and count (ComponentCount) of the component glyphs in a ligature. The array stores the ComponentRecords in the same order as the components in the ligature. The order of the records also corresponds to the writing direction of the text. For text written left to right, the first component is on the left; for text written right to left, the first component is on the right.
        //-------------------------------
        //LigatureAttach table
        //Value 	Type 	                            Description
        //uint16 	ComponentCount 	                    Number of ComponentRecords in this ligature
        //struct 	ComponentRecord[ComponentCount] 	Array of Component records-ordered in writing direction
        //-------------------------------
        //A ComponentRecord, one for each component in the ligature, contains an array of offsets to the Anchor tables that define all the attachment points used to attach marks to the component (LigatureAnchor). For each mark class (including Class 0) identified in the MarkArray records, an Anchor table specifies the point used to attach all the marks in a particular class to the ligature base glyph, relative to the component.

        //In a ComponentRecord, the zero-based LigatureAnchor array lists offsets to Anchor tables by mark class. If a component does not define an attachment point for a particular class of marks, then the offset to the corresponding Anchor table will be NULL.

        //Example 8 at the end of this chapter shows a MarkLisPosFormat1 subtable used to attach mark accents to a ligature glyph in the Arabic script.
        //-------------------
        //ComponentRecord
        //Value 	Type 	Description
        //Offset16 	LigatureAnchor[ClassCount] 	Array of offsets (one per class) to Anchor tables-from beginning of LigatureAttach table-ordered by class-NULL if a component does not have an attachment for a class-zero-based array
        class LigatureArrayTable
        {
            LigatureAttachTable[] _ligatures;
            public void ReadFrom(BinaryReader reader, ushort classCount)
            {
                long startPos = reader.BaseStream.Position;
                ushort ligatureCount = reader.ReadUInt16();
                ushort[] offsets = Utils.ReadUInt16Array(reader, ligatureCount);

                _ligatures = new LigatureAttachTable[ligatureCount];

                for (int i = 0; i < ligatureCount; ++i)
                {
                    //each ligature table
                    reader.BaseStream.Seek(startPos + offsets[i], SeekOrigin.Begin);
                    _ligatures[i] = LigatureAttachTable.ReadFrom(reader, classCount);
                }
            }
            public LigatureAttachTable GetLigatureAttachTable(int index) => _ligatures[index];
        }
        class LigatureAttachTable
        {
            //LigatureAttach table
            //Value 	Type 	                            Description
            //uint16 	ComponentCount 	                    Number of ComponentRecords in this ligature
            //struct 	ComponentRecord[ComponentCount] 	Array of Component records-ordered in writing direction
            //-------------------------------
            ComponentRecord[] _records;
            public static LigatureAttachTable ReadFrom(BinaryReader reader, ushort classCount)
            {
                LigatureAttachTable table = new LigatureAttachTable();
                ushort componentCount = reader.ReadUInt16();
                ComponentRecord[] componentRecs = new ComponentRecord[componentCount];
                table._records = componentRecs;
                for (int i = 0; i < componentCount; ++i)
                {
                    componentRecs[i] = new ComponentRecord(
                        Utils.ReadUInt16Array(reader, classCount));
                }
                return table;
            }
            public ComponentRecord GetComponentRecord(int index) => _records[index];
        }
        readonly struct ComponentRecord
        {
            //ComponentRecord
            //Value         Type                          Description
            //Offset16      LigatureAnchor[ClassCount]    Array of offsets(one per class) to Anchor tables-from beginning of LigatureAttach table-ordered by class-NULL if a component does not have an attachment for a class-zero-based array

            public readonly ushort[] offsets;
            public ComponentRecord(ushort[] offsets)
            {
                this.offsets = offsets;
            }

        }

        //------ 
        readonly struct PosLookupRecord
        {


            //PosLookupRecord
            //Value 	Type 	Description
            //USHORT 	SequenceIndex 	Index to input glyph sequence-first glyph = 0
            //USHORT 	LookupListIndex 	Lookup to apply to that position-zero-based

            public readonly ushort seqIndex;
            public readonly ushort lookupListIndex;
            public PosLookupRecord(ushort seqIndex, ushort lookupListIndex)
            {
                this.seqIndex = seqIndex;
                this.lookupListIndex = lookupListIndex;
            }
            public static PosLookupRecord CreateFrom(BinaryReader reader)
            {
                return new PosLookupRecord(reader.ReadUInt16(), reader.ReadUInt16());
            }
        }


        class PosRuleSetTable
        {

            //PosRuleSet table: All contexts beginning with the same glyph
            // Value 	Type 	        Description
            //uint16 	PosRuleCount 	Number of PosRule tables
            //Offset16 	PosRule[PosRuleCount] 	Array of offsets to PosRule tables-from beginning of PosRuleSet-ordered by preference
            //
            //A PosRule table consists of a count of the glyphs to be matched in the input context sequence (GlyphCount), 
            //including the first glyph in the sequence, and an array of glyph indices that describe the context (Input). 
            //The Coverage table specifies the index of the first glyph in the context, and the Input array begins with the second glyph in the context sequence. As a result, the first index position in the array is specified with the number one (1), not zero (0). The Input array lists the indices in the order the corresponding glyphs appear in the text. For text written from right to left, the right-most glyph will be first; conversely, for text written from left to right, the left-most glyph will be first.

            //A PosRule table also contains a count of the positioning operations to be performed on the input glyph sequence (PosCount) and an array of PosLookupRecords (PosLookupRecord). Each record specifies a position in the input glyph sequence and a LookupList index to the positioning lookup to be applied there. The array should list records in design order, or the order the lookups should be applied to the entire glyph sequence.

            //Example 10 at the end of this chapter demonstrates glyph kerning in context with a ContextPosFormat1 subtable.

            PosRuleTable[] _posRuleTables;
            void ReadFrom(BinaryReader reader)
            {
                long tableStartAt = reader.BaseStream.Position;
                ushort posRuleCount = reader.ReadUInt16();
                ushort[] posRuleTableOffsets = Utils.ReadUInt16Array(reader, posRuleCount);
                int j = posRuleTableOffsets.Length;
                _posRuleTables = new PosRuleTable[posRuleCount];
                for (int i = 0; i < j; ++i)
                {
                    //move to and read
                    reader.BaseStream.Seek(tableStartAt + posRuleTableOffsets[i], SeekOrigin.Begin);
                    var posRuleTable = new PosRuleTable();
                    posRuleTable.ReadFrom(reader);
                    _posRuleTables[i] = posRuleTable;

                }
            }

            public static PosRuleSetTable CreateFrom(BinaryReader reader, long beginAt)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //------------
                var posRuleSetTable = new PosRuleSetTable();
                posRuleSetTable.ReadFrom(reader);
                return posRuleSetTable;
            }
        }
        class PosRuleTable
        {

            //PosRule subtable
            //Value 	Type 	    Description
            //uint16 	GlyphCount 	Number of glyphs in the Input glyph sequence
            //uint16 	PosCount 	Number of PosLookupRecords
            //uint16 	Input[GlyphCount - 1]  Array of input GlyphIDs-starting with the second glyph***
            //struct 	PosLookupRecord[PosCount] 	Array of positioning lookups-in design order
            PosLookupRecord[] _posLookupRecords;
            ushort[] _inputGlyphIds;
            public void ReadFrom(BinaryReader reader)
            {
                ushort glyphCount = reader.ReadUInt16();
                ushort posCount = reader.ReadUInt16();
                _inputGlyphIds = Utils.ReadUInt16Array(reader, glyphCount - 1);
                _posLookupRecords = CreateMultiplePosLookupRecords(reader, posCount);
            }
        }


        class PosClassSetTable
        {
            //PosClassSet table: All contexts beginning with the same class
            //Value 	Type 	                        Description
            //----------------------
            //uint16 	PosClassRuleCnt 	            Number of PosClassRule tables
            //Offset16 	PosClassRule[PosClassRuleCnt] 	Array of offsets to PosClassRule tables-from beginning of PosClassSet-ordered by preference
            //----------------------
            //
            //For each context, a PosClassRule table contains a count of the glyph classes in a given context (GlyphCount), 
            //including the first class in the context sequence. 
            //A class array lists the classes, beginning with the second class, 
            //that follow the first class in the context. 
            //The first class listed indicates the second position in the context sequence.

            //Note: Text order depends on the writing direction of the text. 
            //For text written from right to left, the right-most glyph will be first. 
            //Conversely, for text written from left to right, the left-most glyph will be first.

            //The values specified in the Class array are those defined in the ClassDef table.
            //For example, consider a context consisting of the sequence: Class 2, Class 7, Class 5, Class 0.
            //The Class array will read: Class[0] = 7, Class[1] = 5, and Class[2] = 0. 
            //The first class in the sequence, Class 2, is defined by the index into the PosClassSet array of offsets.
            //The total number and sequence of glyph classes listed in the Class array must match the total number and sequence of glyph classes contained in the input context.

            //A PosClassRule also contains a count of the positioning operations to be performed on the context (PosCount) and 
            //an array of PosLookupRecords (PosLookupRecord) that supply the positioning data. 
            //For each position in the context that requires a positioning operation,
            //a PosLookupRecord specifies a LookupList index and a position in the input glyph class sequence where the lookup is applied.
            //The PosLookupRecord array lists PosLookupRecords in design order, or the order in which lookups are applied to the entire glyph sequence.

            //Example 11 at the end of this chapter demonstrates a ContextPosFormat2 subtable that uses glyph classes to modify accent positions in glyph strings.
            //----------------------
            //PosClassRule table: One class context definition
            //----------------------
            //Value 	Type 	    Description
            //uint16 	GlyphCount 	Number of glyphs to be matched
            //uint16 	PosCount 	Number of PosLookupRecords
            //uint16 	Class[GlyphCount - 1] 	Array of classes-beginning with the second class-to be matched to the input glyph sequence
            //struct 	PosLookupRecord[PosCount] 	Array of positioning lookups-in design order
            //----------------------

            public PosClassRule[] PosClassRules;

            void ReadFrom(BinaryReader reader)
            {
                long tableStartAt = reader.BaseStream.Position;
                //
                ushort posClassRuleCnt = reader.ReadUInt16();
                ushort[] posClassRuleOffsets = Utils.ReadUInt16Array(reader, posClassRuleCnt);
                PosClassRules = new PosClassRule[posClassRuleCnt];
                for (int i = 0; i < posClassRuleOffsets.Length; ++i)
                {
                    //move to and read                     
                    PosClassRules[i] = PosClassRule.CreateFrom(reader, tableStartAt + posClassRuleOffsets[i]);
                }
            }

            public static PosClassSetTable CreateFrom(BinaryReader reader, long beginAt)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //--------
                var posClassSetTable = new PosClassSetTable();
                posClassSetTable.ReadFrom(reader);
                return posClassSetTable;
            }
        }

        class PosClassRule
        {
            public PosLookupRecord[] PosLookupRecords;
            public ushort[] InputGlyphIds;

            public static PosClassRule CreateFrom(BinaryReader reader, long beginAt)
            {
                //--------
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //--------
                PosClassRule posClassRule = new PosClassRule();
                ushort glyphCount = reader.ReadUInt16();
                ushort posCount = reader.ReadUInt16();
                if (glyphCount > 1)
                {
                    posClassRule.InputGlyphIds = Utils.ReadUInt16Array(reader, glyphCount - 1);
                }

                posClassRule.PosLookupRecords = CreateMultiplePosLookupRecords(reader, posCount);
                return posClassRule;
            }
        }
    }
}
