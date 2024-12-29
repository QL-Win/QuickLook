//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System.IO;
using System.Text;
namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/name

    public class NameEntry : TableEntry
    {
        public const string _N = "name";
        public override string Name => _N;
        //
        protected override void ReadContentFrom(BinaryReader reader)
        {

            ushort uFSelector = reader.ReadUInt16();
            ushort uNRCount = reader.ReadUInt16();
            ushort uStorageOffset = reader.ReadUInt16();

            uint offset = this.Header.Offset;
            for (int j = 0; j <= uNRCount; j++)
            {
                var ttRecord = new TT_NAME_RECORD()
                {
                    uPlatformID = reader.ReadUInt16(),
                    uEncodingID = reader.ReadUInt16(),
                    uLanguageID = reader.ReadUInt16(),
                    uNameID = reader.ReadUInt16(),
                    uStringLength = reader.ReadUInt16(),
                    uStringOffset = reader.ReadUInt16(),
                };


                long nPos = reader.BaseStream.Position;
                reader.BaseStream.Seek(offset + ttRecord.uStringOffset + uStorageOffset, SeekOrigin.Begin);

                byte[] buf = reader.ReadBytes(ttRecord.uStringLength);
                Encoding enc2;
                if (ttRecord.uEncodingID == 3 || ttRecord.uEncodingID == 1)
                {

                    enc2 = Encoding.BigEndianUnicode;
                }
                else
                {
                    enc2 = Encoding.UTF8;
                }
                string strRet = enc2.GetString(buf, 0, buf.Length);
                //....
                switch ((NameIdKind)ttRecord.uNameID)
                {
                    default:
                        //skip
                        break;
                    case NameIdKind.VersionString:
                        VersionString = strRet;
                        break;
                    case NameIdKind.FontFamilyName:
                        FontName = strRet;
                        break;
                    case NameIdKind.FontSubfamilyName:
                        FontSubFamily = strRet;
                        break;
                    case NameIdKind.UniqueFontIden:
                        UniqueFontIden = strRet;
                        break;
                    case NameIdKind.FullFontName:
                        FullFontName = strRet;
                        break;
                    //
                    case NameIdKind.PostScriptName:
                        PostScriptName = strRet;
                        break;
                    case NameIdKind.PostScriptCID_FindfontName:
                        PostScriptCID_FindfontName = strRet;
                        break;
                    //
                    case NameIdKind.TypographicFamilyName:
                        TypographicFamilyName = strRet;
                        break;
                    case NameIdKind.TypographyicSubfamilyName:
                        TypographyicSubfamilyName = strRet;
                        break;

                }
                //move to saved pos
                reader.BaseStream.Seek(nPos, SeekOrigin.Begin);
            }
        }


        /// <summary>
        /// Font Family name. 
        /// This family name is assumed to be shared among fonts that 
        /// differ only in weight or style (italic, oblique). 
        /// 
        /// Font Family name is used in combination with Font Subfamily name (name ID 2)...
        /// </summary>
        public string FontName { get; private set; }
        /// <summary>
        ///  	Font Subfamily name. The Font Subfamily name distinguishes the fonts in a group with the 
        ///  	same Font Family name (name ID 1).
        ///  	This is assumed to address style (italic, oblique) and weight variants only. 
        ///  	
        ///      A font with no distinctive weight or style (e.g. medium weight, not italic, and OS/2.fsSelection bit 6 set) 
        ///      should use the string “Regular” as the Font Subfamily name (for English language). 
        /// </summary>
        public string FontSubFamily { get; private set; }
        public string UniqueFontIden { get; private set; }
        /// <summary>
        /// Full font name that reflects all family and relevant subfamily descriptors. 
        /// The full font name is generally a combination of name IDs 1 and 2, or 
        /// of name IDs 16 and 17, or a similar human-readable variant. 
        /// </summary>
        public string FullFontName { get; set; }

        public string VersionString { get; set; }

        /// <summary>
        /// PostScript name for the font; Name ID 6 specifies a string which is used to invoke a PostScript language font that corresponds to this OpenType font.
        /// When translated to ASCII, the name string must be no longer than 63 characters and restricted to the printable ASCII subset, 
        /// codes 33 to 126, except for the 10 characters '[', ']', '(', ')', '{', '}', '&lt;', '&gt;', '/', '%'.
        /// 
        ///In a CFF OpenType font, there is no requirement that this name be the same as the font name in the CFF’s Name INDEX.Thus,
        ///the same CFF may be shared among multiple font components in a Font Collection.
        ///...
        /// </summary>
        public string PostScriptName { get; set; }
        public string PostScriptCID_FindfontName { get; set; }
        //
        public string TypographicFamilyName { get; set; }
        public string TypographyicSubfamilyName { get; set; }


        struct TT_NAME_RECORD
        {
            public ushort uPlatformID;
            public ushort uEncodingID;
            public ushort uLanguageID;
            public ushort uNameID;
            public ushort uStringLength;
            public ushort uStringOffset;
        }



        enum NameIdKind
        {
            //...
            //[A] The key information for this table for Microsoft platforms 
            //relates to the use of strings 1, 2, 4, 16 and 17.
            //...


            CopyRightNotice, //0
            FontFamilyName, //1 , [A]
            FontSubfamilyName,//2, [A]
            UniqueFontIden, //3
            FullFontName, //4, [A]
            VersionString,//5
            PostScriptName,//6
            Trademark,//7
            ManufacturerName,//8
            Designer,//9
            Description, //10
            UrlVendor, //11
            UrlDesigner,//12
            LicenseDescription, //13
            LicenseInfoUrl,//14
            Reserved,//15
            TypographicFamilyName,//16 , [A]
            TypographyicSubfamilyName,//17, [A]
            CompatibleFull,//18
            SampleText,//19
            PostScriptCID_FindfontName,//20
            //------------------            
            WWSFamilyName,//21
            WWSSubfamilyName,//22
            //------------------
            LightBackgroundPalette,//23, CPAL
            DarkBackgroundPalette,//24, CPAL
            //------------------
            VariationsPostScriptNamePrefix,//25

        }



    }

}
