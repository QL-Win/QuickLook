//Apache2, 2016-present, WinterDev 
using System.IO;

namespace Typography.OpenFont.Tables
{

    //https://docs.microsoft.com/en-us/typography/opentype/spec/os2
    /// <summary>
    /// OS2 and Windows metrics, 
    /// consists of a set of metrics and other data
    /// that are REQUIRED in OpenType fonts.
    /// </summary>
    public class OS2Table : TableEntry
    {
        public const string _N = "OS/2";
        public override string Name => _N;
        //

        // Type     Name of  Entry        Comments
        //uint16 	version 	           0x0005
        //int16 	xAvgCharWidth 	    
        //uint16 	usWeightClass 	 
        //uint16 	usWidthClass 	 
        //uint16 	fsType 	 
        public ushort version;          //0-5
        public short xAvgCharWidth;     //just average, not recommend to use.
        public ushort usWeightClass;    //visual weight (degree of blackness or thickness of strokes), 0-1000

        //usWeightClass:
        //Value Description 	C Definition (from windows.h)
        //100 	Thin 	        FW_THIN
        //200 	Extra-light     FW_EXTRALIGHT
        //      (Ultra-light) 
        //300 	Light 	        FW_LIGHT
        //400 	Normal  	    FW_NORMAL
        //      (Regular)
        //500 	Medium 	        FW_MEDIUM
        //600 	Semi-bold   	FW_SEMIBOLD
        //      (Demi-bold)
        //700 	Bold 	        FW_BOLD
        //800 	Extra-bold  	FW_EXTRABOLD
        //      (Ultra-bold)
        //900 	Black (Heavy) 	FW_BLACK

        public ushort usWidthClass;     //A relative change from the normal aspect ratio (width to height ratio), 
                                        //as specified by a font designer for the glyphs in a font.
                                        //Although every glyph in a font may have a different numeric aspect ratio, 
                                        //each glyph in a font of normal width is considered to have a relative aspect ratio of one.
                                        //When a new type style is created of a different width class (either by a font designer or by some automated means)
                                        //the relative aspect ratio of the characters in the new font is some percentage greater or less than those same characters in the normal 
                                        //font — it is this difference that this parameter specifies. 

        //usWidthClass
        //Value Description 	    C Definition 	        % of normal
        //1 	Ultra-condensed 	FWIDTH_ULTRA_CONDENSED 	50
        //2 	Extra-condensed 	FWIDTH_EXTRA_CONDENSED 	62.5
        //3 	Condensed 	        FWIDTH_CONDENSED 	    75
        //4 	Semi-condensed 	    FWIDTH_SEMI_CONDENSED 	87.5
        //5 	Medium (normal) 	FWIDTH_NORMAL 	        100
        //6 	Semi-expanded 	    FWIDTH_SEMI_EXPANDED 	112.5
        //7 	Expanded 	        FWIDTH_EXPANDED 	    125
        //8 	Extra-expanded 	    FWIDTH_EXTRA_EXPANDED 	150
        //9 	Ultra-expanded      FWIDTH_ULTRA_EXPANDED 	200








        public ushort fsType;           //Type flags., embedding licensing rights for the font

        //int16 	ySubscriptXSize 	 
        //int16 	ySubscriptYSize 	 
        //int16 	ySubscriptXOffset 	 
        //int16 	ySubscriptYOffset 	 
        //int16 	ySuperscriptXSize 	 
        //int16 	ySuperscriptYSize 	 
        //int16 	ySuperscriptXOffset 	 
        //int16 	ySuperscriptYOffset 	 
        //int16 	yStrikeoutSize 	 
        //int16 	yStrikeoutPosition 	 
        //int16 	sFamilyClass 	
        public short ySubscriptXSize;
        public short ySubscriptYSize;
        public short ySubscriptXOffset;
        public short ySubscriptYOffset;
        public short ySuperscriptXSize;
        public short ySuperscriptYSize;
        public short ySuperscriptXOffset;
        public short ySuperscriptYOffset;
        public short yStrikeoutSize;
        public short yStrikeoutPosition;
        public short sFamilyClass;      //This parameter is a classification of font-family design. ,see https://www.microsoft.com/typography/otspec/ibmfc.htm

