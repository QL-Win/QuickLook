//Apache2, 2016-present, WinterDev 

using System.IO;

namespace Typography.OpenFont.Tables
{

    class HorizontalDeviceMetrics : TableEntry
    {
        public const string _N = "hdmx";
        public override string Name => _N;
        //
        //https://www.microsoft.com/typography/otspec/hdmx.htm
        //The hdmx table relates to OpenType™ fonts with TrueType outlines. 
        //The Horizontal Device Metrics table stores integer advance widths scaled to particular pixel sizes. 
        //This allows the font manager to build integer width tables without calling the scaler for each glyph.
        //Typically this table contains only selected screen sizes.
        //This table is sorted by pixel size. 
        //The checksum for this table applies to both subtables listed.

        //Note that for non-square pixel grids,
        //the character width (in pixels) will be used to determine which device record to use. 
        //For example, a 12 point character on a device with a resolution of 72x96 would be 12 pixels high and 16 pixels wide. 
        //The hdmx device record for 16 pixel characters would be used.

        //If bit 4 of the flag field in the 'head' table is not set,
        //then it is assumed that the font scales linearly; in this case an 'hdmx' table is not necessary and should not be built.
        //If bit 4 of the flag field is set, then one or more glyphs in the font are assumed to scale nonlinearly. 
        //In this case, performance can be improved by including the 'hdmx' table with one or more important DeviceRecord's for important sizes.
        //Please see the chapter “Recommendations for OpenType Fonts” for more detail.

        //The table begins as follows:
        //hdmx  Header 
        //Type 	    Name 	            Description
        //USHORT    version 	        Table version number (0)
        //SHORT     numRecords 	        Number of device records.
        //LONG 	    sizeDeviceRecord 	Size of a device record, long aligned.
        //DeviceRecord 	records[numRecords] 	Array of device records.

        //Each DeviceRecord for format 0 looks like this.
        //Device Record
        //Type 	    Name 	            Description
        //BYTE 	    pixelSize 	        Pixel size for following widths (as ppem).
        //BYTE 	    maxWidth 	        Maximum width.
        //BYTE 	    widths[numGlyphs] 	Array of widths (numGlyphs is from the 'maxp' table).

        //Each DeviceRecord is padded with 0's to make it long word aligned.

        //Each Width value is the width of the particular glyph, in pixels,
        //at the pixels per em (ppem) size listed at the start of the DeviceRecord.

        //The ppem sizes are measured along the y axis. 

        protected override void ReadContentFrom(BinaryReader reader)
        {

        }
    }
}
