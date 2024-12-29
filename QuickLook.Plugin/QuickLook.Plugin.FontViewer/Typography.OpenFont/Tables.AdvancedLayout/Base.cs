//Apache2, 2016-present, WinterDev
//https://docs.microsoft.com/en-us/typography/opentype/spec/base
//BASE - Baseline Table
//The Baseline table (BASE) provides information used to align glyphs of different scripts and sizes in a line of text, 
//whether the glyphs are in the same font or in different fonts.
//To improve text layout, the Baseline table also provides minimum (min) and maximum (max) glyph extent values for each script,
//language system, or feature in a font.

using System.IO;

namespace Typography.OpenFont.Tables
{
    public class BASE : TableEntry
    {
        public const string _N = "BASE";
        public override string Name => _N;

        public AxisTable _horizontalAxis;
        public AxisTable _verticalAxis;

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //BASE Header

            //The BASE table begins with a header that starts with a version number.
            //Two versions are defined. 
            //Version 1.0 contains offsets to horizontal and vertical Axis tables(HorizAxis and VertAxis). 
            //Version 1.1 also includes an offset to an Item Variation Store table.

            //Each Axis table stores all baseline information and min / max extents for one layout direction.
            //The HorizAxis table contains Y values for horizontal text layout;
            //the VertAxis table contains X values for vertical text layout.


            // A font may supply information for both layout directions.
            //If a font has values for only one text direction, 
            //the Axis table offset value for the other direction will be set to NULL.

            //The optional Item Variation Store table is used in variable fonts to provide variation data 
            //for BASE metric values within the Axis tables.


            // BASE Header, Version 1.0
            //Type      Name                Description
            //uint16    majorVersion        Major version of the BASE table, = 1
            //uint16    minorVersion        Minor version of the BASE table, = 0
            //Offset16  horizAxisOffset     Offset to horizontal Axis table, from beginning of BASE table(may be NULL)
            //Offset16  vertAxisOffset      Offset to vertical Axis table, from beginning of BASE table(may be NULL)

            //BASE Header, Version 1.1
            //Type      Name                Description
            //uint16    majorVersion        Major version of the BASE table, = 1
            //uint16    minorVersion        Minor version of the BASE table, = 1
            //Offset16  horizAxisOffset     Offset to horizontal Axis table, from beginning of BASE table(may be NULL)
            //Offset16  vertAxisOffset      Offset to vertical Axis table, from beginning of BASE table(may be NULL)
            //Offset32  itemVarStoreOffset  Offset to Item Variation Store table, from beginning of BASE table(may be null)


            long tableStartAt = reader.BaseStream.Position;

            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort horizAxisOffset = reader.ReadUInt16();
            ushort vertAxisOffset = reader.ReadUInt16();
            uint itemVarStoreOffset = 0;
            if (minorVersion == 1)
            {
                itemVarStoreOffset = reader.ReadUInt32();
            }

            //Axis Tables: HorizAxis and VertAxis 

            if (horizAxisOffset > 0)
            {
                reader.BaseStream.Position = tableStartAt + horizAxisOffset;
                _horizontalAxis = ReadAxisTable(reader);
                _horizontalAxis.isVerticalAxis = false;
            }
            if (vertAxisOffset > 0)
            {
                reader.BaseStream.Position = tableStartAt + vertAxisOffset;
                _verticalAxis = ReadAxisTable(reader);
                _verticalAxis.isVerticalAxis = true;
            }
            if (itemVarStoreOffset > 0)
            {
                //TODO
            }

        }

        public class AxisTable
        {
            public bool isVerticalAxis; //false = horizontal , true= verical axis
            public string[] baseTagList;
            public BaseScript[] baseScripts;
#if DEBUG
            public override string ToString()
            {
                return isVerticalAxis ? "vertical_axis" : "horizontal_axis";
            }
#endif
        }

