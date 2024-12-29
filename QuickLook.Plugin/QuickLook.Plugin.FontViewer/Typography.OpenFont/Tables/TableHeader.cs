//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

namespace Typography.OpenFont.Tables
{
    class TableHeader
    {
        readonly uint _tag;

        public TableHeader(uint tag, uint checkSum, uint offset, uint len)
        {
            _tag = tag;
            CheckSum = checkSum;
            Offset = offset;
            Length = len;
            Tag = Utils.TagToString(_tag);
        }
        public TableHeader(string tag, uint checkSum, uint offset, uint len)
        {
            _tag = 0;
            CheckSum = checkSum;
            Offset = offset;
            Length = len;
            Tag = tag;
        }
        //
        public string Tag { get; }
        public uint Offset { get; }
        public uint CheckSum { get; }
        public uint Length { get; }

        public TableHeader Clone() => (_tag != 0) ? new TableHeader(_tag, CheckSum, Offset, Length) : new TableHeader(Tag, CheckSum, Offset, Length);
#if DEBUG
        public override string ToString()
        {
            return "{" + Tag + "}";
        }
#endif
    }
}
