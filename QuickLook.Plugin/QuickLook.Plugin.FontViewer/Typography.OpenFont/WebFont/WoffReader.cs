//MIT, 2019-present, WinterDev
//see https://www.w3.org/TR/2012/REC-WOFF-20121213/

using System;
using System.IO;
using Typography.OpenFont.Tables;
using Typography.OpenFont.Trimmable;

namespace Typography.OpenFont.WebFont
{
    //NOTE: Web Font file structure is not part of 'Open Font Format'.

    class WoffHeader
    {
        //WOFFHeader
        //UInt32  signature   0x774F4646 'wOFF'
        //UInt32 flavor  The "sfnt version" of the input font.
        //UInt32 length  Total size of the WOFF file.
        //UInt16 numTables   Number of entries in directory of font tables.
        //UInt16 reserved    Reserved; set to zero.
        //UInt32 totalSfntSize   Total size needed for the uncompressed font data, including the sfnt header, directory, and font tables(including padding).
        //UInt16  majorVersion    Major version of the WOFF file.
        //UInt16  minorVersion    Minor version of the WOFF file.
        //UInt32  metaOffset  Offset to metadata block, from beginning of WOFF file.
        //UInt32  metaLength  Length of compressed metadata block.
        //UInt32  metaOrigLength  Uncompressed size of metadata block.
        //UInt32  privOffset  Offset to private data block, from beginning of WOFF file.
        //UInt32  privLength Length of private data block.

        public uint flavor;
        public uint length;
        public uint numTables;

        //public ushort reserved;
        public uint totalSfntSize;

        public ushort majorVersion;
        public ushort minorVersion;
        public uint metaOffset;
        public uint metaLength;
        public uint metaOriginalLength;
        public uint privOffset;
        public uint privLength;
    }

    class WoffTableDirectory
    {
        //WOFF TableDirectoryEntry
        //UInt32 tag	        4-byte sfnt table identifier.
        //UInt32 offset         Offset to the data, from beginning of WOFF file.
        //UInt32 compLength     Length of the compressed data, excluding padding.
        //UInt32 origLength     Length of the uncompressed table, excluding padding.
        //UInt32 origChecksum   Checksum of the uncompressed table.
        public uint tag;

        public uint offset;
        public uint compLength;
        public uint origLength;
        public uint origChecksum;

        //translated values
        //public UnreadTableEntry unreadTableEntry; //simulate
        public string Name { get; set; }

        public long ExpectedStartAt { get; set; }
#if DEBUG

        public WoffTableDirectory()
        {
        }

        public override string ToString()
        {
            return Name;
        }

#endif
    }

    public delegate bool ZlibDecompressStreamFunc(byte[] compressedInput, byte[] decompressOutput);

    public static class WoffDefaultZlibDecompressFunc
    {
        public static ZlibDecompressStreamFunc DecompressHandler;
    }

    class WoffReader
    {
        private WoffHeader _header;

        public ZlibDecompressStreamFunc DecompressHandler;

        public PreviewFontInfo ReadPreview(BinaryReader reader)
        {
            //read preview only
            //WOFF File
            //WOFFHeader        File header with basic font type and version, along with offsets to metadata and private data blocks.
            //TableDirectory    Directory of font tables, indicating the original size, compressed size and location of each table within the WOFF file.
            //FontTables        The font data tables from the input sfnt font, compressed to reduce bandwidth requirements.
            //ExtendedMetadata  An optional block of extended metadata, represented in XML format and compressed for storage in the WOFF file.
            //PrivateData       An optional block of private data for the font designer, foundry, or vendor to use.

            PreviewFontInfo fontPreviewInfo = null;
            _header = ReadWOFFHeader(reader);
            if (_header == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("can't read ");
#endif
                return null; //notify user too
            }

            //
            WoffTableDirectory[] woffTableDirs = ReadTableDirectories(reader);
            if (woffTableDirs == null)
            {
                return null;
            }
            //try read each compressed table
            if (DecompressHandler == null)
            {
                if (WoffDefaultZlibDecompressFunc.DecompressHandler != null)
                {
                    DecompressHandler = WoffDefaultZlibDecompressFunc.DecompressHandler;
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("no Zlib DecompressHandler ");
#endif
                    return null; //notify user too
                }
            }

            TableEntryCollection tableEntryCollection = CreateTableEntryCollection(woffTableDirs);
            //for font preview, we may not need to extract
            using (MemoryStream decompressStream = new MemoryStream())
            {
                if (Extract(reader, woffTableDirs, decompressStream))
                {
                    using (ByteOrderSwappingBinaryReader reader2 = new ByteOrderSwappingBinaryReader(decompressStream))
                    {
                        decompressStream.Position = 0;
                        OpenFontReader openFontReader = new OpenFontReader();
                        PreviewFontInfo previewFontInfo = openFontReader.ReadPreviewFontInfo(tableEntryCollection, reader2);
                        if (previewFontInfo != null)
                        {
                            //add webfont info to this preview font
                            previewFontInfo.IsWebFont = true;
                        }
                        return previewFontInfo;
                    }
                }
            }

            return fontPreviewInfo;
        }

