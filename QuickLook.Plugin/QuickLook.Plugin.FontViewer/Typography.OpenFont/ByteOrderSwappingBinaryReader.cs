//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.IO;

namespace Typography.OpenFont
{
    class ByteOrderSwappingBinaryReader : BinaryReader
    {
        //All OpenType fonts use Motorola-style byte ordering (Big Endian)
        //
        public ByteOrderSwappingBinaryReader(Stream input)
            : base(input)
        {
        }
        protected override void Dispose(bool disposing)
        {
            GC.SuppressFinalize(this);
            base.Dispose(disposing);
        }
        //
        //as original
        //
        //public override byte ReadByte() { return base.ReadByte(); } 
        // 
        //we override the 4 methods here
        //
        public override short ReadInt16() => BitConverter.ToInt16(RR(2), 8 - 2);
        public override ushort ReadUInt16() => BitConverter.ToUInt16(RR(2), 8 - 2);
        public override uint ReadUInt32() => BitConverter.ToUInt32(RR(4), 8 - 4);
        public override ulong ReadUInt64() => BitConverter.ToUInt64(RR(8), 8 - 8);


        //used in CFF font
        public override double ReadDouble() => BitConverter.ToDouble(RR(8), 8 - 8);
        //used in CFF font
        public override int ReadInt32() => BitConverter.ToInt32(RR(4), 8 - 4);

        //
        readonly byte[] _reusable_buffer = new byte[8]; //fix buffer size to 8 bytes
        /// <summary>
        /// read and reverse 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private byte[] RR(int count)
        {
            base.Read(_reusable_buffer, 0, count);
            Array.Reverse(_reusable_buffer);
            return _reusable_buffer;
        }

        //we don't use these methods in our OpenFont, so => throw the exception
        public override int PeekChar() { throw new NotImplementedException(); }
        public override int Read() { throw new NotImplementedException(); }
        public override int Read(byte[] buffer, int index, int count) => base.Read(buffer, index, count);
        public override int Read(char[] buffer, int index, int count) { throw new NotImplementedException(); }
        public override bool ReadBoolean() { throw new NotImplementedException(); }
        public override char ReadChar() { throw new NotImplementedException(); }
        public override char[] ReadChars(int count) { throw new NotImplementedException(); }
        public override decimal ReadDecimal() { throw new NotImplementedException(); }

        public override long ReadInt64() { throw new NotImplementedException(); }
        public override sbyte ReadSByte() { throw new NotImplementedException(); }
        public override float ReadSingle() { throw new NotImplementedException(); }
        public override string ReadString() { throw new NotImplementedException(); }
        //

    }
}
