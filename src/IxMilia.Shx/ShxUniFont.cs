using System;
using System.Diagnostics;

namespace IxMilia.Shx
{
    public class ShxUniFont : ShxFont
    {
        public override ShxFontType FontType => ShxFontType.UniFont;
        
        public ShxUniFont()
            : base()
        {
        }

        internal bool TryReadFontData(ByteReader reader)
        {
            Name = reader.ReadNullTerminatedString();
            if (reader.TryReadByte(out var upperCaseBaselineOffset) &&
                reader.TryReadByte(out var lowerCaseBaselineOffset) &&
                reader.TryReadByte(out var fontMode) &&
                reader.TryReadByte(out var fontEncoding) &&
                reader.TryReadByte(out var embeddingType) &&
                reader.TryReadByte(out var unknown))
            {
                UpperCaseBaselineOffset = upperCaseBaselineOffset;
                LowerCaseBaselineDropOffset = lowerCaseBaselineOffset;
                FontMode = (ShxFontMode)fontMode;
                FontEncoding = (ShxFontEncoding)fontEncoding;
                EmbeddingType = (ShxFontEmbeddingType)embeddingType;
                return true;
            }

            return false;
        }

        internal override ShxGlyphCommandData Load(ByteReader reader)
        {
            var commandData = new ShxGlyphCommandData();
            if (reader.TryReadUInt16LittleEndian(out var characterCount))
            {
                for (int i = 0; i < characterCount; i++)
                {
                    if (reader.TryReadUInt16LittleEndian(out var characterCode) &&
                        reader.TryReadUInt16LittleEndian(out var characterByteCount))
                    {
                        var startPos = reader.Offset;
                        var expectedEnd = startPos + characterByteCount;
                        if (characterCode == 0)
                        {
                            TryReadFontData(reader);
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
            }

            return commandData;
        }
    }
}
