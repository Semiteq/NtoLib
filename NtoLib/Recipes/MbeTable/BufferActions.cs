namespace NtoLib.Recipes.MbeTable
{
    internal static class BufferActions
    {
        public static void WriteDWordToBuffer(byte[] buf, ref int offset, uint data)
        {
            buf[offset] = (byte)(data >> 24 & (uint)byte.MaxValue);
            buf[offset + 1] = (byte)(data >> 16 & (uint)byte.MaxValue);
            buf[offset + 2] = (byte)(data >> 8 & (uint)byte.MaxValue);
            buf[offset + 3] = (byte)(data & (uint)byte.MaxValue);
            offset += 4;
        }
        public static void WriteWordToBuffer(byte[] buf, ref int offset, ushort data)
        {
            buf[offset] = (byte)((int)data >> 8 & (int)byte.MaxValue);
            buf[offset + 1] = (byte)((uint)data & (uint)byte.MaxValue);
            offset += 2;
        }
        public static void WriteByteToBuffer(byte[] buf, ref int offset, byte data)
        {
            buf[offset] = (byte)((uint)data & (uint)byte.MaxValue);
            ++offset;
        }



        public static ushort ReadDWordFromBuffer(byte[] buf, ref int offset)
        {
            ushort num = (ushort)((uint)(ushort)(0U + (uint)(ushort)((uint)buf[offset] << 8)) + (uint)(ushort)buf[offset + 1]);
            offset += 2;
            return num;
        }
        public static byte ReadByteFromBuffer(byte[] buf, ref int offset)
        {
            byte num = buf[offset];
            ++offset;
            return num;
        }



        public static uint ReadDwordToBuffer(byte[] buf, ref int offset)
        {
            uint num = 0U | (uint)buf[offset] | (uint)buf[offset + 1] << 8 | (uint)buf[offset + 2] << 16 | (uint)buf[offset + 3] << 24;
            offset += 4;
            return num;
        }
        public static ushort ReadWordToBuffer(byte[] buf, ref int offset)
        {
            ushort num = (ushort)((uint)(ushort)(0U | (uint)(ushort)buf[offset]) | (uint)(ushort)((uint)buf[offset + 1] << 8));
            offset += 2;
            return num;
        }
        public static byte ReadByteToBuffer(byte[] buf, ref int offset)
        {
            byte num = buf[offset];
            ++offset;
            return num;
        }
    }
}
