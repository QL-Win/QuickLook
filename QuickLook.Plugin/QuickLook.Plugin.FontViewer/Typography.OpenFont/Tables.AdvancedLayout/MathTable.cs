//MIT, 2018-present, WinterDev
//https://docs.microsoft.com/en-us/typography/opentype/spec/math

using System.IO;


namespace Typography.OpenFont.MathGlyphs
{

    public readonly struct MathValueRecord
    {
        //MathValueRecord
        //Type      Name            Description
        //int16     Value           The X or Y value in design units
        //Offset16  DeviceTable     Offset to the device table – from the beginning of parent table.May be NULL. Suggested format for device table is 1.
        public readonly short Value;
        public readonly ushort DeviceTable;
        public MathValueRecord(short value, ushort deviceTable)
        {
            this.Value = value;
            this.DeviceTable = deviceTable;
        }
#if DEBUG
        public override string ToString()
        {
            if (DeviceTable == 0)
            {
                return Value.ToString();
            }
            else
            {
                return Value + "," + DeviceTable;
            }

        }
#endif
    }

    public class MathConstants
    {
        //MathConstantsTable
        //When selecting names for values in the MathConstants table, the following naming convention should be used:

        //Height – Specifies a distance from the main baseline.
        //Kern – Represents a fixed amount of empty space to be introduced.
        //Gap – Represents an amount of empty space that may need to be increased to meet certain criteria.
        //Drop and Rise – Specifies the relationship between measurements of two elements to be positioned relative to each other(but not necessarily in a stack - like manner) that must meet certain criteria.For a Drop, one of the positioned elements has to be moved down to satisfy those criteria; for a Rise, the movement is upwards.
        //Shift – Defines a vertical shift applied to an element sitting on a baseline.
        //Dist – Defines a distance between baselines of two elements.

        /// <summary>
        /// Percentage of scaling down for script level 1. 
        /// Suggested value: 80%.
        /// </summary>
        public short ScriptPercentScaleDown { get; internal set; }
        /// <summary>
        /// Percentage of scaling down for script level 2 (ScriptScript).
        /// Suggested value: 60%.
        /// </summary>
        public short ScriptScriptPercentScaleDown { get; internal set; }
        /// <summary>
        /// Minimum height required for a delimited expression to be treated as a sub-formula.
        /// Suggested value: normal line height ×1.5.
        /// </summary>
        public ushort DelimitedSubFormulaMinHeight { get; internal set; }
        /// <summary>
        ///  	Minimum height of n-ary operators (such as integral and summation) for formulas in display mode.
        /// </summary>
        public ushort DisplayOperatorMinHeight { get; internal set; }


        /// <summary>
        /// White space to be left between math formulas to ensure proper line spacing. 
        /// For example, for applications that treat line gap as a part of line ascender,
        /// formulas with ink going above (os2.sTypoAscender + os2.sTypoLineGap - MathLeading) 
        /// or with ink going below os2.sTypoDescender will result in increasing line height.
        /// </summary>
        public MathValueRecord MathLeading { get; internal set; }
        /// <summary>
        /// Axis height of the font.
        /// </summary>
        public MathValueRecord AxisHeight { get; internal set; }
        /// <summary>
        /// Maximum (ink) height of accent base that does not require raising the accents.
        /// Suggested: x‑height of the font (os2.sxHeight) plus any possible overshots.
        /// </summary>
        public MathValueRecord AccentBaseHeight { get; internal set; }
        /// <summary>
        ///Maximum (ink) height of accent base that does not require flattening the accents. 
        ///Suggested: cap height of the font (os2.sCapHeight).
        /// </summary>
        public MathValueRecord FlattenedAccentBaseHeight { get; internal set; }

        //---------------------------------------------------------
        /// <summary>
        /// The standard shift down applied to subscript elements.
        /// Positive for moving in the downward direction. 
        /// Suggested: os2.ySubscriptYOffset.
        /// </summary>
        public MathValueRecord SubscriptShiftDown { get; internal set; }
        /// <summary>
        /// Maximum allowed height of the (ink) top of subscripts that does not require moving subscripts further down.
        /// Suggested: 4/5 x- height.
        /// </summary>
        public MathValueRecord SubscriptTopMax { get; internal set; }
        /// <summary>
        /// Minimum allowed drop of the baseline of subscripts relative to the (ink) bottom of the base.
        /// Checked for bases that are treated as a box or extended shape. 
        /// Positive for subscript baseline dropped below the base bottom.
        /// </summary>
        public MathValueRecord SubscriptBaselineDropMin { get; internal set; }
        /// <summary>
        /// Standard shift up applied to superscript elements. 
        /// Suggested: os2.ySuperscriptYOffset.
        /// </summary>
        public MathValueRecord SuperscriptShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift of superscripts relative to the base, in cramped style.
        /// </summary>
        public MathValueRecord SuperscriptShiftUpCramped { get; internal set; }
        /// <summary>
        /// Minimum allowed height of the (ink) bottom of superscripts that does not require moving subscripts further up. 
        /// Suggested: ¼ x-height.
        /// </summary>
        public MathValueRecord SuperscriptBottomMin { get; internal set; }
        /// <summary>
        ///  Maximum allowed drop of the baseline of superscripts relative to the (ink) top of the base. Checked for bases that are treated as a box or extended shape. 
        ///  Positive for superscript baseline below the base top.
        /// </summary>
        public MathValueRecord SuperscriptBaselineDropMax { get; internal set; }
        /// <summary>
        /// Minimum gap between the superscript and subscript ink. 
        /// Suggested: 4×default rule thickness.
        /// </summary>
        public MathValueRecord SubSuperscriptGapMin { get; internal set; }
        /// <summary>
        /// The maximum level to which the (ink) bottom of superscript can be pushed to increase the gap between 
        /// superscript and subscript, before subscript starts being moved down. 
        /// Suggested: 4/5 x-height.
        /// </summary>
        public MathValueRecord SuperscriptBottomMaxWithSubscript { get; internal set; }
        /// <summary>
        /// Extra white space to be added after each subscript and superscript. Suggested: 0.5pt for a 12 pt font.
        /// </summary>
        public MathValueRecord SpaceAfterScript { get; internal set; }

        //---------------------------------------------------------
        /// <summary>
        /// Minimum gap between the (ink) bottom of the upper limit, and the (ink) top of the base operator.
        /// </summary>
        public MathValueRecord UpperLimitGapMin { get; internal set; }
        /// <summary>
        /// Minimum distance between baseline of upper limit and (ink) top of the base operator.
        /// </summary>
        public MathValueRecord UpperLimitBaselineRiseMin { get; internal set; }
        /// <summary>
        /// Minimum gap between (ink) top of the lower limit, and (ink) bottom of the base operator.
        /// </summary>
        public MathValueRecord LowerLimitGapMin { get; internal set; }
        /// <summary>
        /// Minimum distance between baseline of the lower limit and (ink) bottom of the base operator.
        /// </summary>
        public MathValueRecord LowerLimitBaselineDropMin { get; internal set; }

