using System;

namespace IxMilia.Shx
{
    internal static class ByteExtensions
    {
        public static byte[] GetUInt16LittleEndian(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                return bytes;
            }
            else
            {
                return new byte[] { bytes[1], bytes[0] };
            }
        }
    }
}
