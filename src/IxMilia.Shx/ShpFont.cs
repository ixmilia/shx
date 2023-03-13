using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IxMilia.Shx
{
    public class ShpFont
    {
        public Dictionary<ushort, ShpShapeDescription> Shapes { get; } = new Dictionary<ushort, ShpShapeDescription>();

        public string Name { get; set; }
        public double UpperCaseBaselineOffset { get; set; }
        public double LowerCaseBaselineDropOffset { get; set; }
        public ShxFontMode FontMode { get; set; }
        public ShxFontEncoding FontEncoding { get; set; }
        public ShxFontEmbeddingType EmbeddingType { get; set; }

        private ShpFont()
        {
        }

        public ShpFont(string name, double upperCaseBaseLineOffset, double lowerCaseBaseLineOffset, ShxFontMode fontMode)
        {
            Name = name;
            UpperCaseBaselineOffset = upperCaseBaseLineOffset;
            LowerCaseBaselineDropOffset = lowerCaseBaseLineOffset;
            FontMode = fontMode;
        }

        public byte[] Compile()
        {
            var finalBytes = new List<byte>();
            var typeText = FontEncoding == ShxFontEncoding.Unicode ? "unifont" : "bigfont";
            finalBytes.AddRange(Encoding.ASCII.GetBytes($"AutoCAD-86 {typeText} 1.0\r\n")); // file identifier
            finalBytes.Add(0x1A); // unknown, always present

            var content = FontEncoding == ShxFontEncoding.Unicode
                ? CompileUniFont()
                : CompileBigFont(finalBytes.Count);
            finalBytes.AddRange(content);

            return finalBytes.ToArray();
        }

        private byte[] CompileBigFont(int headerLength)
        {
            var finalBytes = new List<byte>();
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(8)); // estimated item count; always 8?
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)(Shapes.Count + 1))); // character count (+1 for font info)

            var rangeCount = 1;
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)rangeCount)); // range count; always 1?
            for (int rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
            {
                finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(128)); // range start; always 128?
                finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(158)); // range end; always 158?
            }

            // compile all character info
            var shapeInfoList = new List<Tuple<ushort, string, byte[]>>();

            // font info is character 0
            var fontInfoBytes = new List<byte>();
            fontInfoBytes.AddRange(Encoding.ASCII.GetBytes(Name));
            fontInfoBytes.Add(0); // name terminator
            fontInfoBytes.Add((byte)UpperCaseBaselineOffset);
            fontInfoBytes.Add((byte)LowerCaseBaselineDropOffset);
            fontInfoBytes.Add((byte)FontMode);
            fontInfoBytes.Add((byte)EmbeddingType);
            shapeInfoList.Add(Tuple.Create((ushort)0, Name, fontInfoBytes.ToArray()));
            foreach (var shape in Shapes.Values.OrderBy(s => s.ShapeNumber))
            {
                shapeInfoList.Add(Tuple.Create(shape.ShapeNumber, shape.Name, shape.Compile(includeHeader: false)));
            }

            // write character table and offsets
            var shapeListSize = (Shapes.Count + 1) * 8; // 2 ushorts and 1 uint each
            var currentCharacterDataOffset = headerLength + finalBytes.Count + shapeListSize;
            var shapeOffsets = new Dictionary<ushort, uint>();
            foreach (var shapeInfoPair in shapeInfoList)
            {
                var shapeCode = shapeInfoPair.Item1;
                var shapeName = shapeInfoPair.Item2;
                var shapeData = shapeInfoPair.Item3;
                finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(shapeCode));
                finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)shapeData.Length));
                finalBytes.AddRange(ByteExtensions.GetUInt32LittleEndian((uint)currentCharacterDataOffset));
                shapeOffsets[shapeCode] = (uint)currentCharacterDataOffset;
                currentCharacterDataOffset += shapeData.Length;
            }

            // write shape data
            foreach (var shapeInfoPair in shapeInfoList)
            {
                var shapeCode = shapeInfoPair.Item1;
                var shapeName = shapeInfoPair.Item2;
                var shapeData = shapeInfoPair.Item3;
                finalBytes.AddRange(shapeData);
            }

            return finalBytes.ToArray();
        }

        private byte[] CompileUniFont()
        {
            var finalBytes = new List<byte>();
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)(Shapes.Count + 1))); // character count (+1 for font info)

            // add font info
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(0)); // shape number
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(6));
            finalBytes.AddRange(Encoding.ASCII.GetBytes(Name));
            finalBytes.Add(0); // name null terminator
            finalBytes.Add((byte)UpperCaseBaselineOffset);
            finalBytes.Add((byte)LowerCaseBaselineDropOffset);
            finalBytes.Add((byte)FontMode);
            finalBytes.Add((byte)FontEncoding);
            finalBytes.Add((byte)EmbeddingType);
            finalBytes.Add(0); // terminator

            // add characters
            foreach (var shape in Shapes.Select(kvp => kvp.Value).OrderBy(s => s.ShapeNumber))
            {
                finalBytes.AddRange(shape.Compile(includeHeader: true));
            }

            return finalBytes.ToArray();
        }

        public static ShpFont Parse(string content)
        {
            var font = new ShpFont();
            var lines = content.Split('\n').Where(l => !string.IsNullOrEmpty(l) && l[0] != ';').Select(l => l.Trim()).ToArray();
            var startingLine = 0;
            var isUnifont = false;
            while (startingLine < lines.Length)
            {
                var shape = ShpShapeDescription.Parse(lines, startingLine, isUnifont, out var nextLine);
                if (shape.ShapeNumber == 0)
                {
                    font.Name = shape.Name;
                    font.UpperCaseBaselineOffset = shape.Data[0];
                    font.LowerCaseBaselineDropOffset = shape.Data[1];
                    font.FontMode = (ShxFontMode)shape.Data[2];
                    switch (shape.Data.Length + 1)
                    {
                        case 4:
                            // bigfont
                            font.FontEncoding = ShxFontEncoding.PackedMultibyte1;
                            font.EmbeddingType = ShxFontEmbeddingType.Embeddable;
                            break;
                        case 6:
                            // unifont
                            isUnifont = true;
                            font.FontEncoding = (ShxFontEncoding)shape.Data[3];
                            font.EmbeddingType = (ShxFontEmbeddingType)shape.Data[4];
                            break;
                        default:
                            throw new NotSupportedException("Unexpected font info byte count");
                    }
                }
                else
                {
                    font.Shapes.Add(shape.ShapeNumber, shape);
                }

                startingLine = nextLine;
            }

            return font;
        }
    }
}