        internal bool Read(Typeface typeface, BinaryReader reader, RestoreTicket ticket)
        {
            //WOFF File
            //WOFFHeader        File header with basic font type and version, along with offsets to metadata and private data blocks.
            //TableDirectory    Directory of font tables, indicating the original size, compressed size and location of each table within the WOFF file.
            //FontTables        The font data tables from the input sfnt font, compressed to reduce bandwidth requirements.
            //ExtendedMetadata  An optional block of extended metadata, represented in XML format and compressed for storage in the WOFF file.
            //PrivateData       An optional block of private data for the font designer, foundry, or vendor to use.
            _header = ReadWOFFHeader(reader);
            if (_header == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("can't read ");
#endif
                return false;
            }

            //
            WoffTableDirectory[] woffTableDirs = ReadTableDirectories(reader);
            if (woffTableDirs == null)
            {
                return false;
            }
            //
            //try read each compressed table
            if (DecompressHandler == null)
            {
                if (WoffDefaultZlibDecompressFunc.DecompressHandler != null)
                {
                    DecompressHandler = WoffDefaultZlibDecompressFunc.DecompressHandler;
                }
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("no Zlib DecompressHandler ");
#endif
                    return false;
                }
            }

            TableEntryCollection tableEntryCollection = CreateTableEntryCollection(woffTableDirs);

            using (MemoryStream decompressStream = new MemoryStream())
            {
                if (Extract(reader, woffTableDirs, decompressStream))
                {
                    using (ByteOrderSwappingBinaryReader reader2 = new ByteOrderSwappingBinaryReader(decompressStream))
                    {
                        decompressStream.Position = 0;
                        OpenFontReader openFontReader = new OpenFontReader();
                        return openFontReader.ReadTableEntryCollection(typeface, ticket, tableEntryCollection, reader2);
                    }
                }
            }
            return false;
        }

        static TableEntryCollection CreateTableEntryCollection(WoffTableDirectory[] woffTableDirs)
        {
            TableEntryCollection tableEntryCollection = new TableEntryCollection();
            for (int i = 0; i < woffTableDirs.Length; ++i)
            {
                WoffTableDirectory woffTableDir = woffTableDirs[i];
                tableEntryCollection.AddEntry(
                    new UnreadTableEntry(
                        new TableHeader(woffTableDir.tag,
                            woffTableDir.origChecksum,
                            (uint)woffTableDir.ExpectedStartAt,
                            woffTableDir.origLength)));
            }

            return tableEntryCollection;
        }

        static WoffHeader ReadWOFFHeader(BinaryReader reader)
        {
            //WOFFHeader
            //UInt32  signature         0x774F4646 'wOFF'
            //UInt32  flavor            The "sfnt version" of the input font.
            //UInt32  length            Total size of the WOFF file.
            //UInt16  numTables         Number of entries in directory of font tables.
            //UInt16  reserved          Reserved; set to zero.
            //UInt32  totalSfntSize     Total size needed for the uncompressed font data, including the sfnt header, directory, and font tables(including padding).
            //UInt16  majorVersion      Major version of the WOFF file.
            //UInt16  minorVersion      Minor version of the WOFF file.
            //UInt32  metaOffset        Offset to metadata block, from beginning of WOFF file.
            //UInt32  metaLength        Length of compressed metadata block.
            //UInt32  metaOrigLength    Uncompressed size of metadata block.
            //UInt32  privOffset        Offset to private data block, from beginning of WOFF file.
            //UInt32  privLength        Length of private data block.

            //signature
            byte b0 = reader.ReadByte();
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();
            byte b3 = reader.ReadByte();
            if (!(b0 == 0x77 && b1 == 0x4f && b2 == 0x46 && b3 == 0x46))
            {
                return null;
            }
            WoffHeader header = new WoffHeader();
            header.flavor = reader.ReadUInt32BE();
            header.length = reader.ReadUInt32BE();
            header.numTables = reader.ReadUInt16BE();
            ushort reserved = reader.ReadUInt16BE();
            header.totalSfntSize = reader.ReadUInt32BE();

            header.majorVersion = reader.ReadUInt16BE();
            header.minorVersion = reader.ReadUInt16BE();

            header.metaOffset = reader.ReadUInt32BE();
            header.metaLength = reader.ReadUInt32BE();
            header.metaOriginalLength = reader.ReadUInt32BE();

            header.privOffset = reader.ReadUInt32BE();
            header.privLength = reader.ReadUInt32BE();

            return header;
        }