        static AxisTable ReadAxisTable(BinaryReader reader)
        {
            //An Axis table is used to render scripts either horizontally or vertically. 
            //It consists of offsets, measured from the beginning of the Axis table,
            //to a BaseTagList and a BaseScriptList:

            //The BaseScriptList enumerates all scripts rendered in the text layout direction.
            //The BaseTagList enumerates all baselines used to render the scripts in the text layout direction.
            //If no baseline data is available for a text direction,
            //the offset to the corresponding BaseTagList may be set to NULL.

            //Axis Table
            //Type        Name                      Description
            //Offset16    baseTagListOffset         Offset to BaseTagList table, from beginning of Axis table(may be NULL)
            //Offset16    baseScriptListOffset      Offset to BaseScriptList table, from beginning of Axis table

            long axisTableStartAt = reader.BaseStream.Position;

            ushort baseTagListOffset = reader.ReadUInt16();
            ushort baseScriptListOffset = reader.ReadUInt16();

            AxisTable axisTable = new AxisTable();
            if (baseTagListOffset > 0)
            {
                reader.BaseStream.Position = axisTableStartAt + baseTagListOffset;
                axisTable.baseTagList = ReadBaseTagList(reader);
            }
            if (baseScriptListOffset > 0)
            {
                reader.BaseStream.Position = axisTableStartAt + baseScriptListOffset;
                axisTable.baseScripts = ReadBaseScriptList(reader);
            }
            return axisTable;
        }

