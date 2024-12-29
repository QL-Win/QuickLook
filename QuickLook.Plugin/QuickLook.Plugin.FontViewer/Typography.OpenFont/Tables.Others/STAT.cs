//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/stat

    //The style attributes table describes design attributes that
    //distinguish font-style variants within a font family. 
    //It also provides associations between those attributes and 
    //name elements that may be used to present font options within application user interfaces.

    //**A style attributes table is required in all variable fonts.

    //The style attributes table is also recommended for all new, non-variable fonts,    //
    //especially if fonts have style attributes in axes other than weight, width, or slope.


    /// <summary>
    /// STAT — Style Attributes Table
    /// </summary>
    class STAT : TableEntry
    {

        public const string _N = "STAT";
        public override string Name => _N;
        //
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //Style Attributes Header 
            //The style attributes table, version 1.2, is organized as follows: 
            //Style attributes header:
            //Type      Name                Description
            //uint16    majorVersion        Major version number of the style attributes table — set to 1.
            //uint16    minorVersion        Minor version number of the style attributes table — set to 2.
            //uint16    designAxisSize      The size in bytes of each axis record.
            //uint16    designAxisCount     The number of design axis records.
            //                              In a font with an 'fvar' table, this value must be greater than or equal to the axisCount value in the 'fvar' table.
            //                              In all fonts, must be greater than zero if axisValueCount is greater than zero.
            //Offset32  designAxesOffset    Offset in bytes from the beginning of the STAT table to the start of the design axes array.
            //                              If designAxisCount is zero, set to zero; 
            //                              if designAxisCount is greater than zero, must be greater than zero.
            //uint16    axisValueCount      The number of axis value tables.
            //Offset32  offsetToAxisValueOffsets    Offset in bytes from the beginning of the STAT table to the start of the design axes value offsets array. 
            //                                      If axisValueCount is zero, set to zero; 
            //                                      if axisValueCount is greater than zero, must be greater than zero.
            //uint16    elidedFallbackNameID    Name ID used as fallback when projection of names into a particular font model produces a subfamily name containing only elidable elements.

            long beginPos = reader.BaseStream.Position;
            //
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort designAxisSize = reader.ReadUInt16();
            ushort designAxisCount = reader.ReadUInt16();
            //
            uint designAxesOffset = reader.ReadUInt32();
            ushort axisValueCount = reader.ReadUInt16();
            uint offsetToAxisValueOffsets = reader.ReadUInt32();
            //
            ushort elidedFallbackNameID = (minorVersion != 0) ? reader.ReadUInt16() : (ushort)0;
            //(elidedFallbackNameIDk, In version 1.0 of the style attributes table, the elidedFallbackNameId field was not included. Use of version 1.0 is deprecated)



            //The header is followed by the design axes and axis value offsets arrays, the location of which are provided by offset fields.
            //Type          Name                                Description
            //AxisRecord    designAxes[designAxisCount]         The design-axes array.
            //Offset16      axisValueOffsets[axisValueCount]    Array of offsets to axis value tables,
            //                                                  in bytes from the start of the axis value offsets array.

            //The designAxisSize field indicates the size of each axis record. 
            //Future minor-version updates of the STAT table may define compatible extensions 
            //to the axis record format with additional fields.
            //**Implementations must use the designAxisSize designAxisSize field to determine the start of each record.**


            //Axis Records
            //The axis record provides information about a single design axis.
            //AxisRecord:
            //Type      Name            Description
            //Tag       axisTag         A tag identifying the axis of design variation.
            //uint16    axisNameID      The name ID for entries in the 'name' table that provide a display string for this axis.
            //uint16    axisOrdering    A value that applications can use to determine primary sorting of face names, or for ordering of descriptors when composing family or face names.

            AxisRecord[] axisRecords = new AxisRecord[designAxisCount];
            for (int i = 0; i < designAxisCount; ++i)
            {
                var axisRecord = new AxisRecord();
                axisRecords[i] = axisRecord;
                axisRecord.axisTagName = Utils.TagToString(reader.ReadUInt32()); //4
                axisRecord.axisNameId = reader.ReadUInt16(); //2
                axisRecord.axisOrdering = reader.ReadUInt16(); //2


                //***
                if (designAxisSize > 8)
                {
                    //**Implementations must use the designAxisSize designAxisSize field to determine the start of each record.**
                    //Future minor-version updates of the STAT table may define compatible extensions 
                    //to the axis record format with additional fields.


                    // so skip more ...
                    //
                    //at least there are 8 bytes 
                    reader.BaseStream.Position += (designAxisSize - 8);
                }
            }


            long axisValueOffsets_beginPos = reader.BaseStream.Position = beginPos + offsetToAxisValueOffsets;
            ushort[] axisValueOffsets = Utils.ReadUInt16Array(reader, axisValueCount); // Array of offsets to axis value tables,in bytes from the start of the axis value offsets array.


            //move to axis value record

            AxisValueTableBase[] axisValueTables = new AxisValueTableBase[axisValueCount];
            for (int i = 0; i < axisValueCount; ++i)
            {

                //Axis Value Tables
                //Axis value tables provide details regarding a specific style - attribute value on some specific axis of design variation, 
                //or a combination of design-variation axis values, and the relationship of those values to name elements. 
                //This information can be useful for presenting fonts in application user interfaces.

                //           
                //read each axis table
                ushort offset = axisValueOffsets[i];
                reader.BaseStream.Position = axisValueOffsets_beginPos + offset;

                ushort format = reader.ReadUInt16();//common field of all axis value table
                AxisValueTableBase axisValueTbl = null;
                switch (format)
                {
                    default: throw new OpenFontNotSupportedException();
                    case 1: axisValueTbl = new AxisValueTableFmt1(); break;
                    case 2: axisValueTbl = new AxisValueTableFmt2(); break;
                    case 3: axisValueTbl = new AxisValueTableFmt3(); break;
                    case 4: axisValueTbl = new AxisValueTableFmt4(); break;
                }
                axisValueTbl.ReadContent(reader);
                axisValueTables[i] = axisValueTbl;
            }


            //Each AxisValue record must have a different axisIndex value.
            //The records can be in any order. 

            //Flags
            //The following axis value table flags are defined:
            //Mask    Name                            Description
            //0x0001  OLDER_SIBLING_FONT_ATTRIBUTE    If set, this axis value table provides axis value information that is applicable to other fonts within the same font family.This is used if the other fonts were released earlier and did not include information about values for some axis. If newer versions of the other fonts include the information themselves and are present, then this record is ignored.
            //0x0002  ELIDABLE_AXIS_VALUE_NAME        If set, it indicates that the axis value represents the “normal” value for the axis and may be omitted when composing name strings.
            //0xFFFC  Reserved                        Reserved for future use — set to zero.


            //When the OlderSiblingFontAttribute flag is used, implementations may use the information provided to determine behaviour associated with a different font in the same family.
            //If a previously - released family is extended with fonts for style variations from a new axis of design variation, 
            //then all of them should include a OlderSiblingFontAttribute table for the “normal” value of earlier fonts.

            //The values in the different fonts should match; if they do not, application behavior may be unpredictable.

            // Note: When the OlderSiblingFontAttribute flag is set, that axis value table is intended to provide default information about other fonts in the same family,
            //but not about the font in which that axis value table is contained.
            //The font should contain different axis value tables that do not use this flag to make declarations about itself.

            //The ElidableAxisValueName flag can be used to designate a “normal” value for an axis that should not normally appear in a face name.
            //For example, the designer may prefer that face names not include “Normal” width or “Regular” weight.
            //If this flag is set, applications are permitted to omit these descriptors from face names, though they may also include them in certain scenarios.

            //Note: Fonts should provide axis value tables for “normal” axis values even if they should not normally be reflected in face names.

            //Note: If a font or a variable-font instance is selected for which all axis values have the ElidableAxisValueName flag set, 
            //then applications may keep the name for the weight axis, if present, to use as a constructed subfamily name, with names for all other axis values omitted.

            //When the OlderSiblingFontAttribute flag is set, this will typically be providing information regarding the “normal” value on some newly-introduced axis.
            //In this case, the ElidableAxisValueName flag may also be set, as desired.When applied to the earlier fonts,
            //those likely would not have included any descriptors for the new axis, and so the effects of the ElidableAxisValueName flag are implicitly assumed.

            //If multiple axis value tables have the same axis index, then one of the following should be true:

            //    The font is a variable font, and the axis is defined in the font variations table as a variation.
            //    The OlderSiblingFontAttribute flag is set in one of the records.

            //Two different fonts within a family may share certain style attributes in common.
            //For example, Bold Condensed and Bold Semi Condensed fonts both have the same weight attribute, Bold.
            //Axis value tables for particular values should be implemented consistently across a family.
            //If they are not consistent, applications may exhibit unpredictable behaviors.
        }


        public class AxisRecord
        {
            public string axisTagName;
            public ushort axisNameId;
            public ushort axisOrdering;
#if DEBUG
            public override string ToString()
            {
                return axisTagName;
            }
#endif
        }

        public abstract class AxisValueTableBase
        {
            public abstract int Format { get; }


            /// <summary>
            /// assume we have read format
            /// </summary>
            /// <param name="reader"></param>
            public abstract void ReadContent(BinaryReader reader);
        }



        public class AxisValueTableFmt1 : AxisValueTableBase
        {
            public override int Format => 1;
            //Axis value table, format 1 
            //Axis value table format 1 has the following structure.

            //AxisValueFormat1:
            //Type      Name            Description
            //uint16    format          Format identifier — set to 1.
            //uint16    axisIndex       Zero - base index into the axis record array identifying the axis of design variation to which the axis value record applies.
            //                          Must be less than designAxisCount.
            //uint16    flags           Flags — see below for details.
            //uint16    valueNameID     The name ID for entries in the 'name' table that provide a display string for this attribute value.
            //Fixed     value           A numeric value for this attribute value. 

            //A format 1 table is used simply to associate a specific axis value with a name. 

            public ushort axisIndex;
            public ushort flags;
            public ushort valueNameId;
            public float value;
            public override void ReadContent(BinaryReader reader)
            {
                //at here, assume we have read format, 
                //Fixed =>	32-bit signed fixed-point number (16.16) 
                axisIndex = reader.ReadUInt16();
                flags = reader.ReadUInt16();
                valueNameId = reader.ReadUInt16();
                value = reader.ReadFixed();
            }
        }
        public class AxisValueTableFmt2 : AxisValueTableBase
        {
            public override int Format => 2;
            //Axis value table, format 2 
            //Axis value table format 2 has the following structure.

            //AxisValueFormat2
            //Type      Name            Description
            //uint16    format          Format identifier — set to 2.
            //uint16    axisIndex       Zero - base index into the axis record array identifying the axis of design variation to which the axis value record applies.
            //                          Must be less than designAxisCount.
            //uint16    flags           Flags — see below for details.
            //uint16    valueNameID     The name ID for entries in the 'name' table that provide a display string for this attribute value.
            //Fixed     nominalValue    A nominal numeric value for this attribute value.
            //Fixed     rangeMinValue   The minimum value for a range associated with the specified name ID.
            //Fixed     rangeMaxValue   The maximum value for a range associated with the specified name ID.

            //A format 2 table can be used if a given name is associated with a particular axis value, but is also associated with a range of values.For example,
            //in a family that supports optical size variations, “Subhead” may be used in relation to a range of sizes.
            //The rangeMinValue and rangeMaxValue fields are used to define that range.
            //In a variable font, a named instance has specific coordinates for each axis. 

            //The nominalValue field allows some specific, nominal value to be associated with a name,
            //to align with the named instances defined in the font variations table,
            //while the rangeMinValue and rangeMaxValue fields allow the same name 
            //also to be associated with a range of axis values.

            //Some design axes may be open ended, having an effective minimum value of negative infinity, 
            //or an effective maximum value of positive infinity.
            //To represent an effective minimum of negative infinity, set rangeMinValue to 0x80000000.
            //To represent an effective maximum of positive infinity, set rangeMaxValue to 0x7FFFFFFF.

            //Two format 2 tables for a given axis should not have ranges with overlap greater than zero.
            //If a font has two format 2 tables for a given axis, 
            //T1 and T2, with overlapping ranges, the following rules will apply:


            //If the range of T1 overlaps the higher end of the range of T2 with a greater max value than T2(T1.rangeMaxValue > T2.rangeMaxValue and T1.rangeMinValue <= T2.rangeMaxValue),
            //then T1 is used for all values within its range, including the portion that overlaps the range of T2.

            //If the range of T2 is contained entirely within the range of T1(T2.rangeMinValue >= T1.rangeMinValue and T2.rangeMaxValue <= T1.rangeMaValue), then T2 is ignored.

            //In the case of two tables with identical ranges for the same axis, it will be up to the implementation which is used and which is ignored.

            public ushort axisIndex;
            public ushort flags;
            public ushort valueNameId;
            public float nominalValue;
            public float rangeMinValue;
            public float rangeMaxValue;
            public override void ReadContent(BinaryReader reader)
            {
                axisIndex = reader.ReadUInt16();
                flags = reader.ReadUInt16();
                valueNameId = reader.ReadUInt16();
                nominalValue = reader.ReadFixed();
                rangeMinValue = reader.ReadFixed();
                rangeMaxValue = reader.ReadFixed();
            }
        }
        public class AxisValueTableFmt3 : AxisValueTableBase
        {
            public override int Format => 3;
            //
            //Axis value table, format 3
            //Axis value table format 3 has the following structure:
            //AxisValueFormat3:
            //Type      Name            Description
            //uint16    format          Format identifier — set to 3.
            //uint16    axisIndex       Zero-base index into the axis record array identifying the axis of design variation to which the axis value record applies.
            //                          Must be less than designAxisCount.
            //uint16    flags           Flags — see below for details.
            //uint16    valueNameID     The name ID for entries in the 'name' table that provide a display string for this attribute value.
            //Fixed     value           A numeric value for this attribute value.
            //Fixed     linkedValue     The numeric value for a style-linked mapping from this value.


            //A format 3 table can be used to indicate another value on the same axis that is to be treated as a style - linked counterpart to the current value.
            //This is primarily intended for “bold” style linking on a weight axis.
            //These mappings may be used in applications to determine which style within a family should be selected when a user selects a “bold” formatting option.
            //A mapping is defined from a “non - bold” value to its “bold” counterpart.
            //It is not necessary to provide a “bold” mapping for every weight value;
            //mappings should be provided for lighter weights, 
            //but heavier weights(typically, semibold or above) would already be considered “bold” and would not require a “bold” mapping.

            //Note: Applications are not required to use these style - linked mappings when implementing text formatting user interfaces.
            //This data can be provided in a font for the benefit of applications that choose to do so.
            //If a given application does not apply such style mappings for the given axis, then the linkedValue field is ignored.

            public ushort axisIndex;
            public ushort flags;
            public ushort valueNameId;
            public float value;
            public float linkedValue;

            public override void ReadContent(BinaryReader reader)
            {
                axisIndex = reader.ReadUInt16();
                flags = reader.ReadUInt16();
                valueNameId = reader.ReadUInt16();
                value = reader.ReadFixed();
                linkedValue = reader.ReadFixed();
            }
        }


        public class AxisValueTableFmt4 : AxisValueTableBase
        {
            public override int Format => 4;
            //Axis value table, format 4
            //Axis value table format 4 has the following structure:

            //AxisValueFormat4:
            //Type      Name            Description
            //uint16    format          Format identifier — set to 4.
            //uint16    axisCount       The total number of axes contributing to this axis-values combination.
            //uint16    flags           Flags — see below for details.
            //uint16    valueNameID     The name ID for entries in the 'name' table that provide a display string for this combination of axis values.
            //AxisValue axisValues[axisCount]   Array of AxisValue records that provide the combination of axis values, one for each contributing axis.


            public AxisValueRecord[] _axisValueRecords;
            public ushort flags;
            public ushort valueNameId;
            public override void ReadContent(BinaryReader reader)
            {
                ushort axisCount = reader.ReadUInt16();
                flags = reader.ReadUInt16();
                valueNameId = reader.ReadUInt16();
                _axisValueRecords = new AxisValueRecord[axisCount];
                for (int i = 0; i < axisCount; ++i)
                {
                    _axisValueRecords[i] = new AxisValueRecord(
                        reader.ReadUInt16(),
                        reader.ReadFixed());
                }
            }
        }

        public readonly struct AxisValueRecord
        {
            //The axisValues array uses AxisValue records, which have the following format.
            //AxisValue record:
            //Type    Name          Description
            //uint16  axisIndex     Zero - base index into the axis record array identifying the axis to which this value applies.Must be less than designAxisCount.
            //Fixed   value         A numeric value for this attribute value.
            public readonly ushort axisIndex;
            public readonly float value;
            public AxisValueRecord(ushort axisIndex, float value)
            {
                this.axisIndex = axisIndex;
                this.value = value;
            }
        }

    }


}