        WoffTableDirectory[] ReadTableDirectories(BinaryReader reader)
        {
            //The table directory is an array of WOFF table directory entries, as defined below.
            //The directory follows immediately after the WOFF file header;
            //therefore, there is no explicit offset in the header pointing to this block.
            //Its size is calculated by multiplying the numTables value in the WOFF header times the size of a single WOFF table directory.
            //Each table directory entry specifies the size and location of a single font data table.

            uint tableCount = (uint)_header.numTables; //?
            //tableDirs = new WoffTableDirectory[tableCount];
            long expectedStartAt = 0;

            //simulate table entry collection
            //var tableEntryCollection = new TableEntryCollection((int)tableCount);
            WoffTableDirectory[] tableDirs = new WoffTableDirectory[tableCount];

            for (int i = 0; i < tableCount; ++i)
            {
                //UInt32 tag	        4-byte sfnt table identifier.
                //UInt32 offset         Offset to the data, from beginning of WOFF file.
                //UInt32 compLength     Length of the compressed data, excluding padding.
                //UInt32 origLength     Length of the uncompressed table, excluding padding.
                //UInt32 origChecksum   Checksum of the uncompressed table.

                WoffTableDirectory table = new WoffTableDirectory();
                table.tag = reader.ReadUInt32();
                table.offset = reader.ReadUInt32();
                table.compLength = reader.ReadUInt32();
                table.origLength = reader.ReadUInt32();
                table.origChecksum = reader.ReadUInt32();

                table.ExpectedStartAt = expectedStartAt;
                table.Name = Utils.TagToString(table.tag);
                //var tableHeader = new TableHeader(tag, origChecksum, (uint)expectedStartAt, origLength);
                //var unreadTable = new UnreadTableEntry(tableHeader);
                //tableEntryCollection.AddEntry(unreadTable);
                //table.unreadTableEntry = unreadTable;

                tableDirs[i] = table;
                expectedStartAt += table.origLength;
            }

            return tableDirs;
        }

        bool Extract(BinaryReader reader, WoffTableDirectory[] tables, Stream newDecompressedStream)
        {
            for (int i = 0; i < tables.Length; ++i)
            {
                //UInt32 tag	        4-byte sfnt table identifier.
                //UInt32 offset         Offset to the data, from beginning of WOFF file.
                //UInt32 compLength     Length of the compressed data, excluding padding.
                //UInt32 origLength     Length of the uncompressed table, excluding padding.
                //UInt32 origChecksum   Checksum of the uncompressed table.

                WoffTableDirectory table = tables[i];
                reader.BaseStream.Seek(table.offset, SeekOrigin.Begin);

                //indeed, table may be compress or not=> check length of before and after ...
                byte[] compressedBuffer = reader.ReadBytes((int)table.compLength);

                if (compressedBuffer.Length == table.origLength)
                {
                    //not a compress buffer
                    newDecompressedStream.Write(compressedBuffer, 0, compressedBuffer.Length);
                }
                else
                {
                    var decompressedBuffer = new byte[table.origLength];
                    if (!DecompressHandler(compressedBuffer, decompressedBuffer))
                    {
                        //if not pass set to null
                        decompressedBuffer = null;
                    }
                    else
                    {
                        //pass
                        newDecompressedStream.Write(decompressedBuffer, 0, decompressedBuffer.Length);
                    }
                }
            }
            newDecompressedStream.Flush();
            return true;
        }
    }
}