        static string ConvertToTagString(byte[] iden_tag_bytes)
        {
            return new string(new char[] {
                 (char)iden_tag_bytes[0],
                 (char)iden_tag_bytes[1],
                 (char)iden_tag_bytes[2],
                 (char)iden_tag_bytes[3]});
        }
        static string[] ReadBaseTagList(BinaryReader reader)
        {
            //BaseTagList Table

            //The BaseTagList table identifies the baselines for all scripts in the font that are rendered in the same text direction. 
            //Each baseline is identified with a 4-byte baseline tag. 
            //The Baseline Tags section of the OpenType Layout Tag Registry lists currently registered baseline tags.
            //The BaseTagList can define any number of baselines, and it may include baseline tags for scripts supported in other fonts.

            //Each script in the BaseScriptList table must designate one of these BaseTagList baselines as its default,
            //which the OpenType Layout Services use to align all glyphs in the script. 
            //Even though the BaseScriptList and the BaseTagList are defined independently of one another, 
            //the BaseTagList typically includes a tag for each different default baseline needed to render the scripts in the layout direction.
            //If some scripts use the same default baseline, the BaseTagList needs to list the common baseline tag only once.

            //The BaseTagList table consists of an array of baseline identification tags (baselineTags),
            //listed alphabetically, and a count of the total number of baseline Tags in the array (baseTagCount).

            //BaseTagList table
            //Type 	    Name 	                    Description
            //uint16 	baseTagCount 	            Number of baseline identification tags in this text direction — may be zero (0)
            //Tag 	    baselineTags[baseTagCount] 	Array of 4-byte baseline identification tags — must be in alphabetical order

            //see baseline tag =>  https://docs.microsoft.com/en-us/typography/opentype/spec/baselinetags


            //Baseline Tag                  
            //'hang'  
            //Baseline for HorizAxis: The hanging baseline.This is the horizontal line from which syllables seem to hang in Tibetan and other similar scripts.
            //Baseline for VertAxis:  The hanging baseline, (which now appears vertical) for Tibetan(or some other similar script) characters rotated 90 degrees clockwise, 
            //                        for vertical writing mode.            
            //------
            //'icfb'  
            //HorizAxis: Ideographic character face bottom edge. (See Ideographic Character Face below for usage.)
            //VertAxis: Ideographic character face left edge. (See Ideographic Character Face below for usage.)
            //--------
            //'icft' 
            //HorizAxis: Ideographic character face top edge. (See Ideographic Character Face below for usage.) 
            //VertAxis: Ideographic character face right edge. (See Ideographic Character Face below for usage.)
            //-----
            //'ideo' 
            //HorizAxis: Ideographic em-box bottom edge. (See Ideographic Em-Box below for usage.) 
            //VertAxis: Ideographic em-box left edge. If this tag is present in the VertAxis, the value must be set to 0. (See Ideographic Em - Box below for usage.)

            //-------
            //'idtp'  
            //HorizAxis: Ideographic em-box top edge baseline. (See Ideographic Em - Box below for usage.)
            //VertAxis: Ideographic em-box right edge baseline.
            //          If this tag is present in the VertAxis, 
            //          the value is strongly recommended to be set to head.unitsPerEm. (See Ideographic Em - Box below for usage.)
            //-------
            //'math' 
            //HorizAxis: The baseline about which mathematical characters are centered. 	
            //VertAxis: The baseline about which mathematical characters, when rotated 90 degrees clockwise for vertical writing mode, are centered.

            //-------
            //'romn'
            //HorizAxis: The baseline used by alphabetic scripts such as Latin, Cyrillic and Greek.
            //VertAxis: The alphabetic baseline for characters rotated 90 degrees clockwise for vertical writing mode. (This would not apply to alphabetic characters that remain upright in vertical writing mode, since these characters are not rotated.)


            ushort baseTagCount = reader.ReadUInt16();
            string[] baselineTags = new string[baseTagCount];
            for (int i = 0; i < baseTagCount; ++i)
            {
                baselineTags[i] = ConvertToTagString(reader.ReadBytes(4));
            }
            return baselineTags;
        }
        static BaseScript[] ReadBaseScriptList(BinaryReader reader)
        {
            //BaseScriptList Table

            //The BaseScriptList table identifies all scripts in the font that are rendered in the same layout direction. 
            //If a script is not listed here, then
            //the text-processing client will render the script using the layout information specified for the entire font.

            //For each script listed in the BaseScriptList table,
            //a BaseScriptRecord must be defined that identifies the script and references its layout data.
            //BaseScriptRecords are stored in the baseScriptRecords array, ordered alphabetically by the baseScriptTag in each record.
            //The baseScriptCount specifies the total number of BaseScriptRecords in the array.

            //BaseScriptList table
            //Type  	        Name 	                            Description
            //uint16 	        baseScriptCount 	                Number of BaseScriptRecords defined
            //BaseScriptRecord 	baseScriptRecords[baseScriptCount] 	Array of BaseScriptRecords, in alphabetical order by baseScriptTag

            long baseScriptListStartAt = reader.BaseStream.Position;
            ushort baseScriptCount = reader.ReadUInt16();

            BaseScriptRecord[] baseScriptRecord_offsets = new BaseScriptRecord[baseScriptCount];
            for (int i = 0; i < baseScriptCount; ++i)
            {
                //BaseScriptRecord

                //A BaseScriptRecord contains a script identification tag (baseScriptTag), 
                //which must be identical to the ScriptTag used to define the script in the ScriptList of a GSUB or GPOS table. 
                //Each record also must include an offset to a BaseScript table that defines the baseline and min/max extent data for the script.             

                //BaseScriptRecord
                //Type 	    Name 	            Description
                //Tag 	    baseScriptTag 	    4-byte script identification tag
                //Offset16 	baseScriptOffset 	Offset to BaseScript table, from beginning of BaseScriptList             
                baseScriptRecord_offsets[i] = new BaseScriptRecord(ConvertToTagString(reader.ReadBytes(4)), reader.ReadUInt16());
            }
            BaseScript[] baseScripts = new BaseScript[baseScriptCount];
            for (int i = 0; i < baseScriptCount; ++i)
            {
                BaseScriptRecord baseScriptRecord = baseScriptRecord_offsets[i];
                reader.BaseStream.Position = baseScriptListStartAt + baseScriptRecord.baseScriptOffset;
                //
                BaseScript baseScipt = ReadBaseScriptTable(reader);
                baseScipt.ScriptIdenTag = baseScriptRecord.baseScriptTag;
                baseScripts[i] = baseScipt;
            }
            return baseScripts;
        }
        readonly struct BaseScriptRecord
        {
            public readonly string baseScriptTag;
            public readonly ushort baseScriptOffset;
            public BaseScriptRecord(string scriptTag, ushort offset)
            {
                this.baseScriptTag = scriptTag;
                this.baseScriptOffset = offset;
            }
        }
        public readonly struct BaseLangSysRecord
        {
            public readonly string baseScriptTag;
            public readonly ushort baseScriptOffset;
            public BaseLangSysRecord(string scriptTag, ushort offset)
            {
                this.baseScriptTag = scriptTag;
                this.baseScriptOffset = offset;
            }
        }