        //---------------------------------------------------------
        /// <summary>
        /// Standard shift up applied to the top element of a stack.
        /// </summary>
        public MathValueRecord StackTopShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift up applied to the top element of a stack in display style.
        /// </summary>
        public MathValueRecord StackTopDisplayStyleShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift down applied to the bottom element of a stack. 
        /// Positive for moving in the downward direction.
        /// </summary>
        public MathValueRecord StackBottomShiftDown { get; internal set; }
        /// <summary>
        /// Standard shift down applied to the bottom element of a stack in display style.
        /// Positive for moving in the downward direction.
        /// </summary>
        public MathValueRecord StackBottomDisplayStyleShiftDown { get; internal set; }
        /// <summary>
        /// Minimum gap between (ink) bottom of the top element of a stack, and the (ink) top of the bottom element.
        /// Suggested: 3×default rule thickness.
        /// </summary>
        public MathValueRecord StackGapMin { get; internal set; }
        /// <summary>
        /// Minimum gap between (ink) bottom of the top element of a stack, and the (ink) top of the bottom element in display style.
        /// Suggested: 7×default rule thickness.
        /// </summary>
        public MathValueRecord StackDisplayStyleGapMin { get; internal set; }

        /// <summary>
        /// Standard shift up applied to the top element of the stretch stack.
        /// </summary>
        public MathValueRecord StretchStackTopShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift down applied to the bottom element of the stretch stack.
        /// Positive for moving in the downward direction.
        /// </summary>         
        public MathValueRecord StretchStackBottomShiftDown { get; internal set; }
        /// <summary>
        /// Minimum gap between the ink of the stretched element, and the (ink) bottom of the element above. 
        /// Suggested: UpperLimitGapMin.
        /// </summary>
        public MathValueRecord StretchStackGapAboveMin { get; internal set; }
        /// <summary>
        /// Minimum gap between the ink of the stretched element, and the (ink) top of the element below. 
        /// Suggested: LowerLimitGapMin.
        /// </summary>
        public MathValueRecord StretchStackGapBelowMin { get; internal set; }



        //---------------------------------------------------------
        /// <summary>
        /// Standard shift up applied to the numerator.
        /// </summary>
        public MathValueRecord FractionNumeratorShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift up applied to the numerator in display style. Suggested: StackTopDisplayStyleShiftUp.
        /// </summary>
        public MathValueRecord FractionNumeratorDisplayStyleShiftUp { get; internal set; }
        /// <summary>
        /// Standard shift down applied to the denominator. Positive for moving in the downward direction.
        /// </summary>
        public MathValueRecord FractionDenominatorShiftDown { get; internal set; }
        /// <summary>
        /// Standard shift down applied to the denominator in display style. Positive for moving in the downward direction. 
        /// Suggested: StackBottomDisplayStyleShiftDown
        /// </summary>
        public MathValueRecord FractionDenominatorDisplayStyleShiftDown { get; internal set; }
        /// <summary>
        ///  Minimum tolerated gap between the (ink) bottom of the numerator and the ink of the fraction bar. 
        ///  Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord FractionNumeratorGapMin { get; internal set; }
        /// <summary>
        /// Minimum tolerated gap between the (ink) bottom of the numerator and the ink of the fraction bar in display style. 
        /// Suggested: 3×default rule thickness
        /// </summary>
        public MathValueRecord FractionNumDisplayStyleGapMin { get; internal set; }
        /// <summary>
        /// Thickness of the fraction bar. 
        /// Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord FractionRuleThickness { get; internal set; }
        /// <summary>
        ///  Minimum tolerated gap between the (ink) top of the denominator and the ink of the fraction bar.
        ///  Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord FractionDenominatorGapMin { get; internal set; }
        /// <summary>
        /// Minimum tolerated gap between the (ink) top of the denominator and the ink of the fraction bar in display style. 
        /// Suggested: 3×default rule thickness
        /// </summary>
        public MathValueRecord FractionDenomDisplayStyleGapMin { get; internal set; }



        //---------------------------------------------------------
        /// <summary>
        /// Horizontal distance between the top and bottom elements of a skewed fraction.
        /// </summary>
        public MathValueRecord SkewedFractionHorizontalGap { get; internal set; }
        /// <summary>
        /// Vertical distance between the ink of the top and bottom elements of a skewed fraction
        /// </summary>
        public MathValueRecord SkewedFractionVerticalGap { get; internal set; }



        //---------------------------------------------------------
        /// <summary>
        /// Distance between the overbar and the (ink) top of he base.
        /// Suggested: 3×default rule thickness.
        /// </summary>
        public MathValueRecord OverbarVerticalGap { get; internal set; }
        /// <summary>
        /// Thickness of overbar. 
        /// Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord OverbarRuleThickness { get; internal set; }
        /// <summary>
        /// Extra white space reserved above the overbar. 
        /// Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord OverbarExtraAscender { get; internal set; }



        //---------------------------------------------------------
        /// <summary>
        /// Distance between underbar and (ink) bottom of the base. 
        /// Suggested: 3×default rule thickness.
        /// </summary>
        public MathValueRecord UnderbarVerticalGap { get; internal set; }
        /// <summary>
        /// Thickness of underbar. 
        /// Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord UnderbarRuleThickness { get; internal set; }
        /// <summary>
        /// Extra white space reserved below the underbar. Always positive. 
        /// Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord UnderbarExtraDescender { get; internal set; }



        //---------------------------------------------------------
        /// <summary>
        /// Space between the (ink) top of the expression and the bar over it. 
        /// Suggested: 1¼ default rule thickness.
        /// </summary>
        public MathValueRecord RadicalVerticalGap { get; internal set; }
        /// <summary>
        ///  Space between the (ink) top of the expression and the bar over it. 
        ///  Suggested: default rule thickness + ¼ x-height.
        /// </summary>
        public MathValueRecord RadicalDisplayStyleVerticalGap { get; internal set; }
        /// <summary>
        ///  Thickness of the radical rule. This is the thickness of the rule in designed or constructed radical signs. 
        ///  Suggested: default rule thickness.
        /// </summary>
        public MathValueRecord RadicalRuleThickness { get; internal set; }
        /// <summary>
        /// Extra white space reserved above the radical.
        /// Suggested: RadicalRuleThickness.
        /// </summary>
        public MathValueRecord RadicalExtraAscender { get; internal set; }
        /// <summary>
        /// Extra horizontal kern before the degree of a radical, if such is present.
        /// </summary>
        public MathValueRecord RadicalKernBeforeDegree { get; internal set; }
        /// <summary>
        /// Negative kern after the degree of a radical, if such is present. 
        /// Suggested: −10/18 of em
        /// </summary>
        public MathValueRecord RadicalKernAfterDegree { get; internal set; }
        /// <summary>
        ///  Height of the bottom of the radical degree, 
        ///  if such is present, in proportion to the ascender of the radical sign. 
        ///  Suggested: 60%.
        /// </summary>
        public short RadicalDegreeBottomRaisePercent { get; internal set; }


