using System.IO;

namespace Typography.OpenFont;

internal static class BinaryReaderExtensions
{
    public static ushort ReadUInt16BE(this BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    public static uint ReadUInt32BE(this BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }
}
