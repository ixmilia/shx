using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IxMilia.Shx
{
    public enum ShxFontMode
    {
        HorizontalOnly = 0,
        HorizontalAndVertical = 2,
    }

    public enum ShxFontEncoding
    {
        Unicode = 0,
        PackedMultibyte1 = 1,
        ShapeFile = 2,
    }

    public enum ShxFontEmbeddingType
    {
        Embeddable = 0,
        NotEmbeddable = 1,
        ReadOnlyEmbeddable = 2,
    }

    public class ShxFont
    {
        Dictionary<char, ShxGlyph> _glyphs = new Dictionary<char, ShxGlyph>();

        public IReadOnlyDictionary<char, ShxGlyph> Glyphs => _glyphs;
        public string FileIdentifier { get; private set; }
        public string Name { get; private set; }
        public double UpperCaseBaselineOffset { get; private set; }
        public double LowerCaseBaselineDropOffset { get; private set; }
        public ShxFontMode FontMode { get; private set; }
        public ShxFontEncoding FontEncoding { get; private set; }
        public ShxFontEmbeddingType EmbeddingType { get; private set; }

        private void AddGlyph(char code, ShxGlyph glyph) => _glyphs.Add(code, glyph);

        public ShxFont()
        {
        }

        public static ShxFont Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return Load(stream);
            }
        }

        public static ShxFont Load(Stream stream)
        {
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return Load(buffer);
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

        public static ShxFont Load(byte[] data)
        {
            var font = new ShxFont();
            var names = new Dictionary<ushort, string>();
            var commands = new Dictionary<ushort, IEnumerable<ShxGlyphCommand>>();
            var reader = new ByteReader(data);
            font.FileIdentifier = reader.ReadLine();
            if (!font.FileIdentifier.Contains("unifont"))
            {
                // TODO: unsupported
                return font;
            }

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
                            font.TryReadFontData(reader);
                        }
                        else
                        {
                            var character = (char)characterCode;
                            var glyphName = reader.ReadNullTerminatedString();
                            if (string.IsNullOrEmpty(glyphName))
                            {
                                glyphName = character.ToString();
                            }

                            var glyphCommands = ShxGlyph.ParseCommands(reader, font.FontEncoding);
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
                font.AddGlyph(character, glyph);
            }

            return font;
        }
    }
}