        //---------------------------------------------------------
        //ONLY this value come from  "MathVariants" *** 
        //I expose that value on this class
        public ushort MinConnectorOverlap { get; internal set; }
        //---------------------------------------------------------
    }

    public class MathGlyphInfo
    {
        public readonly ushort GlyphIndex;
        public MathGlyphInfo(ushort glyphIndex)
        {
            this.GlyphIndex = glyphIndex;
        }

        public MathValueRecord? ItalicCorrection { get; internal set; }
        public MathValueRecord? TopAccentAttachment { get; internal set; }
        public bool IsShapeExtensible { get; internal set; }

        //optional 
        public MathKern TopLeftMathKern => _mathKernRec.TopLeft;
        public MathKern TopRightMathKern => _mathKernRec.TopRight;
        public MathKern BottomLeftMathKern => _mathKernRec.BottomLeft;
        public MathKern BottomRightMathKern => _mathKernRec.BottomRight;
        public bool HasSomeMathKern { get; private set; }

        //
        MathKernInfoRecord _mathKernRec;
        internal void SetMathKerns(MathKernInfoRecord mathKernRec)
        {
            _mathKernRec = mathKernRec;
            HasSomeMathKern = true;
        }

        /// <summary>
        /// vertical glyph construction
        /// </summary>
        public MathGlyphConstruction VertGlyphConstruction;
        /// <summary>
        /// horizontal glyph construction
        /// </summary>
        public MathGlyphConstruction HoriGlyphConstruction;

    }
    public class MathGlyphConstruction
    {
        public MathValueRecord GlyphAsm_ItalicCorrection;
        public GlyphPartRecord[] GlyphAsm_GlyphPartRecords;
        public MathGlyphVariantRecord[] glyphVariantRecords;
    }


    public readonly struct GlyphPartRecord
    {
        //Thus, a GlyphPartRecord consists of the following fields: 
        //1) Glyph ID for the part.
        //2) Lengths of the connectors on each end of the glyph. 
        //      The connectors are straight parts of the glyph that can be used to link it with the next or previous part.
        //      The connectors of neighboring parts can overlap, which provides flexibility of how these glyphs can be put together.However, the overlap should not be less than the value of MinConnectorOverlap defined in the MathVariants tables, and it should not exceed the length of either of two overlapping connectors.If the part does not have a connector on one of its sides, the corresponding length should be set to zero.

        //3) The full advance of the part. 
        //      It is also used to determine the measurement of the result by using the following formula:

        //  *** Size of Assembly = Offset of the Last Part + Full Advance of the Last Part ***

        //4) PartFlags is the last field.
        //      It identifies a number of parts as extenders – those parts that can be repeated(that is, multiple instances of them can be used in place of one) or skipped altogether.Usually the extenders are vertical or horizontal bars of the appropriate thickness, aligned with the rest of the assembly.

        //To ensure that the width/height is distributed equally and the symmetry of the shape is preserved,
        //following steps can be used by math handling client.

        //1. Assemble all parts by overlapping connectors by maximum amount, and removing all extenders.
        //  This gives the smallest possible result.

        //2. Determine how much extra width/height can be distributed into all connections between neighboring parts.
        //   If that is enough to achieve the size goal, extend each connection equally by changing overlaps of connectors to finish the job.
        //3. If all connections have been extended to minimum overlap and further growth is needed, add one of each extender, 
        //and repeat the process from the first step.

        //Note that for assemblies growing in vertical direction,
        //the distribution of height or the result between ascent and descent is not defined.
        //The math handling client is responsible for positioning the resulting assembly relative to the baseline.


        //GlyphPartRecord Table
        //Type      Name                    Description
        //uint16    Glyph                   Glyph ID for the part.
        //uint16    StartConnectorLength    Advance width/ height of the straight bar connector material, in design units, is at the beginning of the glyph, in the direction of the extension.
        //uint16    EndConnectorLength      Advance width/ height of the straight bar connector material, in design units, is at the end of the glyph, in the direction of the extension.
        //uint16    FullAdvance             Full advance width/height for this part, in the direction of the extension.In design units.
        //uint16    PartFlags               Part qualifiers. PartFlags enumeration currently uses only one bit:
        //                                       0x0001 fExtender If set, the part can be skipped or repeated.
        //                                       0xFFFE Reserved.

        public readonly ushort GlyphId;
        public readonly ushort StartConnectorLength;
        public readonly ushort EndConnectorLength;
        public readonly ushort FullAdvance;
        public readonly ushort PartFlags;
        public bool IsExtender => (PartFlags & 0x0001) != 0;

        public GlyphPartRecord(ushort glyphId, ushort startConnectorLength, ushort endConnectorLength, ushort fullAdvance, ushort partFlags)
        {
            GlyphId = glyphId;
            StartConnectorLength = startConnectorLength;
            EndConnectorLength = endConnectorLength;
            FullAdvance = fullAdvance;
            PartFlags = partFlags;
        }
#if DEBUG
        public override string ToString()
        {
            return "glyph_id:" + GlyphId;
        }
#endif
    }


    public readonly struct MathGlyphVariantRecord
    {
        //    MathGlyphVariantRecord Table
        //Type      Name                Description
        //uint16    VariantGlyph        Glyph ID for the variant.
        //uint16    AdvanceMeasurement  Advance width/height, in design units, of the variant, in the direction of requested glyph extension.
        public readonly ushort VariantGlyph;
        public readonly ushort AdvanceMeasurement;
        public MathGlyphVariantRecord(ushort variantGlyph, ushort advanceMeasurement)
        {
            this.VariantGlyph = variantGlyph;
            this.AdvanceMeasurement = advanceMeasurement;
        }

#if DEBUG
        public override string ToString()
        {
            return "variant_glyph_id:" + VariantGlyph + ", adv:" + AdvanceMeasurement;
        }
#endif
    }

    public class MathKern
    {
        //reference =>see  MathKernTable
        public ushort HeightCount;
        public MathValueRecord[] CorrectionHeights;
        public MathValueRecord[] KernValues;

        public MathKern(ushort heightCount, MathValueRecord[] correctionHeights, MathValueRecord[] kernValues)
        {
            HeightCount = heightCount;
            CorrectionHeights = correctionHeights;
            KernValues = kernValues;
        }

#if DEBUG
        public override string ToString()
        {
            return HeightCount.ToString();
        }
#endif
    }

