//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats

    //Item variation stores are used for most variation data other than that used for TrueType glyph outlines, 
    //including the variation data in MVAR, HVAR, VVAR, BASE and GDEF tables.

    //Note: For CFF2 glyph outlines, delta values are interleaved directly within the glyph outline description in the CFF2 table.
    //The sets of regions which are associated with the delta sets are defined in an item variation store,
    //contained as a subtable within the CFF2 table.
    //See the CFF2 chapter for additional details.


    //...
    //The item variation store includes a variation region list and an array of item variation data subtables

    class ItemVariationStoreTable
    {

        public VariationRegion[] variationRegions;
        public void ReadContentFrom(BinaryReader reader)
        {


            //VariationRegionList:
            //Type      Name            Description
            //uint16    axisCount       The number of variation axes for this font.This must be the same number as axisCount in the 'fvar' table.
            //uint16    regionCount     The number of variation region tables in the variation region list.
            //VariationRegion  variationRegions[regionCount] Array of variation regions.

            //The regions can be in any order.
            //The regions are defined using an array of RegionAxisCoordinates records, one for each axis defined in the 'fvar' table:

            ushort axisCount = reader.ReadUInt16();
            ushort regionCount = reader.ReadUInt16();
            variationRegions = new VariationRegion[regionCount];
            for (int i = 0; i < regionCount; ++i)
            {
                var variationRegion = new VariationRegion();
                variationRegion.ReadContent(reader, axisCount);
                variationRegions[i] = variationRegion;
            }
        }
    }
    class VariationRegion
    {
        //VariationRegion record:
        //Type                    Name                   Description
        //RegionAxisCoordinates   regionAxes[axisCount]   Array of region axis coordinates records, in the order of axes given in the 'fvar' table.
        //Each RegionAxisCoordinates record provides coordinate values for a region along a single axis:

        public RegionAxisCoordinate[] regionAxes;
        public void ReadContent(BinaryReader reader, int axisCount)
        {
            regionAxes = new RegionAxisCoordinate[axisCount];
            for (int i = 0; i < axisCount; ++i)
            {   
                regionAxes[i] = new RegionAxisCoordinate(
                    reader.ReadF2Dot14(), //start
                    reader.ReadF2Dot14(), //peak
                    reader.ReadF2Dot14() //end
                    );
            }
        }
    }
    readonly struct RegionAxisCoordinate
    {
        //RegionAxisCoordinates record:
        //Type        Name          Description
        //F2DOT14     startCoord    The region start coordinate value for the current axis.
        //F2DOT14     peakCoord     The region peak coordinate value for the current axis.
        //F2DOT14     endCoord      The region end coordinate value for the current axis.
        public readonly float startCoord;
        public readonly float peakCoord;
        public readonly float endCoord;

        public RegionAxisCoordinate(float startCoord, float peakCoord, float endCoord)
        {
            this.startCoord = startCoord;
            this.peakCoord = peakCoord;
            this.endCoord = endCoord;


            //The three values must all be within the range - 1.0 to + 1.0.
            //startCoord must be less than or equal to peakCoord, 
            //and peakCoord must be less than or equal to endCoord.
            //The three values must be either all non-positive or all non-negative with one possible exception: 
            //if peakCoord is zero, then startCoord can be negative or 0 while endCoord can be positive or zero.

            //...
            //Note: The following guidelines are used for setting the three values in different scenarios:

            //In the case of a non-intermediate region for which the given axis should factor into the scalar calculation for the region, 
            //either startCoord and peakCoord are set to a negative value(typically, -1.0) 
            //and endCoord is set to zero, or startCoord is set to zero and peakCoord and endCoord are set to a positive value(typically + 1.0).

            //In the case of an intermediate region for which the given axis should factor into the scalar calculation for the region,
            //startCoord, peakCoord and endCoord are all set to non - positive values or are all set to non - negative values.

            //If the given axis should not factor into the scalar calculation for a region,
            //then this is achieved by setting peakCoord to zero.
            //In this case, startCoord can be any non - positive value, and endCoord can be any non - negative value.
            //It is recommended either that all three be set to zero, or that startCoord be set to - 1.0 and endCoord be set to + 1.0.


        }
#if DEBUG
        public override string ToString()
        {
            return "start:" + startCoord + ",peak:" + peakCoord + ",end:" + endCoord;
        }
#endif
    }

}