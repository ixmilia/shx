using System.Collections.Generic;
using System.IO;

namespace IxMilia.Shx
{
    public enum ShxFontMode
    {
        HorizontalOnly = 0,
        VerticalOnly = 1,
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

    public enum ShxFontType
    {
        ShapeFile,
        BigFont,
        UniFont
    }

    public abstract class ShxFont
    {
        Dictionary<ushort, ShxGlyph> _glyphs = new Dictionary<ushort, ShxGlyph>();

        public abstract ShxFontType FontType { get; }

        public IReadOnlyDictionary<ushort, ShxGlyph> Glyphs => _glyphs;
        public string FileIdentifier { get; private set; }
        public string Name { get; protected set; }
        public double UpperCaseBaselineOffset { get; protected set; }
        public double LowerCaseBaselineDropOffset { get; protected set; }
        public double CharacterWidth { get; protected set; }
        public ShxFontMode FontMode { get; protected set; }
        public ShxFontEncoding FontEncoding { get; protected set; }
        public ShxFontEmbeddingType EmbeddingType { get; protected set; }

        private void AddGlyph(ushort code, ShxGlyph glyph) => _glyphs.Add(code, glyph);

        protected ShxFont()
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

        internal abstract ShxGlyphCommandData Load(ByteReader reader);

        public static ShxFont Load(byte[] data)
        {
            var reader = new ByteReader(data);
            var fileIdentifier = reader.ReadLine();
            ShxFont font = null;
            if (fileIdentifier.Contains("unifont"))
            {
                font = new ShxUniFont();
            }
            else if (fileIdentifier.Contains("bigfont"))
            {
                font = new ShxBigFont();
            }
            else if (fileIdentifier.Contains("shapes"))
            {
                font = new ShxShapeFont();
            }

            if (font == null)
            {
                return null;
            }

            font.FileIdentifier = fileIdentifier;
            reader.TryReadByte(out var unknown); // always 26 (0x1A)?
            var commandData = font.Load(reader);

            foreach (var kvp in commandData.Commands)
            {
                var character = kvp.Key;
                var glyphCommands = kvp.Value;
                var glyph = ShxCommandProcessor.Process(commandData.Names[kvp.Key], glyphCommands, commandData.Commands);
                font.AddGlyph(character, glyph);
            }

            return font;
        }
    }
}