    readonly struct MathKernInfoRecord
    {
        //resolved value
        public readonly MathKern TopRight;
        public readonly MathKern TopLeft;
        public readonly MathKern BottomRight;
        public readonly MathKern BottomLeft;
        public MathKernInfoRecord(MathKern topRight,
             MathKern topLeft,
             MathKern bottomRight,
             MathKern bottomLeft)
        {
            TopRight = topLeft;
            TopLeft = topLeft;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }
    }
}

namespace Typography.OpenFont.Tables
{
    using Typography.OpenFont.MathGlyphs;

    static class MathValueRecordReaderHelper
    {
        public static MathValueRecord ReadMathValueRecord(this BinaryReader reader)
        {
            return new MathValueRecord(reader.ReadInt16(), reader.ReadUInt16());
        }

        public static MathValueRecord[] ReadMathValueRecords(this BinaryReader reader, int count)
        {
            MathValueRecord[] records = new MathValueRecord[count];
            for (int i = 0; i < count; ++i)
            {
                records[i] = reader.ReadMathValueRecord();
            }
            return records;
        }
    }

    readonly struct MathGlyphLoader
    {

        static MathGlyphInfo GetMathGlyphOrCreateNew(MathGlyphInfo[] mathGlyphInfos, ushort glyphIndex)
        {
            return mathGlyphInfos[glyphIndex] ?? (mathGlyphInfos[glyphIndex] = new MathGlyphInfo(glyphIndex));
        }

        public static void LoadMathGlyph(Typeface typeface, MathTable mathTable)
        {
            //expand math info to each glyph in typeface

            typeface._mathTable = mathTable;


            //expand all information to the glyph  
            int glyphCount = typeface.GlyphCount;
            MathGlyphInfo[] mathGlyphInfos = new MathGlyphInfo[glyphCount];


            int index = 0;
            //-----------------
            //2. MathGlyphInfo
            //-----------------
            {    //2.1 expand italic correction
                MathItalicsCorrectonInfoTable italicCorrection = mathTable._mathItalicCorrectionInfo;
                index = 0; //reset
                if (italicCorrection.CoverageTable != null)
                {
                    foreach (ushort glyphIndex in italicCorrection.CoverageTable.GetExpandedValueIter())
                    {
                        GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).ItalicCorrection = italicCorrection.ItalicCorrections[index];
                        index++;
                    }
                }
            }
            //--------
            {
                //2.2 expand top accent
                MathTopAccentAttachmentTable topAccentAttachment = mathTable._mathTopAccentAttachmentTable;
                index = 0; //reset
                if (topAccentAttachment.CoverageTable != null)
                {
                    foreach (ushort glyphIndex in topAccentAttachment.CoverageTable.GetExpandedValueIter())
                    {
                        GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).TopAccentAttachment = topAccentAttachment.TopAccentAttachment[index];
                        index++;
                    }
                }
            }
            //--------
            {
                //2.3 expand , expand shape
                index = 0; //reset
                if (mathTable._extendedShapeCoverageTable != null)
                {
                    foreach (ushort glyphIndex in mathTable._extendedShapeCoverageTable.GetExpandedValueIter())
                    {
                        GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).IsShapeExtensible = true;
                        index++;
                    }
                }
            }
            //--------
            {
                //2.4 math kern
                index = 0; //reset
                if (mathTable._mathKernInfoCoverage != null)
                {
                    MathKernInfoRecord[] kernRecs = mathTable._mathKernInfoRecords;
                    foreach (ushort glyphIndex in mathTable._mathKernInfoCoverage.GetExpandedValueIter())
                    {
                        GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).SetMathKerns(kernRecs[index]);
                        index++;
                    }
                }
            }
            //-----------------
            //3. MathVariants
            //-----------------
            {

                MathVariantsTable mathVariants = mathTable._mathVariantsTable;

                //3.1  vertical
                index = 0; //reset
                foreach (ushort glyphIndex in mathVariants.vertCoverage.GetExpandedValueIter())
                {
                    GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).VertGlyphConstruction = mathVariants.vertConstructionTables[index];
                    index++;
                }
                //
                //3.2 horizontal
                index = 0;//reset
                if (mathVariants.horizCoverage != null)
                {
                    foreach (ushort glyphIndex in mathVariants.horizCoverage.GetExpandedValueIter())
                    {
                        GetMathGlyphOrCreateNew(mathGlyphInfos, glyphIndex).HoriGlyphConstruction = mathVariants.horizConstructionTables[index];
                        index++;
                    }
                }
            }
            typeface.LoadMathGlyphInfos(mathGlyphInfos);
        }

    }



    class MathTable : TableEntry
    {
        public const string _N = "MATH";
        public override string Name => _N;
        //
        internal MathConstants _mathConstTable;

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //eg. latin-modern-math-regular.otf, asana-math.otf

            long beginAt = reader.BaseStream.Position;
            //math table header
            //Type          Name    Description
            //uint16        MajorVersion Major version of the MATH table, = 1.
            //uint16        MinorVersion    Minor version of the MATH table, = 0.
            //Offset16      MathConstants   Offset to MathConstants table -from the beginning of MATH table.
            //Offset16      MathGlyphInfo   Offset to MathGlyphInfo table -from the beginning of MATH table.
            //Offset16      MathVariants    Offset to MathVariants table -from the beginning of MATH table.

            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort mathConstants_offset = reader.ReadUInt16();
            ushort mathGlyphInfo_offset = reader.ReadUInt16();
            ushort mathVariants_offset = reader.ReadUInt16();
            //---------------------------------

            //(1)
            reader.BaseStream.Position = beginAt + mathConstants_offset;
            ReadMathConstantsTable(reader);
            //
            //(2)
            reader.BaseStream.Position = beginAt + mathGlyphInfo_offset;
            ReadMathGlyphInfoTable(reader);
            //
            //(3)
            reader.BaseStream.Position = beginAt + mathVariants_offset;
            ReadMathVariantsTable(reader);

            //NOTE: expose  MinConnectorOverlap via _mathConstTable
            _mathConstTable.MinConnectorOverlap = _mathVariantsTable.MinConnectorOverlap;
        }
        /// <summary>
        /// (1) MathConstants
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathConstantsTable(BinaryReader reader)
        {
            //MathConstants Table

            //The MathConstants table defines miscellaneous constants required to properly position elements of mathematical formulas.
            //These constants belong to several groups of semantically related values such as values needed to properly position accents,
            //values for positioning superscripts and subscripts, and values for positioning elements of fractions.
            //The table also contains general use constants that may affect all parts of the formula,
            //such as axis height and math leading.Note that most of the constants deal with the vertical positioning.

            MathConstants mc = new MathConstants();
            mc.ScriptPercentScaleDown = reader.ReadInt16();
            mc.ScriptScriptPercentScaleDown = reader.ReadInt16();
            mc.DelimitedSubFormulaMinHeight = reader.ReadUInt16();
            mc.DisplayOperatorMinHeight = reader.ReadUInt16();
            //
            //            

            mc.MathLeading = reader.ReadMathValueRecord();
            mc.AxisHeight = reader.ReadMathValueRecord();
            mc.AccentBaseHeight = reader.ReadMathValueRecord();
            mc.FlattenedAccentBaseHeight = reader.ReadMathValueRecord();

            // 
            mc.SubscriptShiftDown = reader.ReadMathValueRecord();
            mc.SubscriptTopMax = reader.ReadMathValueRecord();
            mc.SubscriptBaselineDropMin = reader.ReadMathValueRecord();

            //
            mc.SuperscriptShiftUp = reader.ReadMathValueRecord();
            mc.SuperscriptShiftUpCramped = reader.ReadMathValueRecord();
            mc.SuperscriptBottomMin = reader.ReadMathValueRecord();
            mc.SuperscriptBaselineDropMax = reader.ReadMathValueRecord();
            //
            mc.SubSuperscriptGapMin = reader.ReadMathValueRecord();
            mc.SuperscriptBottomMaxWithSubscript = reader.ReadMathValueRecord();
            mc.SpaceAfterScript = reader.ReadMathValueRecord();

            //
            mc.UpperLimitGapMin = reader.ReadMathValueRecord();
            mc.UpperLimitBaselineRiseMin = reader.ReadMathValueRecord();
            mc.LowerLimitGapMin = reader.ReadMathValueRecord();
            mc.LowerLimitBaselineDropMin = reader.ReadMathValueRecord();

            // 
            mc.StackTopShiftUp = reader.ReadMathValueRecord();
            mc.StackTopDisplayStyleShiftUp = reader.ReadMathValueRecord();
            mc.StackBottomShiftDown = reader.ReadMathValueRecord();
            mc.StackBottomDisplayStyleShiftDown = reader.ReadMathValueRecord();
            mc.StackGapMin = reader.ReadMathValueRecord();
            mc.StackDisplayStyleGapMin = reader.ReadMathValueRecord();

            //
            mc.StretchStackTopShiftUp = reader.ReadMathValueRecord();
            mc.StretchStackBottomShiftDown = reader.ReadMathValueRecord();
            mc.StretchStackGapAboveMin = reader.ReadMathValueRecord();
            mc.StretchStackGapBelowMin = reader.ReadMathValueRecord();

            // 
            mc.FractionNumeratorShiftUp = reader.ReadMathValueRecord();
            mc.FractionNumeratorDisplayStyleShiftUp = reader.ReadMathValueRecord();
            mc.FractionDenominatorShiftDown = reader.ReadMathValueRecord();
            mc.FractionDenominatorDisplayStyleShiftDown = reader.ReadMathValueRecord();
            mc.FractionNumeratorGapMin = reader.ReadMathValueRecord();
            mc.FractionNumDisplayStyleGapMin = reader.ReadMathValueRecord();
            mc.FractionRuleThickness = reader.ReadMathValueRecord();
            mc.FractionDenominatorGapMin = reader.ReadMathValueRecord();
            mc.FractionDenomDisplayStyleGapMin = reader.ReadMathValueRecord();

            // 
            mc.SkewedFractionHorizontalGap = reader.ReadMathValueRecord();
            mc.SkewedFractionVerticalGap = reader.ReadMathValueRecord();

            //
            mc.OverbarVerticalGap = reader.ReadMathValueRecord();
            mc.OverbarRuleThickness = reader.ReadMathValueRecord();
            mc.OverbarExtraAscender = reader.ReadMathValueRecord();

            //
            mc.UnderbarVerticalGap = reader.ReadMathValueRecord();
            mc.UnderbarRuleThickness = reader.ReadMathValueRecord();
            mc.UnderbarExtraDescender = reader.ReadMathValueRecord();

            //
            mc.RadicalVerticalGap = reader.ReadMathValueRecord();
            mc.RadicalDisplayStyleVerticalGap = reader.ReadMathValueRecord();
            mc.RadicalRuleThickness = reader.ReadMathValueRecord();
            mc.RadicalExtraAscender = reader.ReadMathValueRecord();
            mc.RadicalKernBeforeDegree = reader.ReadMathValueRecord();
            mc.RadicalKernAfterDegree = reader.ReadMathValueRecord();
            mc.RadicalDegreeBottomRaisePercent = reader.ReadInt16();


            _mathConstTable = mc;
        }


        //--------------------------------------------------------------------------

        /// <summary>
        /// (2) MathGlyphInfo
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathGlyphInfoTable(BinaryReader reader)
        {

            //MathGlyphInfo Table
            //  The MathGlyphInfo table contains positioning information that is defined on per - glyph basis.
            //  The table consists of the following parts:
            //    Offset to MathItalicsCorrectionInfo table that contains information on italics correction values.
            //    Offset to MathTopAccentAttachment table that contains horizontal positions for attaching mathematical accents.
            //    Offset to Extended Shape coverage table.The glyphs covered by this table are to be considered extended shapes.
            //    Offset to MathKernInfo table that provides per - glyph information for mathematical kerning.


            //  NOTE: Here, and elsewhere in the subclause – please refer to subclause 6.2.4 "Features and Lookups" for description of the coverage table formats.

            long startAt = reader.BaseStream.Position;
            ushort offsetTo_MathItalicsCorrectionInfo_Table = reader.ReadUInt16();
            ushort offsetTo_MathTopAccentAttachment_Table = reader.ReadUInt16();
            ushort offsetTo_Extended_Shape_coverage_Table = reader.ReadUInt16();
            ushort offsetTo_MathKernInfo_Table = reader.ReadUInt16();
            //-----------------------

            //(2.1)
            reader.BaseStream.Position = startAt + offsetTo_MathItalicsCorrectionInfo_Table;
            ReadMathItalicCorrectionInfoTable(reader);

            //(2.2)
            reader.BaseStream.Position = startAt + offsetTo_MathTopAccentAttachment_Table;
            ReadMathTopAccentAttachment(reader);
            //


            //TODO:...
            //The glyphs covered by this table are to be considered extended shapes.
            //These glyphs are variants extended in the vertical direction, e.g.,
            //to match height of another part of the formula.
            //Because their dimensions may be very large in comparison with normal glyphs in the glyph set,
            //the standard positioning algorithms will not produce the best results when applied to them.
            //In the vertical direction, other formula elements will be positioned not relative to those glyphs,
            //but instead to the ink box of the subexpression containing them

            //.... 

            //(2.3)
            if (offsetTo_Extended_Shape_coverage_Table > 0)
            {
                //may be null, eg. found in font Linux Libertine Regular (https://sourceforge.net/projects/linuxlibertine/)
                _extendedShapeCoverageTable = CoverageTable.CreateFrom(reader, startAt + offsetTo_Extended_Shape_coverage_Table);
            }

            //(2.4)
            if (offsetTo_MathKernInfo_Table > 0)
            {
                //may be null, eg. latin-modern-math.otf => not found
                //we found this in Asana-math-regular
                reader.BaseStream.Position = startAt + offsetTo_MathKernInfo_Table;
                ReadMathKernInfoTable(reader);
            }
        }


        /// <summary>
        /// (2.1)
        /// </summary>
        internal MathItalicsCorrectonInfoTable _mathItalicCorrectionInfo;
        /// <summary>
        /// (2.1)
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathItalicCorrectionInfoTable(BinaryReader reader)
        {
            long beginAt = reader.BaseStream.Position;
            _mathItalicCorrectionInfo = new MathItalicsCorrectonInfoTable();
            //MathItalicsCorrectionInfo Table
            //Type           Name                           Description
            //Offset16       Coverage                       Offset to Coverage table - from the beginning of MathItalicsCorrectionInfo table.
            //uint16         ItalicsCorrectionCount         Number of italics correction values.Should coincide with the number of covered glyphs.
            //MathValueRecord ItalicsCorrection[ItalicsCorrectionCount]  Array of MathValueRecords defining italics correction values for each covered glyph. 
            ushort coverageOffset = reader.ReadUInt16();
            ushort italicCorrectionCount = reader.ReadUInt16();
            _mathItalicCorrectionInfo.ItalicCorrections = reader.ReadMathValueRecords(italicCorrectionCount);
            //read coverage ...
            if (coverageOffset > 0)
            {
                //may be null?, eg. found in font Linux Libertine Regular (https://sourceforge.net/projects/linuxlibertine/)
                _mathItalicCorrectionInfo.CoverageTable = CoverageTable.CreateFrom(reader, beginAt + coverageOffset);
            }
        }


        /// <summary>
        /// (2.2)
        /// </summary>
        internal MathTopAccentAttachmentTable _mathTopAccentAttachmentTable;
        /// <summary>
        /// (2.2)
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathTopAccentAttachment(BinaryReader reader)
        {
            //MathTopAccentAttachment Table

            //The MathTopAccentAttachment table contains information on horizontal positioning of top math accents. 
            //The table consists of the following parts:

            //Coverage of glyphs for which information on horizontal positioning of math accents is provided.
            //To position accents over any other glyph, its geometrical center(with respect to advance width) can be used.

            //Count of covered glyphs.

            //Array of top accent attachment points for each covered glyph, in order of coverage.
            //These attachment points are to be used for finding horizontal positions of accents over characters.
            //It is done by aligning the attachment point of the base glyph with the attachment point of the accent.
            //Note that this is very similar to mark-to-base attachment, but here alignment only happens in the horizontal direction, 
            //and the vertical positions of accents are determined by different means.

            //MathTopAccentAttachment Table
            //Type          Name                        Description
            //Offset16      TopAccentCoverage           Offset to Coverage table - from the beginning of MathTopAccentAttachment table.
            //uint16        TopAccentAttachmentCount    Number of top accent attachment point values.Should coincide with the number of covered glyphs.
            //MathValueRecord TopAccentAttachment[TopAccentAttachmentCount]  Array of MathValueRecords defining top accent attachment points for each covered glyph.


            long beginAt = reader.BaseStream.Position;
            _mathTopAccentAttachmentTable = new MathTopAccentAttachmentTable();

            ushort coverageOffset = reader.ReadUInt16();
            ushort topAccentAttachmentCount = reader.ReadUInt16();
            _mathTopAccentAttachmentTable.TopAccentAttachment = reader.ReadMathValueRecords(topAccentAttachmentCount);
            if (coverageOffset > 0)
            {
                //may be null?, eg. found in font Linux Libertine Regular (https://sourceforge.net/projects/linuxlibertine/)
                _mathTopAccentAttachmentTable.CoverageTable = CoverageTable.CreateFrom(reader, beginAt + coverageOffset);
            }

        }

        /// <summary>
        /// (2.3)
        /// </summary>
        internal CoverageTable _extendedShapeCoverageTable;


        /// <summary>
        /// (2.4)
        /// </summary>
        internal CoverageTable _mathKernInfoCoverage;
        /// <summary>
        /// (2.4)
        /// </summary>
        internal MathKernInfoRecord[] _mathKernInfoRecords;
        /// <summary>
        /// (2.4)
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathKernInfoTable(BinaryReader reader)
        {
            // MathKernInfo Table

            //The MathKernInfo table provides information on glyphs for which mathematical (height - dependent) kerning values are defined.
            //It consists of the following fields:

            //    Coverage of glyphs for which mathematical kerning information is provided.
            //    Count of MathKernInfoRecords.Should coincide with the number of glyphs in Coverage table.
            //    Array of MathKernInfoRecords for each covered glyph, in order of coverage.

            //MathKernInfo Table
            //Type          Name                Description
            //Offset16      MathKernCoverage    Offset to Coverage table - from the beginning of the MathKernInfo table.
            //uint16        MathKernCount       Number of MathKernInfoRecords.
            //MathKernInfoRecord MathKernInfoRecords[MathKernCount]     Array of MathKernInfoRecords, per - glyph information for mathematical positioning of subscripts and superscripts.

            //...
            //Each MathKernInfoRecord points to up to four kern tables for each of the corners around the glyph.

            long beginAt = reader.BaseStream.Position;

            ushort mathKernCoverage_offset = reader.ReadUInt16();
            ushort mathKernCount = reader.ReadUInt16();


            //MathKernInfoRecord Table 
            //Each MathKernInfoRecord points to up to four kern tables for each of the corners around the glyph.

            //    //MathKernInfoRecord Table
            //    //Type      Name                Description
            //    //Offset16  TopRightMathKern    Offset to MathKern table for top right corner - from the beginning of MathKernInfo table.May be NULL.
            //    //Offset16  TopLeftMathKern     Offset to MathKern table for the top left corner - from the beginning of MathKernInfo table. May be NULL.
            //    //Offset16  BottomRightMathKern Offset to MathKern table for bottom right corner - from the beginning of MathKernInfo table. May be NULL.
            //    //Offset16  BottomLeftMathKern  Offset to MathKern table for bottom left corner - from the beginning of MathKernInfo table. May be NULL.

            ushort[] allKernRecOffset = Utils.ReadUInt16Array(reader, 4 * mathKernCount);//*** 

            //read each kern table  
            _mathKernInfoRecords = new MathKernInfoRecord[mathKernCount];
            int index = 0;
            ushort m_kern_offset = 0;

            for (int i = 0; i < mathKernCount; ++i)
            {

                //top-right
                m_kern_offset = allKernRecOffset[index];

                MathKern topRight = null, topLeft = null, bottomRight = null, bottomLeft = null;

                if (m_kern_offset > 0)
                {
                    reader.BaseStream.Position = beginAt + m_kern_offset;
                    topRight = ReadMathKernTable(reader);
                }
                //top-left
                m_kern_offset = allKernRecOffset[index + 1];
                if (m_kern_offset > 0)
                {
                    reader.BaseStream.Position = beginAt + m_kern_offset;
                    topLeft = ReadMathKernTable(reader);
                }
                //bottom-right
                m_kern_offset = allKernRecOffset[index + 2];
                if (m_kern_offset > 0)
                {
                    reader.BaseStream.Position = beginAt + m_kern_offset;
                    bottomRight = ReadMathKernTable(reader);
                }
                //bottom-left
                m_kern_offset = allKernRecOffset[index + 3];
                if (m_kern_offset > 0)
                {
                    reader.BaseStream.Position = beginAt + m_kern_offset;
                    bottomLeft = ReadMathKernTable(reader);
                }

                _mathKernInfoRecords[i] = new MathKernInfoRecord(topRight, topLeft, bottomRight, bottomLeft);

                index += 4;//***
            }

            //-----
            _mathKernInfoCoverage = CoverageTable.CreateFrom(reader, beginAt + mathKernCoverage_offset);

        }
        /// <summary>
        /// (2.4)
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        static MathKern ReadMathKernTable(BinaryReader reader)
        {

            //The MathKern table contains adjustments to horizontal positions of subscripts and superscripts
            //The kerning algorithm consists of the following steps:

            //1. Calculate vertical positions of subscripts and superscripts.
            //2. Set the default horizontal position for the subscript immediately after the base glyph.
            //3. Set the default horizontal position for the superscript as shifted relative to the position of the subscript by the italics correction of the base glyph.
            //4. Based on the vertical positions, calculate the height of the top/ bottom for the bounding boxes of sub/superscript relative to the base glyph, and the height of the top/ bottom of the base relative to the super/ subscript.These will be the correction heights.
            //5. Get the kern values corresponding to these correction heights for the appropriate corner of the base glyph and sub/superscript glyph from the appropriate MathKern tables.Kern the default horizontal positions by the minimum of sums of those values at the correction heights for the base and for the sub/superscript.
            //6. If either one of the base or superscript expression has to be treated as a box not providing glyph
            //MathKern Table
            //Type              Name                                Description
            //uint16            HeightCount                         Number of heights on which the kern value changes.
            //MathValueRecord   CorrectionHeight[HeightCount]       Array of correction heights at which the kern value changes.Sorted by the height value in design units.
            //MathValueRecord   KernValue[HeightCount+1]            Array of kern values corresponding to heights.

            //First value is the kern value for all heights less or equal than the first height in this table.
            //Last value is the value to be applied for all heights greater than the last height in this table.
            //Negative values are interpreted as "move glyphs closer to each other".

            ushort heightCount = reader.ReadUInt16();
            return new MathKern(heightCount,
                reader.ReadMathValueRecords(heightCount),
                reader.ReadMathValueRecords(heightCount + 1)
                );
        }


        //--------------------------------------------------------------------------

        /// <summary>
        /// (3)
        /// </summary>
        internal MathVariantsTable _mathVariantsTable;
        /// <summary>
        /// (3) MathVariants
        /// </summary>
        /// <param name="reader"></param>
        void ReadMathVariantsTable(BinaryReader reader)
        {
            //MathVariants Table

            //The MathVariants table solves the following problem:
            //given a particular default glyph shape and a certain width or height, 
            //find a variant shape glyph(or construct created by putting several glyph together) 
            //that has the required measurement.
            //This functionality is needed for growing the parentheses to match the height of the expression within,
            //growing the radical sign to match the height of the expression under the radical, 
            //stretching accents like tilde when they are put over several characters, 
            //for stretching arrows, horizontal curly braces, and so forth.

            //The MathVariants table consists of the following fields:


            //  Count and coverage of glyph that can grow in the vertical direction.
            //  Count and coverage of glyphs that can grow in the horizontal direction.
            //  MinConnectorOverlap defines by how much two glyphs need to overlap with each other when used to construct a larger shape. 
            //  Each glyph to be used as a building block in constructing extended shapes will have a straight part at either or both ends.
            //  This connector part is used to connect that glyph to other glyphs in the assembly. 
            //  These connectors need to overlap to compensate for rounding errors and hinting corrections at a lower resolution.
            //  The MinConnectorOverlap value tells how much overlap is necessary for this particular font.

            //  Two arrays of offsets to MathGlyphConstruction tables: 
            //  one array for glyphs that grow in the vertical direction, 
            //  and the other array for glyphs that grow in the horizontal direction.
            //  The arrays must be arranged in coverage order and have specified sizes.


            //MathVariants Table
            //Type          Name                    Description
            //uint16        MinConnectorOverlap     Minimum overlap of connecting glyphs during glyph construction, in design units.
            //Offset16      VertGlyphCoverage       Offset to Coverage table - from the beginning of MathVariants table.
            //Offset16      HorizGlyphCoverage      Offset to Coverage table - from the beginning of MathVariants table.
            //uint16        VertGlyphCount          Number of glyphs for which information is provided for vertically growing variants.
            //uint16        HorizGlyphCount         Number of glyphs for which information is provided for horizontally growing variants.
            //Offset16      VertGlyphConstruction[VertGlyphCount]  Array of offsets to MathGlyphConstruction tables - from the beginning of the MathVariants table, for shapes growing in vertical direction.
            //Offset16      HorizGlyphConstruction[HorizGlyphCount]    Array of offsets to MathGlyphConstruction tables - from the beginning of the MathVariants table, for shapes growing in horizontal direction.

            _mathVariantsTable = new MathVariantsTable();

            long beginAt = reader.BaseStream.Position;
            //
            _mathVariantsTable.MinConnectorOverlap = reader.ReadUInt16();
            //
            ushort vertGlyphCoverageOffset = reader.ReadUInt16();
            ushort horizGlyphCoverageOffset = reader.ReadUInt16();
            ushort vertGlyphCount = reader.ReadUInt16();
            ushort horizGlyphCount = reader.ReadUInt16();
            ushort[] vertGlyphConstructions = Utils.ReadUInt16Array(reader, vertGlyphCount);
            ushort[] horizonGlyphConstructions = Utils.ReadUInt16Array(reader, horizGlyphCount);
            //

            if (vertGlyphCoverageOffset > 0)
            {
                _mathVariantsTable.vertCoverage = CoverageTable.CreateFrom(reader, beginAt + vertGlyphCoverageOffset);
            }

            if (horizGlyphCoverageOffset > 0)
            {
                //may be null?, eg. found in font Linux Libertine Regular (https://sourceforge.net/projects/linuxlibertine/)
                _mathVariantsTable.horizCoverage = CoverageTable.CreateFrom(reader, beginAt + horizGlyphCoverageOffset);
            }

            //read math construction table

            //(3.1)
            //vertical
            var vertGlyphConstructionTables = _mathVariantsTable.vertConstructionTables = new MathGlyphConstruction[vertGlyphCount];
            for (int i = 0; i < vertGlyphCount; ++i)
            {
                reader.BaseStream.Position = beginAt + vertGlyphConstructions[i];
                vertGlyphConstructionTables[i] = ReadMathGlyphConstructionTable(reader);
            }

            //(3.2)
            //horizon
            var horizGlyphConstructionTables = _mathVariantsTable.horizConstructionTables = new MathGlyphConstruction[horizGlyphCount];
            for (int i = 0; i < horizGlyphCount; ++i)
            {
                reader.BaseStream.Position = beginAt + horizonGlyphConstructions[i];
                horizGlyphConstructionTables[i] = ReadMathGlyphConstructionTable(reader);
            }
        }


        /// <summary>
        /// (3.1, 3.2)
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        MathGlyphConstruction ReadMathGlyphConstructionTable(BinaryReader reader)
        {

            //MathGlyphConstruction Table  
            //The MathGlyphConstruction table provides information on finding or assembling extended variants for one particular glyph.
            //It can be used for shapes that grow in both horizontal and vertical directions.

            //The first entry is the offset to the GlyphAssembly table that specifies how the shape for this glyph can be assembled 
            //from parts found in the glyph set of the font.
            //If no such assembly exists, this offset will be set to NULL.

            //The MathGlyphConstruction table also contains the count and array of ready-made glyph variants for the specified glyph.
            //Each variant consists of the glyph index and this glyph’s measurement in the direction of extension(vertical or horizontal).

            //Note that it is quite possible that both the GlyphAssembly table and some variants are defined for a particular glyph.
            //For example, the font may specify several variants for curly braces of different sizes,
            //and a general mechanism of how larger versions of curly braces can be constructed by stacking parts found in the glyph set.
            //First attempt is made to find glyph among provided variants.
            //
            //However, if the required size is bigger than all glyph variants provided, 
            //the general mechanism can be employed to typeset the curly braces as a glyph assembly.


            //MathGlyphConstruction Table
            //Type          Name            Description
            //Offset16      GlyphAssembly   Offset to GlyphAssembly table for this shape - from the beginning of MathGlyphConstruction table.May be NULL.
            //uint16        VariantCount    Count of glyph growing variants for this glyph.
            //MathGlyphVariantRecord MathGlyphVariantRecord [VariantCount]   MathGlyphVariantRecords for alternative variants of the glyphs.

            long beginAt = reader.BaseStream.Position;

            var glyphConstructionTable = new MathGlyphConstruction();

            ushort glyphAsmOffset = reader.ReadUInt16();
            ushort variantCount = reader.ReadUInt16();

            var variantRecords = glyphConstructionTable.glyphVariantRecords = new MathGlyphVariantRecord[variantCount];

            for (int i = 0; i < variantCount; ++i)
            {
                variantRecords[i] = new MathGlyphVariantRecord(
                    reader.ReadUInt16(),
                    reader.ReadUInt16()
                    );
            }


            //read glyph asm table
            if (glyphAsmOffset > 0)//may be NULL
            {
                reader.BaseStream.Position = beginAt + glyphAsmOffset;
                FillGlyphAssemblyInfo(reader, glyphConstructionTable);
            }
            return glyphConstructionTable;
        }
        /// <summary>
        /// (3.1, 3.2,)
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="glyphConstruction"></param>
        static void FillGlyphAssemblyInfo(BinaryReader reader, MathGlyphConstruction glyphConstruction)
        {
            //since MathGlyphConstructionTable: GlyphAssembly is 1:1 
            //---------
            //GlyphAssembly Table 
            //The GlyphAssembly table specifies how the shape for a particular glyph can be constructed from parts found in the glyph set.
            //The table defines the italics correction of the resulting assembly, and a number of parts that have to be put together to form the required shape.

            //GlyphAssembly
            //Type              Name                    Description
            //MathValueRecord   ItalicsCorrection       Italics correction of this GlyphAssembly.Should not depend on the assembly size.
            //uint16            PartCount               Number of parts in this assembly.
            //GlyphPartRecord   PartRecords[PartCount]  Array of part records, 
            //                                          from left to right  (for assemblies that extend horizontally) and 
            //                                          bottom to top(for assemblies that extend vertically).. 

            //The result of the assembly process is an array of glyphs with an offset specified for each of those glyphs.
            //When drawn consecutively at those offsets, the glyphs should combine correctly and produce the required shape.

            //The offsets in the direction of growth (advance offsets), as well as the number of parts labeled as extenders, 
            //are determined based on the size requirement for the resulting assembly.

            //Note that the glyphs comprising the assembly should be designed so that they align properly in the direction that is orthogonal to the direction of growth.


            glyphConstruction.GlyphAsm_ItalicCorrection = reader.ReadMathValueRecord();
            ushort partCount = reader.ReadUInt16();
            var partRecords = glyphConstruction.GlyphAsm_GlyphPartRecords = new GlyphPartRecord[partCount];
            for (int i = 0; i < partCount; ++i)
            {
                partRecords[i] = new GlyphPartRecord(
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16(),
                    reader.ReadUInt16()
                    );
            }
        }
    }

    class MathItalicsCorrectonInfoTable
    {
        //MathItalicsCorrectonInfo Table 
        //The MathItalicsCorrectionInfo table contains italics correction values for slanted glyphs used in math typesetting.The table consists of the following parts:

        //    Coverage of glyphs for which the italics correction values are provided.It is assumed to be zero for all other glyphs.
        //    Count of covered glyphs.
        //    Array of italic correction values for each covered glyph, in order of coverage.The italics correction is the measurement of how slanted the glyph is, and how much its top part protrudes to the right. For example, taller letters tend to have larger italics correction, and a V will probably have larger italics correction than an L.

        //Italics correction can be used in the following situations:

        //    When a run of slanted characters is followed by a straight character (such as an operator or a delimiter), the italics correction of the last glyph is added to its advance width.
        //    When positioning limits on an N-ary operator (e.g., integral sign), the horizontal position of the upper limit is moved to the right by ½ of the italics correction, while the position of the lower limit is moved to the left by the same distance.
        //    When positioning superscripts and subscripts, their default horizontal positions are also different by the amount of the italics correction of the preceding glyph.

        public MathValueRecord[] ItalicCorrections;
        public CoverageTable CoverageTable;

    }
    class MathTopAccentAttachmentTable
    {
        public MathValueRecord[] TopAccentAttachment;
        public CoverageTable CoverageTable;
    }


    class MathVariantsTable
    {
        public ushort MinConnectorOverlap;
        public CoverageTable vertCoverage;
        public CoverageTable horizCoverage;
        public MathGlyphConstruction[] vertConstructionTables;
        public MathGlyphConstruction[] horizConstructionTables;
    }




}
