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

        public ByteReader FromOffset(int offset)
        {
            return new ByteReader(Data, offset);
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

        public static ushort CombineLittleEndian(byte b1, byte b2)
        {
            var result = (ushort)((b2 << 8) | b1);
            return result;
        }

        public bool TryReadUInt16LittleEndian(out ushort us)
        {
            if (TryReadByte(out var low) &&
                TryReadByte(out var high))
            {
                us = CombineLittleEndian(low, high);
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

        public bool TryReadUInt32LittleEndian(out uint ui)
        {
            if (TryReadByte(out var a) &&
                TryReadByte(out var b) &&
                TryReadByte(out var c) &&
                TryReadByte(out var d))
            {
                ui = (uint)((d << 24) | (c << 16) | (b << 8) | a);
                return true;
            }

            ui = default;
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
