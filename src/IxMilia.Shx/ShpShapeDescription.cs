using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace IxMilia.Shx
{
    public class ShpShapeDescription
    {
        public ushort ShapeNumber { get; }
        public string Name { get; }
        public byte[] Data { get; set; }

        public ShpShapeDescription(ushort shapeNumber, string name, byte[] data)
        {
            ShapeNumber = shapeNumber;
            Name = name;
            Data = data;
        }

        public byte[] Compile()
        {
            var shapeBytes = new List<byte>();
            shapeBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(ShapeNumber));
            shapeBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)Data.Length));
            shapeBytes.AddRange(Encoding.ASCII.GetBytes(Name));
            shapeBytes.Add(0); // null terminator
            shapeBytes.AddRange(Data);
            shapeBytes.Add(0); // end of shape
            return shapeBytes.ToArray();
        }

        public override string ToString()
        {
            return $"*{ShapeNumber},{Data.Length + 1},{Name}\n{string.Join(",",Data.Select(b => "0" + $"{b:X}".PadLeft(2, '0')))},0";
        }

        internal static ShpShapeDescription Parse(string[] lines, int startingLine, out int nextLine)
        {
            var parts = lines[startingLine].Split(new[] { ',' }, 3);
            if (parts[0][0] != '*')
            {
                throw new InvalidOperationException("Shape number must begin with an asterisk");
            }

            var shapeNumberText = parts[0].Substring(1);
            var shapeNumber = shapeNumberText == "UNIFONT" // special case; this is dealt with elsewhere
                ? (ushort)0
                : shapeNumberText[0] == '0' && shapeNumberText.Length > 1
                    ? ushort.Parse(shapeNumberText, NumberStyles.HexNumber)
                    : ushort.Parse(shapeNumberText);
            var dataLengthPlusTerminator = int.Parse(parts[1]);
            var name = parts[2].Trim();

            if (shapeNumber == 0)
            {
                if (shapeNumberText == "UNIFONT")
                {
                    if (dataLengthPlusTerminator != 6)
                    {
                        throw new InvalidOperationException("UNIFONT must have a data length of 6");
                    }
                }
                else
                {
                    if (dataLengthPlusTerminator != 4)
                    {
                        throw new InvalidOperationException("Bigfont must have a data length of 4");
                    }
                }
            }

            // data
            var data = new List<byte>();
            for (nextLine = startingLine + 1; nextLine < lines.Length; nextLine++)
            {
                if (data.Count >= dataLengthPlusTerminator)
                {
                    break;
                }

                parts = lines[nextLine].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (part.StartsWith("("))
                    {
                        part = part.Substring(1);
                    }
                    if (part.EndsWith(")"))
                    {
                        part = part.Substring(0, part.Length - 1);
                    }

                    if (part[0] == '0' && part.Length > 1)
                    {
                        // hex encoded
                        for (int j = 1; j < part.Length; j += 2)
                        {
                            data.Add(byte.Parse(part.Substring(j, 2), NumberStyles.HexNumber));
                        }
                    }
                    else
                    {
                        // decimal encoded
                        data.Add(byte.Parse(part));
                    }
                }
            }

            if (data.Count != dataLengthPlusTerminator)
            {
                throw new InvalidOperationException("Data length does not match expected length");
            }

            // remove terminator
            if (data[data.Count - 1] != 0)
            {
                throw new InvalidOperationException("Last data byte must be a zero");
            }

            data.RemoveAt(data.Count - 1);
            return new ShpShapeDescription(shapeNumber, name, data.ToArray());
        }
    }
}