        //uint8 	panose[10] 	        (array of bytes,len =10)
        public byte[] panose;
        //uint32 	ulUnicodeRange1 	Bits 0-31
        //uint32 	ulUnicodeRange2 	Bits 32-63
        //uint32 	ulUnicodeRange3 	Bits 64-95
        //uint32 	ulUnicodeRange4 	Bits 96-127
        public uint ulUnicodeRange1;
        public uint ulUnicodeRange2;
        public uint ulUnicodeRange3;
        public uint ulUnicodeRange4;

        //Tag 	    achVendID[4] 	    char 4 
        public uint achVendID;          //see 'registered venders' at https://www.microsoft.com/typography/links/vendorlist.aspx

        //uint16 	fsSelection 	 
        //uint16 	usFirstCharIndex 	 
        //uint16 	usLastCharIndex 
        public ushort fsSelection;      //Contains information concerning the nature of the font patterns
        public ushort usFirstCharIndex;
        public ushort usLastCharIndex;
        //int16 	sTypoAscender 	 
        //int16 	sTypoDescender 	 
        //int16 	sTypoLineGap 	 
        public short sTypoAscender;
        public short sTypoDescender;
        public short sTypoLineGap;
        //uint16 	usWinAscent 	 
        //uint16 	usWinDescent 	 
        //uint32 	ulCodePageRange1 	Bits 0-31
        //uint32 	ulCodePageRange2 	Bits 32-63
        public ushort usWinAscent;
        public ushort usWinDescent;
        public uint ulCodePageRange1;
        public uint ulCodePageRange2;
        //int16 	sxHeight 	 
        //int16 	sCapHeight 	  
        public short sxHeight;
        public short sCapHeight;
        //uint16 	usDefaultChar 	 
        //uint16 	usBreakChar 	 
        //uint16 	usMaxContext 	 
        //uint16 	usLowerOpticalPointSize 	 
        //uint16 	usUpperOpticalPointSize
        public ushort usDefaultChar;
        public ushort usBreakChar;
        public ushort usMaxContext;
        public ushort usLowerOpticalPointSize;
        public ushort usUpperOpticalPointSize;


#if DEBUG
        public override string ToString()
        {
            return version + "," + Utils.TagToString(this.achVendID);
        }
#endif
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //Six versions of the OS/2 table have been defined: versions 0 to 5
            //Versions 0 to 4 were defined in earlier versions of the OpenType or
            //TrueType specifications. 

            switch (this.version = reader.ReadUInt16())
            {
                default: throw new System.NotSupportedException();
                case 0: //defined in TrueType revision 1.5
                    ReadVersion0(reader);
                    break;
                case 1: // defined in TrueType revision 1.66
                    ReadVersion1(reader);
                    break;
                case 2: //defined in OpenType version 1.2
                    ReadVersion2(reader);
                    break;
                case 3: //defined in OpenType version 1.4
                    ReadVersion3(reader);
                    break;
                case 4: //defined in OpenType version 1.6
                    ReadVersion4(reader);
                    break;
                case 5:
                    ReadVersion5(reader);
                    break;
            }
        }
        void ReadVersion0(BinaryReader reader)
        {
            //https://www.microsoft.com/typography/otspec/os2ver0.htm
            //USHORT 	version 	0x0000
            //SHORT 	xAvgCharWidth 	 
            //USHORT 	usWeightClass 	 
            //USHORT 	usWidthClass 	 
            //USHORT 	fsType 	 
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();

            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();
            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulCharRange[4] 	Bits 0-31
            this.ulUnicodeRange1 = reader.ReadUInt32();
            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 	 
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 	 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
        }

