using System.Text;

namespace IxMilia.Shx
{
    internal class ByteReader
    {
        public byte[] Data { get; }
        public int Offset { get; private set; }

        public ByteReader(byte[] data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }

        public bool TryReadByte(out byte b)
        {
            if (BytesRemain)
            {
                b = Current;
                Advance();
                return true;
            }

            b = default;
            return false;
        }

        public bool TryReadSByte(out sbyte sb)
        {
            if (TryReadByte(out var b))
            {
                sb = (sbyte)b;
                return true;
            }

            sb = default;
            return false;
        }

        public bool TryReadUInt16LittleEndian(out ushort us)
        {
            if (TryReadByte(out var low) &&
                TryReadByte(out var high))
            {
                us = (ushort)((high << 8) | low);
                return true;
            }

            us = default;
            return false;
        }

        public bool TryReadUInt16BigEndian(out ushort us)
        {
            if (TryReadByte(out var high) &&
                TryReadByte(out var low))
            {
                us = (ushort)((high << 8) | low);
                return true;
            }

            us = default;
            return false;
        }

        public string ReadLine()
        {
            var sb = new StringBuilder();
            while (TryReadByte(out var b))
            {
                var c = (char)b;
                sb.Append(c);
                if (c == '\n')
                {
                    break;
                }
            }

            return sb.ToString();
        }

        public string ReadNullTerminatedString()
        {
            var sb = new StringBuilder();
            while (TryReadByte(out var b))
            {
                if (b == 0)
                {
                    break;
                }

                var c = (char)b;
                sb.Append(c);
            }

            return sb.ToString();
        }

        private bool BytesRemain => Offset < Data.Length;

        private byte Current => Data[Offset];

        private void Advance()
        {
            Offset++;
        }
    }
}
