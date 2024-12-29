//Apache2, 2016-present, WinterDev

using System.IO;

namespace Typography.OpenFont.Tables
{

    //https://docs.microsoft.com/en-us/typography/opentype/spec/featurelist
    //The order for applying standard features encoded in OpenType fonts:

    //Feature   	Feature function 	                                Layout operation 	Required
    //---------------------
    //Language based forms: 		
    //---------------------
    //ccmp 	        Character composition/decomposition substitution 	GSUB 	
    //---------------------
    //Typographical forms: 
    //---------------------	
    //liga 	        Standard ligature substitution 	                    GSUB 	
    //clig 	        Contextual ligature substitution 	                GSUB 	
    //Positioning features: 		
    //kern 	        Pair kerning 	                                    GPOS 	
    //mark 	        Mark to base positioning 	                        GPOS 	X
    //mkmk 	        Mark to mark positioning 	                        GPOS 	X

    //[GSUB = glyph substitution, GPOS = glyph positioning]

    public class FeatureList
    {


        public FeatureTable[] featureTables;
        public static FeatureList CreateFrom(BinaryReader reader, long beginAt)
        {
            //https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2

            //------------------
            //FeatureList table                      
            //------------------
            //Type 	    Name 	        Description
            //uint16 	FeatureCount 	Number of FeatureRecords in this table
            //struct 	FeatureRecord[FeatureCount] 	Array of FeatureRecords-zero-based (first feature has FeatureIndex = 0)-listed alphabetically by FeatureTag
            //------------------
            //FeatureRecord
            //------------------
            //Type 	    Name 	        Description
            //Tag 	    FeatureTag 	    4-byte feature identification tag
            //Offset16 	Feature 	    Offset to Feature table-from beginning of FeatureList
            //----------------------------------------------------
            reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
            //
            FeatureList featureList = new FeatureList();
            ushort featureCount = reader.ReadUInt16();
            FeatureRecord[] featureRecords = new FeatureRecord[featureCount];
            for (int i = 0; i < featureCount; ++i)
            {
                //read script record
                featureRecords[i] = new FeatureRecord(
                    reader.ReadUInt32(), //feature tag
                    reader.ReadUInt16()); //Offset16 
            }
            //read each feature table
            FeatureTable[] featureTables = featureList.featureTables = new FeatureTable[featureCount];
            for (int i = 0; i < featureCount; ++i)
            {
                FeatureRecord frecord = featureRecords[i];
                (featureTables[i] = FeatureTable.CreateFrom(reader, beginAt + frecord.offset)).FeatureTag = frecord.featureTag;
            }
            return featureList;
        }
        readonly struct FeatureRecord
        {
            public readonly uint featureTag;//4-byte ScriptTag identifier
            public readonly ushort offset; //Script Offset to Script table-from beginning of ScriptList
            public FeatureRecord(uint featureTag, ushort offset)
            {
                this.featureTag = featureTag;
                this.offset = offset;
            }

            public string FeatureName => Utils.TagToString(featureTag);
#if DEBUG
            public override string ToString()
            {
                return FeatureName + "," + offset;
            }
#endif
        }


        //Feature Table

        //A Feature table defines a feature with one or more lookups.
        //The client uses the lookups to substitute or position glyphs.

        //Feature tables defined within the GSUB table contain references to glyph substitution lookups,
        //and feature tables defined within the GPOS table contain references to glyph positioning lookups.
        //If a text-processing operation requires both glyph substitution and positioning,
        //then both the GSUB and GPOS tables must each define a Feature table,
        //and the tables must use the same FeatureTags.

        //A Feature table consists of an offset to a Feature Parameters (FeatureParams) table 
        //(if one has been defined for this feature - see note in the following paragraph), 
        //a count of the lookups listed for the feature (LookupCount), 
        //and an arbitrarily ordered array of indices into a LookupList (LookupListIndex).
        //The LookupList indices are references into an array of offsets to Lookup tables.

        //The format of the Feature Parameters table is specific to a particular feature, 
        //and must be specified in the feature's entry in the Feature Tags section of the OpenType Layout Tag Registry. 
        //The length of the Feature Parameters table must be implicitly or explicitly specified in the Feature Parameters table itself.
        //The FeatureParams field in the Feature Table records the offset relative to the beginning of the Feature Table.
        //If a Feature Parameters table is not needed, the FeatureParams field must be set to NULL.

        //To identify the features in a GSUB or GPOS table,
        //a text-processing client reads the FeatureTag of each FeatureRecord referenced in a given LangSys table. 
        //Then the client selects the features it wants to implement and uses the LookupList to retrieve the Lookup indices of the chosen features.
        //Next, the client arranges the indices in the LookupList order. 
        //Finally, the client applies the lookup data to substitute or position glyphs.

        //Example 3 at the end of this chapter shows the FeatureList and Feature tables used to substitute ligatures in two languages.
        //


        public class FeatureTable
        {
            public static FeatureTable CreateFrom(BinaryReader reader, long beginAt)
            {
                reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
                //
                ushort featureParams = reader.ReadUInt16();
                ushort lookupCount = reader.ReadUInt16();

                FeatureTable featureTable = new FeatureTable();
                featureTable.LookupListIndices = Utils.ReadUInt16Array(reader, lookupCount);
                return featureTable;
            }
            public ushort[] LookupListIndices { get; private set; }
            public uint FeatureTag { get; set; }
            public string TagName => Utils.TagToString(this.FeatureTag);
#if DEBUG
            public override string ToString()
            {
                return this.TagName;
            }
#endif 
        }
    }
}