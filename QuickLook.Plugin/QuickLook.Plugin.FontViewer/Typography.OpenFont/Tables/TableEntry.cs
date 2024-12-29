//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.IO;
namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// this is base class of all 'top' font table
    /// </summary>
    public abstract class TableEntry
    {
        public TableEntry()
        {
        }
        internal TableHeader Header { get; set; }
        protected abstract void ReadContentFrom(BinaryReader reader);
        public abstract string Name { get; }
        internal void LoadDataFrom(BinaryReader reader)
        {
            //ensure that we always start at the correct offset***
            reader.BaseStream.Seek(this.Header.Offset, SeekOrigin.Begin);
            ReadContentFrom(reader);
        }
        public uint TableLength => this.Header.Length;

    }
    class UnreadTableEntry : TableEntry
    {
        public UnreadTableEntry(TableHeader header)
        {
            this.Header = header;
        }
        public override string Name => this.Header.Tag;
        //
        protected sealed override void ReadContentFrom(BinaryReader reader)
        {
            //intend ***
            throw new NotImplementedException();
        }

        public bool HasCustomContentReader { get; protected set; }
        public virtual T CreateTableEntry<T>(BinaryReader reader, T expectedResult)
            where T : TableEntry
        {
            throw new NotImplementedException();
        }
#if DEBUG
        public override string ToString()
        {
            return this.Name;
        }
#endif
    }
}
