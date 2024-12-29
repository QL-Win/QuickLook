//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/fvar
    //'fvar' Header 
    //The format of the font variations table header is as follows. 

    //Note: The 'fvar' table describes a font’s variation space, 
    //and other variation tables provide variation data to describe 
    //how different data items are varied across the font’s variation space

    /// <summary>
    /// fvar font variations
    /// </summary>
    class FVar : TableEntry
    {
        public const string _N = "fvar";
        public override string Name => _N;


        public VariableAxisRecord[] variableAxisRecords;
        public InstanceRecord[] instanceRecords;

        //
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //Font variations header:

            //Type      Name            Description
            //uint16    majorVersion    Major version number of the font variations table — set to 1.
            //uint16    minorVersion    Minor version number of the font variations table — set to 0.
            //Offset16  axesArrayOffset Offset in bytes from the beginning of the table to the start of the VariationAxisRecord array.
            //uint16    (reserved)      This field is permanently reserved.Set to 2.
            //uint16    axisCount       The number of variation axes in the font (the number of records in the axes array).
            //uint16    axisSize        The size in bytes of each VariationAxisRecord — set to 20 (0x0014) for this version.
            //uint16    instanceCount   The number of named instances defined in the font (the number of records in the instances array).
            //uint16    instanceSize    The size in bytes of each InstanceRecord — set to either axisCount * sizeof(Fixed) + 4, 
            //                          or to axisCount * sizeof(Fixed) + 6.


            long beginAt = reader.BaseStream.Position;
            //header:
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort axesArrayOffset = reader.ReadUInt16();
            ushort reserved = reader.ReadUInt16();//set to 2 
            ushort axisCount = reader.ReadUInt16();
            ushort axisSize = reader.ReadUInt16();
            ushort instanceCount = reader.ReadUInt16();
            ushort instanceSize = reader.ReadUInt16();


            //The header is followed by axes and instances arrays. 
            //The location of the axes array is specified in the axesArrayOffset field;
            //the instances array directly follows the axes array.
            //Type                 Name                         Description
            //VariationAxisRecord  axes[axisCount]              The variation axis array.
            //InstanceRecord       instances[instanceCount]     The named instance array.

            //Note: The axisSize and instanceSize fields indicate 
            //the size of the VariationAxisRecord and InstanceRecord structures. 

            //In this version of the 'fvar' table, the InstanceRecord structure has an optional field,
            //and so two different size formulations are possible. 

            //Future minor-version updates of the 'fvar' table may define compatible extensions to either formats. 

            //***Implementations must use the axisSize and instanceSize fields to determine the start of each record.***

            //The set of axes that make up the font’s variation space are defined by an array of variation axis records.
            //The number of records, and the number of axes, is determined by the axisCount field.
            //A functional variable font must have an axisCount value that is greater than zero.

            //If axisCount is zero, then the font is not functional as a variable font, ***
            //and must be treated as a non-variable font; ***
            //any variation-specific tables or data is ignored.

            variableAxisRecords = new VariableAxisRecord[axisCount];

            for (int i = 0; i < axisCount; ++i)
            {
                long pos = reader.BaseStream.Position;
                VariableAxisRecord varAxisRecord = new VariableAxisRecord();
                varAxisRecord.ReadContent(reader);
                variableAxisRecords[i] = varAxisRecord;
                if (reader.BaseStream.Position != pos + axisSize)
                {
                    //***Implementations must use the axisSize and instanceSize fields to determine the start of each record.***
                    reader.BaseStream.Position = pos + axisSize;
                }
            }

            instanceRecords = new InstanceRecord[instanceCount];

            for (int i = 0; i < instanceCount; ++i)
            {
                long pos = reader.BaseStream.Position;

                InstanceRecord instanecRec = new InstanceRecord();
                instanecRec.ReadContent(reader, axisCount, instanceSize);

                if (reader.BaseStream.Position != pos + instanceSize)
                {
                    //***Implementations must use the axisSize and instanceSize fields to determine the start of each record.***
                    reader.BaseStream.Position = pos + instanceSize;
                }
            }

        }

        public class VariableAxisRecord
        {
            //VariationAxisRecord

            //The format of the variation axis record is as follows:

            //VariationAxisRecord
            //Type      Name        Description
            //Tag       axisTag     Tag identifying the design variation for the axis.
            //Fixed     minValue    The minimum coordinate value for the axis.
            //Fixed     defaultValue    The default coordinate value for the axis.
            //Fixed     maxValue        The maximum coordinate value for the axis.
            //uint16    flags           Axis qualifiers — see details below.
            //uint16    axisNameID      The name ID for entries in the 'name' table that provide a display name for this axis.


            public string axisTag;
            public float minValue;
            public float defaultValue;
            public float maxValue;
            public ushort flags;
            public ushort axisNameID;
            public void ReadContent(BinaryReader reader)
            {
                axisTag = Utils.TagToString(reader.ReadUInt32());//4
                minValue = reader.ReadFixed();//4
                defaultValue = reader.ReadFixed();//4
                maxValue = reader.ReadFixed();//4
                flags = reader.ReadUInt16();//2
                axisNameID = reader.ReadUInt16();//2
                //

            }
        }

        public class InstanceRecord
        {
            //InstanceRecord

            //The instance record format includes an array of n-tuple coordinate arrays 
            //that define position within the font’s variation space.
            //The n-tuple array has the following format:

            //Tuple Record(Fixed):
            //Type    Name                      Description
            //Fixed   coordinates[axisCount]    Coordinate array specifying a position within the font’s variation space.

            //The format of the instance record is as follows.

            //InstanceRecord:
            //Type      Name                Description
            //uint16    subfamilyNameID     The name ID for entries in the 'name' table that provide subfamily names for this instance.
            //uint16    flags               Reserved for future use — set to 0.
            //Tuple     coordinates         The coordinates array for this instance.
            //uint16    postScriptNameID    Optional.The name ID for entries in the 'name' table that provide PostScript names for this instance.

            public ushort subfamilyNameID;//point to name table, will be resolved later
            public ushort flags;
            public TupleRecord coordinates;
            public ushort postScriptNameID;//point to name table, will be resolved later

            public void ReadContent(BinaryReader reader, int axisCount, int instanceRecordSize)
            {
                long expectedEndPos = reader.BaseStream.Position + instanceRecordSize;
                subfamilyNameID = reader.ReadUInt16();
                flags = reader.ReadUInt16();
                float[] coords = new float[axisCount];
                for (int i = 0; i < axisCount; ++i)
                {
                    coords[i] = reader.ReadFixed();
                }
                coordinates = new TupleRecord(coords);

                if (reader.BaseStream.Position < expectedEndPos)
                {
                    //optional field
                    postScriptNameID = reader.ReadUInt16();
                }

            }
        }

    }
}



