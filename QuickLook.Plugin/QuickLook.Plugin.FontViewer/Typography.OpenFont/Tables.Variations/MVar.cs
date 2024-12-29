//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables
{

    //https://docs.microsoft.com/en-us/typography/opentype/spec/mvar

    /// <summary>
    /// MVAR — Metrics Variations Table
    /// </summary>
    class MVar : TableEntry
    {
        public const string _N = "MVAR";
        public override string Name => _N;

        //The metrics variations table is used in variable fonts 
        //to provide variations for font-wide metric values 
        //found in the OS/2 table and other font tables

        //The metrics variations table contains an item variation store structure to represent variation data.
        //The item variation store and constituent formats are described in the chapter, 
        //OpenType Font Variations Common Table Formats.

        //The item variation store is also used in the HVAR and GDEF tables,
        //**BUT** is different from the formats for variation data used in the 'cvar' or 'gvar' tables.

        //The item variation store format uses delta-set indices to reference variation delta data 
        //for particular target font-data items to which they are applied. 
        //Data external to the item variation store identifies the delta-set index to be used for each given target item. 

        //Within the MVAR table, an array of value tag records identifies a set of target items,
        //and provides the delta-set index used for each. 
        //The target items are identified by four-byte tags,
        //with a given tag representing some font-wide value found in another table

        //The item variation store format uses a two-level organization for variation data:
        //a store can have multiple item variation data subtables, and  each subtable has multiple delta-set rows.

        //A delta-set index is a two-part index: 
        //an outer index that selects a particular item variation data subtable,
        //and an inner index that selects a particular delta-set row within that subtable.

        //A value record specifies both the outer and inner portions of the delta-set index.


        public ValueRecord[] valueRecords;
        public ItemVariationStoreTable itemVariationStore;

        public MVar()
        {

        }
        protected override void ReadContentFrom(BinaryReader reader)
        {
            long startAt = reader.BaseStream.Position;

            //Metrics variations table:
            //Type      Name                Description
            //uint16    majorVersion        Major version number of the metrics variations table — set to 1.
            //uint16    minorVersion        Minor version number of the metrics variations table — set to 0.
            //uint16    (reserved)          Not used; set to 0.
            //uint16    valueRecordSize     The size in bytes of each value record — must be greater than zero.
            //uint16    valueRecordCount    The number of value records — may be zero.
            //Offset16  itemVariationStoreOffset    Offset in bytes from the start of this table to the item variation store table.
            //                                      If valueRecordCount is zero, set to zero; 
            //                                      if valueRecordCount is greater than zero, must be greater than zero.
            //ValueRecord valueRecords[valueRecordCount]  Array of value records that identify target items and the associated delta-set index for each.
            //                                           The valueTag records must be in binary order of their valueTag field.

            //-----------
            //
            //The valueRecordSize field indicates the size of each value record. 
            //Future, minor version updates of the MVAR table may define compatible extensions to the value record format with additional fields.
            //**Implementations must use the valueRecordSize field to determine the start of each record.**

            //The valueRecords array is an array of value records that identify the target,
            //font -wide measures for which variation adjustment data is provided (target items),
            //and outer and inner delta-set indices for each item into the item variation store data.


            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort reserved = reader.ReadUInt16();
            ushort valueRecordSize = reader.ReadUInt16();
            ushort valueRecordCount = reader.ReadUInt16();
            ushort itemVariationStoreOffset = reader.ReadUInt16();

            valueRecords = new ValueRecord[valueRecordCount];

            for (int i = 0; i < valueRecordCount; ++i)
            {
                long recStartAt = reader.BaseStream.Position;
                valueRecords[i] = new ValueRecord(
                    reader.ReadUInt32(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16()
                    );

                reader.BaseStream.Position = recStartAt + valueRecordSize;//**Implementations must use the valueRecordSize field to determine the start of each record.**
            }

            //
            //item variation store table
            if (valueRecordCount > 0)
            {
                reader.BaseStream.Position = startAt + itemVariationStoreOffset;
                itemVariationStore = new ItemVariationStoreTable();
                itemVariationStore.ReadContentFrom(reader);
            }

        }

        public readonly struct ValueRecord
        {
            //ValueRecord:
            //Type      Name                Description
            //Tag       valueTag            Four-byte tag identifying a font-wide measure.
            //uint16    deltaSetOuterIndex  A delta-set outer index — used to select an item variation data subtable within the item variation store.
            //uint16    deltaSetInnerIndex  A delta-set inner index — used to select a delta-set row within an item variation data subtable.

            //The value records must be given in binary order of the valueTag values.
            //Each tag identifies a font-wide measure found in some other font table.
            //For example, if a value record has a value tag of 'hasc',
            //this corresponds to the OS/2.sTypoAscender field. Details on the tags used within the MVAR table are provided below.

            public readonly uint tag;
            public readonly ushort deltaSetOuterIndex;
            public readonly ushort deltaSetInnerIndex;
            public ValueRecord(uint tag, ushort deltaSetOuterIndex, ushort deltaSetInnerIndex)
            {
                this.tag = tag;
                this.deltaSetOuterIndex = deltaSetOuterIndex;
                this.deltaSetInnerIndex = deltaSetInnerIndex;


                //Tags in the metrics variations table are case sensitive.
                //Tags defined in this table use only lowercase letters or digits.

                //Tags that are used in a font’s metrics variations table should be those that are documented in this table specification. 
                //A font may also use privately-defined tags, which have semantics known only by private agreement.

                //Private-use tags must use begin with an uppercase letter and use only uppercase letters or digits.
                //If a private-use tag is used in a given font, any application that does not recognize that tag should ignore it.
            }
            public string TranslatedTag => Utils.TagToString(tag);
#if DEBUG
            public override string ToString()
            {
                return Utils.TagToString(tag) + ",outer:" + deltaSetOuterIndex + ",inner:" + deltaSetInnerIndex;
            }
#endif
        }


        class ValueTagInfo
        {
            public readonly string Tag;
            public readonly string Mnemonic;
            public readonly string ValueRepresented;
            public ValueTagInfo(string tag, string mnemonic, string valueRepresented)
            {
                this.Tag = tag;
                this.Mnemonic = mnemonic;
                this.ValueRepresented = valueRepresented;
            }
#if DEBUG
            public override string ToString()
            {
                return Tag + ", " + Mnemonic + ", " + ValueRepresented;
            }
#endif
        }

        static class ValueTags
        {

            static Dictionary<string, ValueTagInfo> s_registerTags = new Dictionary<string, ValueTagInfo>();
            public static bool TryGetValueTagInfo(string tag, out ValueTagInfo valueTagInfo)
            {
                return s_registerTags.TryGetValue(tag, out valueTagInfo);
            }
            static void RegisterValueTagInfo(string tag, string mnemonic, string valueRepresented)
            {
                s_registerTags.Add(tag, new ValueTagInfo(tag, mnemonic, valueRepresented));
            }
            static ValueTags()
            {
                //Value tags, ordered by logical grouping:
                //Tag       Mnemonic                Value represented
                //'hasc' 	horizontal ascender     OS/2.sTypoAscender
                //'hdsc' 	horizontal descender    OS/2.sTypoDescender
                //'hlgp' 	horizontal line gap     OS/2.sTypoLineGap
                //'hcla' 	horizontal clipping ascent OS/2.usWinAscent
                //'hcld' 	horizontal clipping descent OS/2.usWinDescent
                RegisterValueTagInfo("hasc", "horizontal ascender", "OS/2.sTypoAscender");
                RegisterValueTagInfo("hdsc", "horizontal descender", "OS/2.sTypoDescender");
                RegisterValueTagInfo("hlgp", "horizontal line gap", "OS/2.sTypoLineGap");
                RegisterValueTagInfo("hcla", "horizontal clipping ascent", "OS/2.usWinAscent");
                RegisterValueTagInfo("hcld", "horizontal clipping descent", "OS/2.usWinDescent");

                //Tag       Mnemonic                Value represented
                //'vasc' 	vertical ascender       vhea.ascent
                //'vdsc' 	vertical descender      vhea.descent
                //'vlgp' 	vertical line gap       vhea.lineGap
                RegisterValueTagInfo("vasc", "vertical ascender", "vhea.ascent");
                RegisterValueTagInfo("vdsc", "vertical descender", "vhea.descent");
                RegisterValueTagInfo("vlgp", "vertical line gap", "vhea.lineGap");

                //Tag       Mnemonic                Value represented
                //'hcrs' 	horizontal caret rise   hhea.caretSlopeRise
                //'hcrn' 	horizontal caret run    hhea.caretSlopeRun
                //'hcof' 	horizontal caret offset hhea.caretOffset
                RegisterValueTagInfo("hcrs", "horizontal caret rise", "hhea.caretSlopeRise");
                RegisterValueTagInfo("hcrn", "horizontal caret run", "hhea.caretSlopeRun");
                RegisterValueTagInfo("hcof", "horizontal caret offset", "hhea.caretOffset");

                //Tag       Mnemonic                Value represented
                //'vcrs' 	vertical caret rise     vhea.caretSlopeRise
                //'vcrn' 	vertical caret run      vhea.caretSlopeRun
                //'vcof' 	vertical caret offset   vhea.caretOffset
                RegisterValueTagInfo("vcrs", "vertical caret rise", "vhea.caretSlopeRise");
                RegisterValueTagInfo("vcrn", "vertical caret run", "vhea.caretSlopeRun");
                RegisterValueTagInfo("vcof", "vertical caret offset", "vhea.caretOffset");

                //Tag       Mnemonic                Value represented
                //'xhgt' 	x height                OS/2.sxHeight
                //'cpht' 	cap height              OS/2.sCapHeight

                //'sbxs' 	subscript em x size     OS/2.ySubscriptXSize
                //'sbys' 	subscript em y size     OS/2.ySubscriptYSize

                //'sbxo' 	subscript em x offset   OS/2.ySubscriptXOffset
                //'sbyo' 	subscript em y offset   OS/2.ySubscriptYOffset

                //'spxs' 	superscript em x size   OS/2.ySuperscriptXSize
                //'spys' 	superscript em y size   OS/2.ySuperscriptYSize

                //'spxo' 	superscript em x offset OS/2.ySuperscriptXOffset
                //'spyo' 	superscript em y offset OS/2.ySuperscriptYOffset

                //'strs' 	strikeout size          OS/2.yStrikeoutSize
                //'stro' 	strikeout offset        OS/2.yStrikeoutPosition
                RegisterValueTagInfo("xhgt", "x height", "OS/2.sTypoAscender");
                RegisterValueTagInfo("cpht", "cap height", "OS/2.sTypoDescender");

                RegisterValueTagInfo("sbxs", "subscript em x size", "OS/2.ySubscriptXSize");
                RegisterValueTagInfo("sbys", "subscript em y size", "OS/2.ySubscriptYSize");

                RegisterValueTagInfo("sbxo", "subscript em x offset", "OS/2.ySubscriptXOffset");
                RegisterValueTagInfo("sbyo", "subscript em y offset", "OS/2.ySubscriptYOffset");

                RegisterValueTagInfo("spxs", "superscript em x size", "OS/2.ySuperscriptXSize");
                RegisterValueTagInfo("spys", "superscript em y size", "OS/2.ySuperscriptYSize");

                RegisterValueTagInfo("spxo", "superscript em x offset", "OS/2.ySuperscriptXOffset");
                RegisterValueTagInfo("spyo", "superscript em y offset", "OS/2.ySuperscriptYOffset");

                RegisterValueTagInfo("strs", "strikeout size", "OS/2.yStrikeoutSize");
                RegisterValueTagInfo("stro", "strikeout offset", "OS/2.yStrikeoutPosition");


                //Tag       Mnemonic                Value represented
                //'unds' 	underline size          post.underlineThickness
                //'undo' 	underline offset        post.underlinePosition
                RegisterValueTagInfo("unds", "underline size", "post.underlineThickness");
                RegisterValueTagInfo("undo", "underline offset", "post.underlinePosition");


                //Tag       Mnemonic                Value represented
                //'gsp0' 	gaspRange[0]            gasp.gaspRange[0].rangeMaxPPEM
                //'gsp1' 	gaspRange[1]            gasp.gaspRange[1].rangeMaxPPEM
                //'gsp2' 	gaspRange[2]            gasp.gaspRange[2].rangeMaxPPEM
                //'gsp3' 	gaspRange[3]            gasp.gaspRange[3].rangeMaxPPEM
                //'gsp4' 	gaspRange[4]            gasp.gaspRange[4].rangeMaxPPEM
                //'gsp5' 	gaspRange[5]            gasp.gaspRange[5].rangeMaxPPEM
                //'gsp6' 	gaspRange[6]            gasp.gaspRange[6].rangeMaxPPEM
                //'gsp7' 	gaspRange[7]            gasp.gaspRange[7].rangeMaxPPEM
                //'gsp8' 	gaspRange[8]            gasp.gaspRange[8].rangeMaxPPEM
                //'gsp9' 	gaspRange[9]            gasp.gaspRange[9].rangeMaxPPEM
                RegisterValueTagInfo("gsp0", "gaspRange[0]", "gasp.gaspRange[0].rangeMaxPPEM");
                RegisterValueTagInfo("gsp1", "gaspRange[1]", "gasp.gaspRange[1].rangeMaxPPEM");
                RegisterValueTagInfo("gsp2", "gaspRange[2]", "gasp.gaspRange[2].rangeMaxPPEM");
                RegisterValueTagInfo("gsp3", "gaspRange[3]", "gasp.gaspRange[3].rangeMaxPPEM");
                RegisterValueTagInfo("gsp4", "gaspRange[4]", "gasp.gaspRange[4].rangeMaxPPEM");
                RegisterValueTagInfo("gsp5", "gaspRange[5]", "gasp.gaspRange[5].rangeMaxPPEM");
                RegisterValueTagInfo("gsp6", "gaspRange[6]", "gasp.gaspRange[6].rangeMaxPPEM");
                RegisterValueTagInfo("gsp7", "gaspRange[7]", "gasp.gaspRange[7].rangeMaxPPEM");
                RegisterValueTagInfo("gsp8", "gaspRange[8]", "gasp.gaspRange[8].rangeMaxPPEM");
                RegisterValueTagInfo("gsp9", "gaspRange[9]", "gasp.gaspRange[9].rangeMaxPPEM");


            }







        }


    }
}