        public class BaseScript
        {
            public string ScriptIdenTag;
            public BaseValues baseValues;
            public BaseLangSysRecord[] baseLangSysRecords;
            public MinMax MinMax;
            public BaseScript() { }

#if DEBUG
            public override string ToString()
            {
                return ScriptIdenTag;
            }
#endif
        }
        static BaseScript ReadBaseScriptTable(BinaryReader reader)
        {
            //BaseScript Table
            //A BaseScript table organizes and specifies the baseline data and min/max extent data for one script. 
            //Within a BaseScript table, the BaseValues table contains baseline information, 
            //and one or more MinMax tables contain min/max extent data
            //....

            //A BaseScript table has four components:
            //...

            long baseScriptTableStartAt = reader.BaseStream.Position;

            //BaseScript Table
            //Type 	                Name 	                                Description
            //Offset16 	            baseValuesOffset 	                    Offset to BaseValues table, from beginning of BaseScript table (may be NULL)
            //Offset16 	            defaultMinMaxOffset 	                Offset to MinMax table, from beginning of BaseScript table (may be NULL)
            //uint16    	        baseLangSysCount 	                    Number of BaseLangSysRecords defined — may be zero (0)
            //BaseLangSysRecord 	baseLangSysRecords[baseLangSysCount] 	Array of BaseLangSysRecords, in alphabetical order by BaseLangSysTag

            ushort baseValueOffset = reader.ReadUInt16();
            ushort defaultMinMaxOffset = reader.ReadUInt16();
            ushort baseLangSysCount = reader.ReadUInt16();
            BaseLangSysRecord[] baseLangSysRecords = null;

            if (baseLangSysCount > 0)
            {
                baseLangSysRecords = new BaseLangSysRecord[baseLangSysCount];
                for (int i = 0; i < baseLangSysCount; ++i)
                {
                    //BaseLangSysRecord
                    //A BaseLangSysRecord defines min/max extents for a language system or a language-specific feature.
                    //Each record contains an identification tag for the language system (baseLangSysTag) and an offset to a MinMax table (MinMax) 
                    //that defines extent coordinate values for the language system and references feature-specific extent data.

                    //BaseLangSysRecord
                    //Type 	        Name 	        Description
                    //Tag 	        baseLangSysTag 	4-byte language system identification tag
                    //Offset16 	    minMaxOffset 	Offset to MinMax table, from beginning of BaseScript table
                    baseLangSysRecords[i] = new BaseLangSysRecord(ConvertToTagString(reader.ReadBytes(4)), reader.ReadUInt16());
                }
            }

            BaseScript baseScript = new BaseScript();
            baseScript.baseLangSysRecords = baseLangSysRecords;
            //--------------------
            if (baseValueOffset > 0)
            {
                reader.BaseStream.Position = baseScriptTableStartAt + baseValueOffset;
                baseScript.baseValues = ReadBaseValues(reader);

            }
            if (defaultMinMaxOffset > 0)
            {
                reader.BaseStream.Position = baseScriptTableStartAt + defaultMinMaxOffset;
                baseScript.MinMax = ReadMinMaxTable(reader);
            }

            return baseScript;
        }
        static BaseValues ReadBaseValues(BinaryReader reader)
        {
            //A BaseValues table lists the coordinate positions of all baselines named in the baselineTags array of the corresponding BaseTagList and
            //identifies a default baseline for a script.

            //...
            //
            //BaseValues table
            //Type 	    Name 	                    Description
            //uint16 	defaultBaselineIndex 	    Index number of default baseline for this script — equals index position of baseline tag in baselineTags array of the BaseTagList
            //uint16 	baseCoordCount          	Number of BaseCoord tables defined — should equal baseTagCount in the BaseTagList
            //Offset16 	baseCoords[baseCoordCount] 	Array of offsets to BaseCoord tables, from beginning of BaseValues table — order matches baselineTags array in the BaseTagList

            long baseValueTableStartAt = reader.BaseStream.Position;

            //
            ushort defaultBaselineIndex = reader.ReadUInt16();
            ushort baseCoordCount = reader.ReadUInt16();
            ushort[] baseCoords_Offset = Utils.ReadUInt16Array(reader, baseCoordCount);

            BaseCoord[] baseCoords = new BaseCoord[baseCoordCount];
            for (int i = 0; i < baseCoordCount; ++i)
            {
                baseCoords[i] = ReadBaseCoordTable(reader, baseValueTableStartAt + baseCoords_Offset[i]);
            }

            return new BaseValues(defaultBaselineIndex, baseCoords);
        }

