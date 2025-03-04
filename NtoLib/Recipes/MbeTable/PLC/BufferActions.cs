using System;
using System.Buffers.Binary;

namespace NtoLib.Recipes.MbeTable.PLC
{
    internal static class BufferActions
    {
        public static void WriteDWordToBuffer(byte[] buf, ref int offset, uint data)
        {
            BinaryPrimitives.WriteUInt32BigEndian(buf.AsSpan(offset), data);
            offset += 4;
        }

        public static void WriteWordToBuffer(byte[] buf, ref int offset, ushort data)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(offset), data);
            offset += 2;
        }

        public static void WriteByteToBuffer(byte[] buf, ref int offset, byte data)
        {
            buf[offset++] = data;
        }

        public static ushort ReadDWordFromBuffer(byte[] buf, ref int offset)
        {
            var result = (ushort)BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(offset));
            offset += 4;
            return result;
        }

        public static ushort ReadWordToBuffer(byte[] buf, ref int offset)
        {
            var result = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(offset));
            offset += 2;
            return result;
        }

        public static byte ReadByteFromBuffer(byte[] buf, ref int offset)
        {
            return buf[offset++];
        }
    }
}
