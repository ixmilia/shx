using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Shx
{
    public class ShxUniFont : ShxFont
    {
        public ShxUniFont()
            : base()
        {
        }

        internal override void Load(ByteReader reader)
        {
            var names = new Dictionary<ushort, string>();
            var commands = new Dictionary<ushort, IEnumerable<ShxGlyphCommand>>();

            if (reader.TryReadByte(out var _) && // always 26 (0x1A)?
                reader.TryReadUInt16LittleEndian(out var characterCount))
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

                            var glyphCommands = ShxGlyph.ParseCommands(reader, FontEncoding);
                            commands.Add(characterCode, glyphCommands);
                            names.Add(character, glyphName);
                        }

                        var remainingBytes = Math.Max(0, expectedEnd - reader.Offset);
                        Debug.Assert(remainingBytes == 0);
                    }
                }
            }

            foreach (var kvp in commands)
            {
                var character = (char)kvp.Key;
                var glyphCommands = kvp.Value;
                var glyph = ShxCommandProcessor.Process(names[kvp.Key], glyphCommands, commands);
                AddGlyph(character, glyph);
            }
        }
    }
}
