using System;

namespace IxMilia.Shx
{
    internal static class ByteExtensions
    {
        public static byte[] GetUInt16LittleEndian(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            CorrectEndianness(bytes);
            return bytes;
        }

        public static byte[] GetUInt32LittleEndian(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            CorrectEndianness(bytes);
            return bytes;
        }

        private static byte[] CorrectEndianness(byte[] bytes)
        {
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }
    }
}