        void ReadVersion1(BinaryReader reader)
        {
            //https://www.microsoft.com/typography/otspec/os2ver1.htm

            //SHORT 	xAvgCharWidth 	 
            //USHORT 	usWeightClass 	 
            //USHORT 	usWidthClass 	 
            //USHORT 	fsType 	 
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();
            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();

            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127
            this.ulUnicodeRange1 = reader.ReadUInt32();
            this.ulUnicodeRange2 = reader.ReadUInt32();
            this.ulUnicodeRange3 = reader.ReadUInt32();
            this.ulUnicodeRange4 = reader.ReadUInt32();
            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 	
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 	 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent 	 
            //ULONG 	ulCodePageRange1 	Bits 0-31
            //ULONG 	ulCodePageRange2 	Bits 32-63
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
            this.ulCodePageRange1 = reader.ReadUInt32();
            this.ulCodePageRange2 = reader.ReadUInt32();
        }
        void ReadVersion2(BinaryReader reader)
        {
            //https://www.microsoft.com/typography/otspec/os2ver2.htm

            // 
            //SHORT 	xAvgCharWidth 	 
            //USHORT 	usWeightClass 	 
            //USHORT 	usWidthClass 	 
            //USHORT 	fsType 	 
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();
            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();
            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127
            this.ulUnicodeRange1 = reader.ReadUInt32();
            this.ulUnicodeRange2 = reader.ReadUInt32();
            this.ulUnicodeRange3 = reader.ReadUInt32();
            this.ulUnicodeRange4 = reader.ReadUInt32();
            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 	 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent 	 
            //ULONG 	ulCodePageRange1 	Bits 0-31
            //ULONG 	ulCodePageRange2 	Bits 32-63
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
            this.ulCodePageRange1 = reader.ReadUInt32();
            this.ulCodePageRange2 = reader.ReadUInt32();
            //SHORT 	sxHeight 	 
            //SHORT 	sCapHeight 	 
            //USHORT 	usDefaultChar 	 
            //USHORT 	usBreakChar 	 
            //USHORT 	usMaxContext
            this.sxHeight = reader.ReadInt16();
            this.sCapHeight = reader.ReadInt16();
            this.usDefaultChar = reader.ReadUInt16();
            this.usBreakChar = reader.ReadUInt16();
            this.usMaxContext = reader.ReadUInt16();
        }
        void ReadVersion3(BinaryReader reader)
        {

            //https://www.microsoft.com/typography/otspec/os2ver3.htm
            //            USHORT 	version 	0x0003
            //SHORT 	xAvgCharWidth 	 
            //USHORT 	usWeightClass 	 
            //USHORT 	usWidthClass 	 
            //USHORT 	fsType 	 
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();
            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();
            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127
            this.ulUnicodeRange1 = reader.ReadUInt32();
            this.ulUnicodeRange2 = reader.ReadUInt32();
            this.ulUnicodeRange3 = reader.ReadUInt32();
            this.ulUnicodeRange4 = reader.ReadUInt32();
            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 	 
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent 	 
            //ULONG 	ulCodePageRange1 	Bits 0-31
            //ULONG 	ulCodePageRange2 	Bits 32-63
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
            this.ulCodePageRange1 = reader.ReadUInt32();
            this.ulCodePageRange2 = reader.ReadUInt32();
            //SHORT 	sxHeight 	 
            //SHORT 	sCapHeight 	 
            //USHORT 	usDefaultChar 	 
            //USHORT 	usBreakChar 	 
            //USHORT 	usMaxContext
            this.sxHeight = reader.ReadInt16();
            this.sCapHeight = reader.ReadInt16();
            this.usDefaultChar = reader.ReadUInt16();
            this.usBreakChar = reader.ReadUInt16();
            this.usMaxContext = reader.ReadUInt16();
        }
        void ReadVersion4(BinaryReader reader)
        {
            //https://www.microsoft.com/typography/otspec/os2ver4.htm

            //SHORT 	xAvgCharWidth 	 
            //USHORT 	usWeightClass 	 
            //USHORT 	usWidthClass 	 
            //USHORT 	fsType 	 
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();
            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();
            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127
            this.ulUnicodeRange1 = reader.ReadUInt32();
            this.ulUnicodeRange2 = reader.ReadUInt32();
            this.ulUnicodeRange3 = reader.ReadUInt32();
            this.ulUnicodeRange4 = reader.ReadUInt32();
            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 	 
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 	 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent 	 
            //ULONG 	ulCodePageRange1 	Bits 0-31
            //ULONG 	ulCodePageRange2 	Bits 32-63
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
            this.ulCodePageRange1 = reader.ReadUInt32();
            this.ulCodePageRange2 = reader.ReadUInt32();
            //SHORT 	sxHeight 	 
            //SHORT 	sCapHeight 	 
            //USHORT 	usDefaultChar 	 
            //USHORT 	usBreakChar 	 
            //USHORT 	usMaxContext
            this.sxHeight = reader.ReadInt16();
            this.sCapHeight = reader.ReadInt16();
            this.usDefaultChar = reader.ReadUInt16();
            this.usBreakChar = reader.ReadUInt16();
            this.usMaxContext = reader.ReadUInt16();
        }

