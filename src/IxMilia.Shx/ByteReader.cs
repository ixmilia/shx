using System;
using System.Collections.Generic;
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

        public bool TryReadUInt16(out ushort us)
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

        private bool BytesRemain => Offset < Data.Length;

        private byte Current => Data[Offset];

        private void Advance()
        {
            Offset++;
        }
    }
}
