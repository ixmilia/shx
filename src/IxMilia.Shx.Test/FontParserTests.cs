using Xunit;

namespace IxMilia.Shx.Test
{
    public class FontParserTests : TestBase
    {
        [Theory]
        [InlineData("ISO3098B.SHX", 115)]
        public void LoadSampleFont(string fileName, int expectedGlyphCount)
        {
            var fontPath = GetPathToSampleFile(fileName);
            var shx = ShxFont.Load(fontPath);
            Assert.Equal(expectedGlyphCount, shx.Glyphs.Count);
        }

        [Fact]
        public void ParseUniFontInfo()
        {
            var reader = new ByteReader(new byte[]
            {
                (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0x00, // "name"
                0x08, // upper baseline offset
                0x02, // lower baseline offset
                0x02, // mode
                0x01, // encoding
                0x02, // embed state
                0x00, // unknown
            });
            var font = new ShxUniFont();
            Assert.True(font.TryReadFontData(reader));
            Assert.Equal("name", font.Name);
            Assert.Equal(8.0, font.UpperCaseBaselineOffset);
            Assert.Equal(2.0, font.LowerCaseBaselineDropOffset);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, font.FontMode);
            Assert.Equal(ShxFontEncoding.PackedMultibyte1, font.FontEncoding);
            Assert.Equal(ShxFontEmbeddingType.ReadOnlyEmbeddable, font.EmbeddingType);
        }

        [Fact]
        public void ParseBigFontInfo4Byte()
        {
            var reader = new ByteReader(new byte[]
            {
                (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0x00, // "name"
                0x08, // upper baseline offset
                0x02, // lower baseline offset
                0x02, // mode
                0x02, // embed state
            });
            var font = new ShxBigFont();
            font.TryReadFontData(reader, 9);
            Assert.Equal("name", font.Name);
            Assert.Equal(8.0, font.UpperCaseBaselineOffset);
            Assert.Equal(2.0, font.LowerCaseBaselineDropOffset);
            Assert.Equal(0.0, font.CharacterWidth);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, font.FontMode);
            Assert.Equal(ShxFontEncoding.PackedMultibyte1, font.FontEncoding);
            Assert.Equal(ShxFontEmbeddingType.ReadOnlyEmbeddable, font.EmbeddingType);
        }

        [Fact]
        public void ParseBigFontInfo5Byte()
        {
            var reader = new ByteReader(new byte[]
            {
                (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0x00, // "name"
                0x08, // upper baseline offset
                0x00, // 0
                0x02, // mode
                0x03, // width
                0x00, // unused
            });
            var font = new ShxBigFont();
            font.TryReadFontData(reader, 10);
            Assert.Equal("name", font.Name);
            Assert.Equal(8.0, font.UpperCaseBaselineOffset);
            Assert.Equal(0.0, font.LowerCaseBaselineDropOffset);
            Assert.Equal(3.0, font.CharacterWidth);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, font.FontMode);
            Assert.Equal(ShxFontEncoding.PackedMultibyte1, font.FontEncoding);
            Assert.Equal(ShxFontEmbeddingType.Embeddable, font.EmbeddingType);
        }
    }
}
