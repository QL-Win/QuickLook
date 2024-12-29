//MIT, 2015-2016, Michael Popoloski, WinterDev

using System.IO;
namespace Typography.OpenFont.Tables
{

    class CvtTable : TableEntry
    {
        public const string _N = "cvt ";//need 4 chars//***
        public override string Name => _N;

        //

        /// <summary>
        /// control value in font unit
        /// </summary>
        internal int[] _controlValues;
        protected override void ReadContentFrom(BinaryReader reader)
        {
            int nelems = (int)(this.TableLength / sizeof(short));
            var results = new int[nelems];
            for (int i = 0; i < nelems; i++)
            {
                results[i] = reader.ReadInt16();
            }
            _controlValues = results;
        }
    }
    class PrepTable : TableEntry
    {
        public const string _N = "prep";
        public override string Name => _N;
        //

        internal byte[] _programBuffer;
        //
        protected override void ReadContentFrom(BinaryReader reader)
        {
            _programBuffer = reader.ReadBytes((int)this.TableLength);
        }
    }
    class FpgmTable : TableEntry
    {
        public const string _N = "fpgm";
        public override string Name => _N;
        //

        internal byte[] _programBuffer;
        protected override void ReadContentFrom(BinaryReader reader)
        {
            _programBuffer = reader.ReadBytes((int)this.TableLength);
        }
    }
}