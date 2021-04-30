using System;
using System.Collections.Generic;
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
        public ShxFontEmbeddingType EmbeddintType { get; private set; }

        private void AddGlyph(char code, ShxGlyph glyph) => _glyphs.Add(code, glyph);

        public ShxFont()
        {
        }

        public static ShxFont Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return Load(stream);
            }
        }

        public static ShxFont Load(Stream stream)
        {
            var font = new ShxFont();
            var names = new Dictionary<ushort, string>();
            var commands = new Dictionary<ushort, IEnumerable<ShxGlyphCommand>>();
            using (var reader = new BinaryReader(stream))
            {
                font.FileIdentifier = reader.ReadLine();
                var _ = reader.ReadByte(); // always 26 (0x1A)?
                var characterCount = reader.ReadUInt16LittleEndian();
                for (int i = 0; i < characterCount; i++)
                {
                    var characterCode = reader.ReadUInt16LittleEndian();
                    var characterByteCount = reader.ReadUInt16LittleEndian();
                    var startPos = reader.BaseStream.Position;
                    var expectedEnd = startPos + characterByteCount;
                    if (characterCode == 0)
                    {
                        font.Name = reader.ReadNullTerminatedString();
                        font.UpperCaseBaselineOffset = reader.ReadByte();
                        font.LowerCaseBaselineDropOffset = reader.ReadByte();
                        font.FontMode = (ShxFontMode)reader.ReadByte();
                        font.FontEncoding = (ShxFontEncoding)reader.ReadByte();
                        font.EmbeddintType = (ShxFontEmbeddingType)reader.ReadByte();
                        var unknown = reader.ReadByte();
                    }
                    else
                    {
                        var character = (char)characterCode;
                        var glyphName = reader.ReadNullTerminatedString();
                        if (string.IsNullOrEmpty(glyphName))
                        {
                            glyphName = character.ToString();
                        }

                        var glyphData = reader.ReadBytesUntilNull();
                        var glyphCommands = ShxGlyph.ParseCommands(glyphData, font.FontEncoding);
                        commands.Add(characterCode, glyphCommands);
                        names.Add(character, glyphName);
                    }

                    var remainingBytes = Math.Max(0, expectedEnd - reader.BaseStream.Position);
                    var unused = reader.ReadBytes((int)remainingBytes);
                    if (remainingBytes > 0)
                    {

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