        void ReadVersion5(BinaryReader reader)
        {
            this.xAvgCharWidth = reader.ReadInt16();
            this.usWeightClass = reader.ReadUInt16();
            this.usWidthClass = reader.ReadUInt16();
            this.fsType = reader.ReadUInt16();
            //SHORT 	ySubscriptXSize 	 
            //SHORT 	ySubscriptYSize 	 
            //SHORT 	ySubscriptXOffset 	 
            //SHORT 	ySubscriptYOffset 	 
            //SHORT 	ySuperscriptXSize 	 
            //SHORT 	ySuperscriptYSize 	 
            //SHORT 	ySuperscriptXOffset 	 
            //SHORT 	ySuperscriptYOffset 	 
            //SHORT 	yStrikeoutSize 	 
            //SHORT 	yStrikeoutPosition 	 
            //SHORT 	sFamilyClass 	 
            this.ySubscriptXSize = reader.ReadInt16();
            this.ySubscriptYSize = reader.ReadInt16();
            this.ySubscriptXOffset = reader.ReadInt16();
            this.ySubscriptYOffset = reader.ReadInt16();
            this.ySuperscriptXSize = reader.ReadInt16();
            this.ySuperscriptYSize = reader.ReadInt16();
            this.ySuperscriptXOffset = reader.ReadInt16();
            this.ySuperscriptYOffset = reader.ReadInt16();
            this.yStrikeoutSize = reader.ReadInt16();
            this.yStrikeoutPosition = reader.ReadInt16();
            this.sFamilyClass = reader.ReadInt16();

            //BYTE 	panose[10] 	 
            this.panose = reader.ReadBytes(10);
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127
            this.ulUnicodeRange1 = reader.ReadUInt32();
            this.ulUnicodeRange2 = reader.ReadUInt32();
            this.ulUnicodeRange3 = reader.ReadUInt32();
            this.ulUnicodeRange4 = reader.ReadUInt32();

            //CHAR 	achVendID[4] 	 
            this.achVendID = reader.ReadUInt32();
            //USHORT 	fsSelection 	 
            //USHORT 	usFirstCharIndex 	 
            //USHORT 	usLastCharIndex 
            this.fsSelection = reader.ReadUInt16();
            this.usFirstCharIndex = reader.ReadUInt16();
            this.usLastCharIndex = reader.ReadUInt16();
            //SHORT 	sTypoAscender 	 
            //SHORT 	sTypoDescender 	 
            //SHORT 	sTypoLineGap 	 
            this.sTypoAscender = reader.ReadInt16();
            this.sTypoDescender = reader.ReadInt16();
            this.sTypoLineGap = reader.ReadInt16();
            //USHORT 	usWinAscent 	 
            //USHORT 	usWinDescent 	 
            //ULONG 	ulCodePageRange1 	Bits 0-31
            //ULONG 	ulCodePageRange2 	Bits 32-63
            this.usWinAscent = reader.ReadUInt16();
            this.usWinDescent = reader.ReadUInt16();
            this.ulCodePageRange1 = reader.ReadUInt32();
            this.ulCodePageRange2 = reader.ReadUInt32();
            //SHORT 	sxHeight 	 
            //SHORT 	sCapHeight 	 
            //USHORT 	usDefaultChar 	 
            //USHORT 	usBreakChar 	 
            //USHORT 	usMaxContext 	 
            this.sxHeight = reader.ReadInt16();
            this.sCapHeight = reader.ReadInt16();
            this.usDefaultChar = reader.ReadUInt16();
            this.usBreakChar = reader.ReadUInt16();
            this.usMaxContext = reader.ReadUInt16();
            //USHORT 	usLowerOpticalPointSize 	 
            //USHORT 	usUpperOpticalPointSize 	 

            this.usLowerOpticalPointSize = reader.ReadUInt16();
            this.usUpperOpticalPointSize = reader.ReadUInt16();
        }
    }
}
