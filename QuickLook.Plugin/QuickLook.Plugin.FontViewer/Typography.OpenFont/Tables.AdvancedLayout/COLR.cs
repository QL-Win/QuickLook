//Apache2, 2017-present Sam Hocevar <sam@hocevar.net>, WinterDev

using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables
{
    public class COLR : TableEntry
    {
        public const string _N = "COLR";
        public override string Name => _N;

        // Read the COLR table
        // https://docs.microsoft.com/en-us/typography/opentype/spec/colr
        protected override void ReadContentFrom(BinaryReader reader)
        {
            long offset = reader.BaseStream.Position;

            //Type      Name                    Description
            //uint16    version                 Table version number(starts at 0).
            //uint16    numBaseGlyphRecords     Number of Base Glyph Records.
            //Offset32  baseGlyphRecordsOffset  Offset(from beginning of COLR table) to Base Glyph records.
            //Offset32  layerRecordsOffset      Offset(from beginning of COLR table) to Layer Records.
            //uint16    numLayerRecords         Number of Layer Records.


            ushort version = reader.ReadUInt16();
            ushort numBaseGlyphRecords = reader.ReadUInt16();
            uint baseGlyphRecordsOffset = reader.ReadUInt32();
            uint layerRecordsOffset = reader.ReadUInt32();
            ushort numLayerRecords = reader.ReadUInt16();

            GlyphLayers = new ushort[numLayerRecords];
            GlyphPalettes = new ushort[numLayerRecords];

            reader.BaseStream.Seek(offset + baseGlyphRecordsOffset, SeekOrigin.Begin);
            for (int i = 0; i < numBaseGlyphRecords; ++i)
            {
                ushort gid = reader.ReadUInt16();
                LayerIndices[gid] = reader.ReadUInt16();
                LayerCounts[gid] = reader.ReadUInt16();
            }

            reader.BaseStream.Seek(offset + layerRecordsOffset, SeekOrigin.Begin);
            for (int i = 0; i < GlyphLayers.Length; ++i)
            {
                GlyphLayers[i] = reader.ReadUInt16();
                GlyphPalettes[i] = reader.ReadUInt16();
            }
        }

        public ushort[] GlyphLayers { get; private set; }
        public ushort[] GlyphPalettes { get; private set; }
        public readonly Dictionary<ushort, ushort> LayerIndices = new Dictionary<ushort, ushort>();
        public readonly Dictionary<ushort, ushort> LayerCounts = new Dictionary<ushort, ushort>();
    }
}

