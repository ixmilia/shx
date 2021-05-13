using Xunit;

namespace IxMilia.Shx.Test
{
    public class FontParserTests
    {
        [Fact]
        public void ParseFontInfo()
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
    }
}
