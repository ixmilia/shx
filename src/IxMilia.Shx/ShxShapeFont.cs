using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Shx
{
    public class ShxShapeFont : ShxFont
    {
        public override ShxFontType FontType => ShxFontType.ShapeFile;

        public ShxShapeFont()
            : base()
        {
        }

        internal void TryReadFontData(ByteReader reader, int totalBytesToRead)
        {
            var startOffset = reader.Offset;
            Name = reader.ReadNullTerminatedString();
            FontEncoding = ShxFontEncoding.PackedMultibyte1;

            var readBytes = reader.Offset - startOffset;
            var remainingBytes = totalBytesToRead - readBytes;

            reader.TryReadByte(out var b1); // character height
            reader.TryReadByte(out var b2); // lower case offset | 0
            reader.TryReadByte(out var b3); // font mode
            reader.TryReadByte(out var _nullTerminator);
            UpperCaseBaselineOffset = b1;
            LowerCaseBaselineDropOffset = b2;
            FontMode = (ShxFontMode)b3;
        }

        internal override ShxGlyphCommandData Load(ByteReader reader)
        {
            var characterOrder = new List<ushort>();
            var shapeDataLengths = new Dictionary<ushort, int>();
            var commandData = new ShxGlyphCommandData();
            if (reader.TryReadUInt16LittleEndian(out var startCode) &&
                reader.TryReadUInt16LittleEndian(out var dataStart) &&
                reader.TryReadUInt16LittleEndian(out var characterCount))
            {
                for (int i = 0; i < characterCount; i++)
                {
                    reader.TryReadUInt16LittleEndian(out var characterCode);
                    reader.TryReadUInt16LittleEndian(out var characterDataLength);
                    characterOrder.Add(characterCode);
                    shapeDataLengths.Add(characterCode, characterDataLength);
                }

                for (int i = 0; i < characterCount; i++)
                {
                    var characterCode = characterOrder[i];
                    var characterByteCount = shapeDataLengths[characterCode];
                    var startPos = reader.Offset;
                    var expectedEnd = startPos + characterByteCount;
                    if (characterCode == 0)
                    {
                        TryReadFontData(reader, characterByteCount);
                    }
                    else
                    {
                        var character = (char)characterCode;
                        var glyphName = reader.ReadNullTerminatedString();
                        if (string.IsNullOrEmpty(glyphName))
                        {
                            glyphName = character.ToString();
                        }

                        var glyphCommands = ShxGlyph.ParseCommands(reader, FontEncoding, FontType);
                        commandData.AddGlyphCommands(characterCode, glyphName, glyphCommands);
                    }

                    var remainingBytes = Math.Max(0, expectedEnd - reader.Offset);
                    Debug.Assert(remainingBytes == 0);
                }
            }

            return commandData;
        }
    }
}
