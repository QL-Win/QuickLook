//MIT, 2020-present, WinterDev

using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/meta
    //meta — Metadata Table


    //NOTE: readmore about language tag, https://tools.ietf.org/html/bcp47
    //...The language of an information item or a user's language preferences
    //often need to be identified so that appropriate processing can be
    //applied. 
    //...
    //...
    //One means of indicating the language used is by labeling the
    //information content with an identifier or "tag".  These tags can also
    //be used to specify the user's preferences when selecting information
    //content or to label additional attributes of content and associated
    //resources. 

    //..
    //..The Language Tag
    //..

    //Language tags are used to help identify languages, whether spoken,
    //written, signed, or otherwise signaled, for the purpose of
    //communication.This includes constructed and artificial languages
    //but excludes languages not intended primarily for human
    //communication, such as programming languages.





    class Meta : TableEntry
    {
        //The metadata table contains various metadata values for the font. 
        //Different categories of metadata are identified by four-character tags. 
        //Values for different categories can be either binary or text.

        public const string _N = "meta";
        public override string Name => _N;
        public Meta() { }
        /// <summary>
        /// dlng tags
        /// </summary>
        public string[] DesignLanguageTags { get; private set; }
        /// <summary>
        /// slng tags
        /// </summary>
        public string[] SupportedLanguageTags { get; private set; }
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //found in some fonts (eg tahoma)

            //Table Formats
            //The metadata table begins with a header, structured as follows.

            //Metadata header:
            //Type 	    Name 	        Description
            //uint32 	version 	    Version number of the metadata table — set to 1.
            //uint32 	flags 	        Flags — currently unused; set to 0.
            //uint32 	(reserved) 	    Not used; should be set to 0.
            //uint32 	dataMapsCount 	The number of data maps in the table.
            //DataMap 	dataMaps[dataMapsCount] 	Array of data map records.


            long tableStartsAt = reader.BaseStream.Position;//***

            uint version = reader.ReadUInt32();
            uint flags = reader.ReadUInt32();
            uint reserved = reader.ReadUInt32();
            uint dataMapsCount = reader.ReadUInt32();
#if DEBUG
            if (version != 1 || flags != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif 

            DataMapRecord[] dataMaps = new DataMapRecord[dataMapsCount];
            for (int i = 0; i < dataMaps.Length; ++i)
            {
                dataMaps[i] = new DataMapRecord(reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32());
            }

            //Metadata tags identify the category of information provided and representation format used for a given metadata value.
            //A registry of commonly - used tags is maintained, 
            //but private, vendor-determined tags can also be used.

            //Like other OpenType tags,            
            //metadata tags are four unsigned bytes that can equivalently be interpreted as a string of four ASCII characters. 
            //Metadata tags must begin with a letter (0x41 to 0x5A, 0x61 to 0x7A) and
            //must use only letters, digits (0x30 to 0x39) or space (0x20). 
            //Space characters must only occur as trailing characters in tags that have fewer than four letters or digits.

            //Privately - defined axis tags must begin with an uppercase letter(0x41 to 0x5A), 
            //and must use only uppercase letters or digits. 
            //Registered axis tags must not use that pattern, but can be any other valid pattern.

            //The data for a given record may be either textual or binary.
            //The representation format is specified for each tag. 
            //Depending on the tag, multiple records for a given tag or multiple, 
            //delimited values in a record may or may not be permitted, as specified for each tag.
            //If only one record or value is permitted for a tag,
            //then any instances after the first may be ignored.

            //translate data for each tags
            //The following registered tags are defined or reserved at this time:



            //------------------------------------------------------------------------------------ 
            //IMPORTANT:
            //Note: OpenType Layout script and language system tags are not the same as 
            //those used in BCP 47 and should not be referenced when creating or processing ScriptLangTags.
            //------------------------------------------------------------------------------------ 
            //... In most cases, however, generic tags should be used,
            //and it is anticipated that most tags used in 'dlng' and 'slng' metadata declarations will consist only of a script subtag.
            //Language or other subtags can be included, however, and may be appropriate in some cases. 
            //Implementations must allow for ScriptLangTags that include additional subtags,
            //but they may also choose to interpret only the script subtag and ignore other subtags.


            for (int i = 0; i < dataMaps.Length; ++i)
            {
                DataMapRecord record = dataMaps[i];

                switch (record.GetTagString())
                {
#if DEBUG
                    default:
                        System.Diagnostics.Debug.WriteLine("openfont-meta: unknown tag:" + record.GetTagString());
                        break;
#endif
                    case "apple": //Reserved — used by Apple.
                    case "bild"://Reserved — used by Apple.
                        break;
                    case "dlng":
                        {
                            //The values for 'dlng' and 'slng' are comprised of a series of comma-separated ScriptLangTags,
                            //which are described in detail below.
                            //Spaces may follow the comma delimiters and are ignored.
                            //Each ScriptLangTag identifies a language or script. 

                            //A list of tags is interpreted to imply that all of the languages or scripts are included.

                            //dlng 	Design languages Text, 
                            //using only Basic Latin (ASCII) characters. 
                            //Indicates languages and/or scripts for the user audiences that the font was primarily designed for.

                            //Only one instance is used.

                            //dlng 	Design languages Text, 
                            //The 'dlng' value is used to indicate the languages or scripts of the primary user audiences for which the font was designed.

                            //This value may be useful for selecting default font formatting based on content language,                             
                            //for presenting filtered font options based on user language preferences, 
                            //or similar applications involving the language or script of content or user settings.

                            if (DesignLanguageTags == null)
                            {
                                //Only one instance is used.
                                reader.BaseStream.Position = tableStartsAt + record.dataOffset;
                                DesignLanguageTags = ReadCommaSepData(reader.ReadBytes((int)record.dataLength));
                            }
                        }
                        break;
                    case "slng":
                        {
                            //slng Supported languages Text, using only Basic Latin(ASCII) characters.
                            //Indicates languages and / or scripts that the font is declared to be capable of supporting.

                            //Only one instance is used. 

                            //slng Supported languages Text
                            //The 'slng' value is used to declare languages or scripts that the font is capable of supported. 

                            //This value may be useful for font fallback mechanisms or other applications involving the language or 
                            //script of content or user settings.
                            //Note: Implementations that use 'slng' values in a font may choose to ignore Unicode - range bits set in the OS/ 2 table.

                            if (SupportedLanguageTags == null)
                            {   //Only one instance is used. 
                                reader.BaseStream.Position = tableStartsAt + record.dataOffset;
                                SupportedLanguageTags = ReadCommaSepData(reader.ReadBytes((int)record.dataLength));
                            }
                        }
                        break;
                }
            }
        }
        static string[] ReadCommaSepData(byte[] data)
        {
            string[] tags = System.Text.Encoding.UTF8.GetString(data).Split(',');
            for (int i = 0; i < tags.Length; ++i)
            {
                tags[i] = tags[i].Trim();

#if DEBUG
                if (tags[i].Contains("-"))
                {

                }
#endif

            }
            return tags;
        }
        readonly struct DataMapRecord
        {
            public readonly uint tag;
            public readonly uint dataOffset;
            public readonly uint dataLength;
            public DataMapRecord(uint tag, uint dataOffset, uint dataLength)
            {
                this.tag = tag;
                this.dataOffset = dataOffset;
                this.dataLength = dataLength;
            }
            public string GetTagString() => Utils.TagToString(tag);
#if DEBUG
            public override string ToString()
            {
                return GetTagString() + ":" + dataOffset + "," + dataLength;
            }
#endif

        }

    }
}