        public readonly struct BaseValues
        {
            public readonly ushort defaultBaseLineIndex;
            public readonly BaseCoord[] baseCoords;

            public BaseValues(ushort defaultBaseLineIndex, BaseCoord[] baseCoords)
            {
                this.defaultBaseLineIndex = defaultBaseLineIndex;
                this.baseCoords = baseCoords;
            }
        }


        public readonly struct BaseCoord
        {
            public readonly ushort baseCoordFormat;
            /// <summary>
            ///  X or Y value, in design units
            /// </summary>
            public readonly short coord;

            public readonly ushort referenceGlyph; //found in format2
            public readonly ushort baseCoordPoint; //found in format2

            public BaseCoord(ushort baseCoordFormat, short coord)
            {
                this.baseCoordFormat = baseCoordFormat;
                this.coord = coord;
                this.referenceGlyph = this.baseCoordPoint = 0;
            }
            public BaseCoord(ushort baseCoordFormat, short coord, ushort referenceGlyph, ushort baseCoordPoint)
            {
                this.baseCoordFormat = baseCoordFormat;
                this.coord = coord;
                this.referenceGlyph = referenceGlyph;
                this.baseCoordPoint = baseCoordPoint;
            }
#if DEBUG
            public override string ToString()
            {
                return "format:" + baseCoordFormat + ",coord=" + coord;
            }
#endif
        }
        static BaseCoord ReadBaseCoordTable(BinaryReader reader, long pos)
        {
            reader.BaseStream.Position = pos;
            //BaseCoord Tables
            //Within the BASE table, a BaseCoord table defines baseline and min/max extent values.
            //Each BaseCoord table defines one X or Y value:

            //If defined within the HorizAxis table, then the BaseCoord table contains a Y value.
            //If defined within the VertAxis table, then the BaseCoord table contains an X value.

            //All values are defined in design units, which typically are scaled and rounded to the nearest integer when scaling the glyphs. 
            //Values may be negative.

            //----------------------
            //BaseCoord Format 1
            //The first BaseCoord format (BaseCoordFormat1) consists of a format identifier, 
            //followed by a single design unit coordinate that specifies the BaseCoord value. 
            //This format has the benefits of small size and simplicity, 
            //but the BaseCoord value cannot be hinted for fine adjustments at different sizes or device resolutions.

            //BaseCoordFormat1 table: Design units only
            //Type 	    Name 	            Description
            //uint16 	baseCoordFormat 	Format identifier — format = 1
            //int16 	coordinate 	        X or Y value, in design units
            //----------------------

            //BaseCoord Format 2

            //The second BaseCoord format (BaseCoordFormat2) specifies the BaseCoord value in design units, 
            //but also supplies a glyph index and a contour point for reference. During font hinting,
            //the contour point on the glyph outline may move. 
            //The point’s final position after hinting provides the final value for rendering a given font size.

            //Note: Glyph positioning operations defined in the GPOS table do not affect the point’s final position.          

            //BaseCoordFormat2 table: Design units plus contour point
            //Type 	    Name 	            Description
            //uint16 	baseCoordFormat 	Format identifier — format = 2
            //int16 	coordinate 	        X or Y value, in design units
            //uint16 	referenceGlyph 	    Glyph ID of control glyph
            //uint16 	baseCoordPoint 	    Index of contour point on the reference glyph

            //----------------------
            //BaseCoord Format 3

            //The third BaseCoord format (BaseCoordFormat3) also specifies the BaseCoord value in design units, 
            //but, in a non-variable font, it uses a Device table rather than a contour point to adjust the value. 
            //This format offers the advantage of fine-tuning the BaseCoord value for any font size and device resolution. 
            //(For more information about Device tables, see the chapter, Common Table Formats.)

            //In a variable font, BaseCoordFormat3 must be used to reference variation data 
            //to adjust the X or Y value for different variation instances, if needed.
            //In this case, BaseCoordFormat3 specifies an offset to a VariationIndex table,
            //which is a variant of the Device table that is used for referencing variation data.

            // Note: While separate VariationIndex table references are required for each Coordinate value that requires variation, two or more values that require the same variation-data values can have offsets that point to the same VariationIndex table, and two or more VariationIndex tables can reference the same variation data entries.

            // Note: If no VariationIndex table is used for a particular X or Y value (the offset is zero, or a different BaseCoord format is used), then that value is used for all variation instances.



            //BaseCoordFormat3 table: Design units plus Device or VariationIndex table
            //Type 	    Name 	            Description
            //uint16 	baseCoordFormat 	Format identifier — format = 3
            //int16 	coordinate 	        X or Y value, in design units
            //Offset16 	deviceTable 	    Offset to Device table (non-variable font) / Variation Index table (variable font) for X or Y value, from beginning of BaseCoord table (may be NULL).

            ushort baseCoordFormat = reader.ReadUInt16();
            switch (baseCoordFormat)
            {
                default: throw new System.NotSupportedException();
                case 1:
                    return new BaseCoord(1,
                        reader.ReadInt16());//coord
                case 2:
                    return new BaseCoord(2,
                        reader.ReadInt16(), //coordinate
                        reader.ReadUInt16(), //referenceGlyph
                        reader.ReadUInt16()); //baseCoordPoint
                case 3:
#if DEBUG

#endif
                    return new BaseCoord();
                    //    //TODO: implement this...
                    //    break;
            }


        }


