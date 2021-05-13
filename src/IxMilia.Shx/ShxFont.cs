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

    public abstract class ShxFont
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

        protected void AddGlyph(char code, ShxGlyph glyph) => _glyphs.Add(code, glyph);

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

        internal abstract void Load(ByteReader reader);

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
            var reader = new ByteReader(data);
            var fileIdentifier = reader.ReadLine();
            ShxFont font = null;
            if (fileIdentifier.Contains("unifont"))
            {
                font = new ShxUniFont();
            }

            if (font == null)
            {
                return null;
            }

            font.FileIdentifier = fileIdentifier;
            font.Load(reader);

            return font;
        }
    }
}
