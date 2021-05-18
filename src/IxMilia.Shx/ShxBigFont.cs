using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Shx
{
    public class ShxBigFont : ShxFont
    {
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
            reader.TryReadByte(out var b4); // embedding type | width
            if (remainingBytes > 4)
            {
                reader.TryReadByte(out var _); // unused
            }

            if (remainingBytes == 4)
            {
                UpperCaseBaselineOffset = b1;
                LowerCaseBaselineDropOffset = b2;
                FontMode = (ShxFontMode)b3;
                EmbeddingType = (ShxFontEmbeddingType)b4;
            }
            else
            {
                UpperCaseBaselineOffset = b1;
                FontMode = (ShxFontMode)b3;
                CharacterWidth = b4;
            }
        }

        // TODO: read code page from "MstnFontConfig.xml": FontConfig/Fonts/ShxFontInfo/Name|CodePage
        internal override ShxGlyphCommandData Load(ByteReader reader)
        {
            var commandData = new ShxGlyphCommandData();
            var twoByteRangePrefixes = new HashSet<Tuple<byte, byte>>();

            reader.TryReadUInt16LittleEndian(out var estimatedItemCount);
            reader.TryReadUInt16LittleEndian(out var characterCount);
            reader.TryReadUInt16LittleEndian(out var rangeCount);
            for (int i = 0; i < rangeCount; i++)
            {
                reader.TryReadUInt16LittleEndian(out var rangeStart);
                reader.TryReadUInt16LittleEndian(out var rangeEnd);
                twoByteRangePrefixes.Add(Tuple.Create((byte)rangeStart, (byte)rangeEnd));
                var rangeStartHex = rangeStart.ToString("X");
                var rangeEndHex = rangeEnd.ToString("X");
            }

            for (int i = 0; i < characterCount; i++)
            {
                if (reader.TryReadUInt16LittleEndian(out var characterCode) &&
                    reader.TryReadUInt16LittleEndian(out var characterByteCount) &&
                    reader.TryReadUInt32LittleEndian(out var characterOffset))
                {
                    if (characterCode == 0 &&
                        characterByteCount == 0 &&
                        characterOffset == 0)
                    {
                        // occasional null entries
                        continue;
                    }

                    var characterReader = reader.FromOffset((int)characterOffset);
                    var startPos = characterReader.Offset;
                    var expectedEnd = startPos + characterByteCount;
                    if (characterCode == 0)
                    {
                        TryReadFontData(characterReader, characterByteCount);
                    }
                    else
                    {
                        var glyphName = characterReader.ReadNullTerminatedString();
                        var glyphCommands = ShxGlyph.ParseCommands(characterReader, FontEncoding, isBigFont: true);
                        commandData.AddGlyphCommands(characterCode, glyphName, glyphCommands);
                    }

                    var remainingBytes = expectedEnd - characterReader.Offset;
                    Debug.Assert(remainingBytes == 0);
                }
            }

            return commandData;
        }
    }
}