        static MinMax ReadMinMaxTable(BinaryReader reader)
        {
            //The MinMax table specifies extents for scripts and language systems.
            //It also contains an array of FeatMinMaxRecords used to define feature-specific extents.

            //...

            //Text-processing clients should use the following procedure to access the script, language system, and feature-specific extent data:

            //Determine script extents in relation to the text content.
            //Select language-specific extent values with respect to the language system in use.
            //Have the application or user choose feature-specific extent values.
            //If no extent values are defined for a language system or for language-specific features,
            //use the default min/max extent values for the script.

            //MinMax table
            //Type 	            Name 	            Description
            //Offset16 	        minCoord 	        Offset to BaseCoord table that defines the minimum extent value, from the beginning of MinMax table (may be NULL)
            //Offset16      	maxCoord 	        Offset to BaseCoord table that defines maximum extent value, from the beginning of MinMax table (may be NULL)
            //uint16 	        featMinMaxCount 	Number of FeatMinMaxRecords — may be zero (0)
            //FeatMinMaxRecord 	featMinMaxRecords[featMinMaxCount] 	Array of FeatMinMaxRecords, in alphabetical order by featureTableTag


            //FeatMinMaxRecord
            //Type              Name                Description
            //Tag               featureTableTag     4 - byte feature identification tag — must match feature tag in FeatureList
            //Offset16          minCoord            Offset to BaseCoord table that defines the minimum extent value, from beginning of MinMax table(may be NULL)
            //Offset16          maxCoord            Offset to BaseCoord table that defines the maximum extent value, from beginning of MinMax table(may be NULL)


            long startMinMaxTableAt = reader.BaseStream.Position;
            //
            MinMax minMax = new MinMax();
            ushort minCoordOffset = reader.ReadUInt16();
            ushort maxCoordOffset = reader.ReadUInt16();
            ushort featMinMaxCount = reader.ReadUInt16();

            FeatureMinMaxOffset[] minMaxFeatureOffsets = null;
            if (featMinMaxCount > 0)
            {
                minMaxFeatureOffsets = new FeatureMinMaxOffset[featMinMaxCount];
                for (int i = 0; i < featMinMaxCount; ++i)
                {
                    minMaxFeatureOffsets[i] = new FeatureMinMaxOffset(
                        ConvertToTagString(reader.ReadBytes(4)), //featureTableTag
                        reader.ReadUInt16(), //minCoord offset
                        reader.ReadUInt16() //maxCoord offset
                        );
                }
            }

            //----------
            if (minCoordOffset > 0)
            {
                minMax.minCoord = ReadBaseCoordTable(reader, startMinMaxTableAt + minCoordOffset);
            }
            if (maxCoordOffset > 0)
            {
                minMax.maxCoord = ReadBaseCoordTable(reader, startMinMaxTableAt + maxCoordOffset);
            }

            if (minMaxFeatureOffsets != null)
            {
                var featureMinMaxRecords = new FeatureMinMax[minMaxFeatureOffsets.Length];
                for (int i = 0; i < minMaxFeatureOffsets.Length; ++i)
                {
                    FeatureMinMaxOffset featureMinMaxOffset = minMaxFeatureOffsets[i];

                    featureMinMaxRecords[i] = new FeatureMinMax(
                        featureMinMaxOffset.featureTableTag, //tag
                        ReadBaseCoordTable(reader, startMinMaxTableAt + featureMinMaxOffset.minCoord), //min
                        ReadBaseCoordTable(reader, startMinMaxTableAt + featureMinMaxOffset.maxCoord)); //max
                }
                minMax.featureMinMaxRecords = featureMinMaxRecords;
            }

            return minMax;
        }

        public class MinMax
        {
            public BaseCoord minCoord;
            public BaseCoord maxCoord;
            public FeatureMinMax[] featureMinMaxRecords;
        }
        public readonly struct FeatureMinMax
        {
            public readonly string featureTableTag;
            public readonly BaseCoord minCoord;
            public readonly BaseCoord maxCoord;
            public FeatureMinMax(string tag, BaseCoord minCoord, BaseCoord maxCoord)
            {
                featureTableTag = tag;
                this.minCoord = minCoord;
                this.maxCoord = maxCoord;
            }
        }
        readonly struct FeatureMinMaxOffset
        {
            public readonly string featureTableTag;
            public readonly ushort minCoord;
            public readonly ushort maxCoord;
            public FeatureMinMaxOffset(string featureTableTag, ushort minCoord, ushort maxCoord)
            {
                this.featureTableTag = featureTableTag;
                this.minCoord = minCoord;
                this.maxCoord = maxCoord;
            }
        }

    }
}