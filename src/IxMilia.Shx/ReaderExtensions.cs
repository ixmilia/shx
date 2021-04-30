using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IxMilia.Shx
{
    internal static class ReaderExtensions
    {
        public static ushort ReadUInt16LittleEndian(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            var result = (bytes[1] << 8) + bytes[0];
            return (ushort)result;
        }

        public static byte[] ReadBytesUntilNull(this BinaryReader reader)
        {
            var bytes = new List<byte>();
            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }

                bytes.Add(b);
            }

            return bytes.ToArray();
        }

        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }

                var c = (char)b;
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string ReadLine(this BinaryReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var b = reader.ReadByte();
                var c = (char)b;
                sb.Append(c);
                if (c == '\n')
                {
                    break;
                }
            }

            return sb.ToString();
        }
    }
}
