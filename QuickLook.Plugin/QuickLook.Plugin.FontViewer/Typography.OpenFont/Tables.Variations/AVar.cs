//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/avar

    /// <summary>
    /// avar — Axis Variations Table
    /// </summary>
    class AVar : TableEntry
    {
        public const string _N = "avar";
        public override string Name => _N;

        //The axis variations table('avar') is an optional table 
        //used in variable fonts that use OpenType Font Variations mechanisms. 
        //It can be used to modify aspects of how a design varies for different instances along a particular design-variation axis.
        //Specifically, it allows modification of the coordinate normalization that is used when processing variation data for a particular variation instance.

        //... 

        //The 'avar' table must be used in combination with a font variations('fvar') table and 
        //other required or optional tables used in variable fonts.  

        SegmentMapRecord[] _axisSegmentMaps;
        protected override void ReadContentFrom(BinaryReader reader)
        {

            //The 'avar' table is comprised of a small header plus segment maps for each axis.

            //Axis variation table:
            //Type      Name            Description
            //uint16    majorVersion    Major version number of the axis variations table — set to 1.
            //uint16    minorVersion    Minor version number of the axis variations table — set to 0.
            //uint16    <reserved>      Permanently reserved; set to zero.
            //uint16    axisCount       The number of variation axes for this font.
            //                          This must be the same number as axisCount in the 'fvar' table.
            //SegmentMaps  axisSegmentMaps[axisCount]  The segment maps array—one segment map for each axis,
            //                                          in the order of axes specified in the 'fvar' table.
            //--------------

            //There must be one segment map for each axis defined in the 'fvar' table,
            //and the segment maps for the different axes must be given in the order of axes specified in the 'fvar' table.
            //The segment map for each axis is comprised of a list of axis - value mapping records. 

            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort reserved = reader.ReadUInt16();
            ushort axisCount = reader.ReadUInt16();

            //Each axis value map record provides a single axis-value mapping correspondence.
            _axisSegmentMaps = new SegmentMapRecord[axisCount];
            for (int i = 0; i < axisCount; ++i)
            {
                SegmentMapRecord segmentMap = new SegmentMapRecord();
                segmentMap.ReadContent(reader);
                _axisSegmentMaps[i] = segmentMap;
            }



        }
        public class SegmentMapRecord
        {
            //SegmentMaps record:
            //Type          Name                                Description
            //uint16        positionMapCount                    The number of correspondence pairs for this axis.
            //AxisValueMap  axisValueMaps[positionMapCount]     The array of axis value map records for this axis.
            public AxisValueMap[] axisValueMaps;
            public void ReadContent(BinaryReader reader)
            {
                ushort positionMapCount = reader.ReadUInt16();
                axisValueMaps = new AxisValueMap[positionMapCount];
                for (int i = 0; i < positionMapCount; ++i)
                {
                    axisValueMaps[i] = new AxisValueMap(
                        reader.ReadF2Dot14(),
                        reader.ReadF2Dot14()
                        );
                }
            }
        }
        public readonly struct AxisValueMap
        {
            //AxisValueMap record:
            //Type        Name            Description
            //F2DOT14     fromCoordinate  A normalized coordinate value obtained using default normalization.
            //F2DOT14     toCoordinate    The modified, normalized coordinate value.


            //Axis value maps can be provided for any axis,
            //but are required only if the normalization mapping for an axis is being modified.
            //If the segment map for a given axis has any value maps, 
            //then it must include at least three value maps: -1 to - 1, 0 to 0, and 1 to 1.
            //These value mappings are essential to the design of the variation mechanisms and
            //are required even if no additional maps are specified for a given axis.
            //If any of these is missing, then no modification to axis coordinate values will be made for that axis.


            //All of the axis value map records for a given axis must have different fromCoordinate values,
            //and axis value map records must be arranged in increasing order of the fromCoordinate value.
            //If the fromCoordinate value of a record is less than or equal to the fromCoordinate value of a previous record in the array, 
            //then the given record may be ignored.

            //Also, for any given record except the first, 
            //the toCoordinate value must be greater than or equal to the toCoordinate value of the preceding record.
            //This requirement ensures that there are no retrograde behaviors as the user-scale value range is traversed.
            //If a toCoordinate value of a record is less than that of the previous record, then the given record may be ignored.

            public readonly float fromCoordinate;
            public readonly float toCoordinate;
            public AxisValueMap(float fromCoordinate, float toCoordinate)
            {
                this.fromCoordinate = fromCoordinate;
                this.toCoordinate = toCoordinate;
            }
#if DEBUG
            public override string ToString()
            {
                return "from:" + fromCoordinate + " to:" + toCoordinate;
            }
#endif
        }
    }
}
