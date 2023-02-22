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
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian((ushort)(Shapes.Count + 1))); // character count (+1 for font info)

            // add font info
            finalBytes.Add(0); // shape number
            finalBytes.Add(0);

            var contentLength = (ushort)(FontEncoding == ShxFontEncoding.Unicode ? 6 : 4);
            finalBytes.AddRange(ByteExtensions.GetUInt16LittleEndian(contentLength));
            finalBytes.AddRange(Encoding.ASCII.GetBytes(Name));
            finalBytes.Add(0); // name null terminator
            finalBytes.Add((byte)UpperCaseBaselineOffset);
            finalBytes.Add((byte)LowerCaseBaselineDropOffset);
            finalBytes.Add((byte)FontMode);
            if (FontEncoding == ShxFontEncoding.Unicode)
            {
                finalBytes.Add((byte)FontEncoding);
                finalBytes.Add((byte)EmbeddingType);
            }

            finalBytes.Add(0); // terminator

            // add characters
            foreach (var shape in Shapes.Select(kvp => kvp.Value).OrderBy(s => s.ShapeNumber))
            {
                finalBytes.AddRange(shape.Compile());
            }

            return finalBytes.ToArray();
        }

        public static ShpFont Parse(string content)
        {
            var font = new ShpFont();
            var lines = content.Split('\n').Where(l => !string.IsNullOrEmpty(l) && l[0] != ';').Select(l => l.Trim()).ToArray();
            var startingLine = 0;
            while (startingLine < lines.Length)
            {
                var shape = ShpShapeDescription.Parse(lines, startingLine, out var nextLine);
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
