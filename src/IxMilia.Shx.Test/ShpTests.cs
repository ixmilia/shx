using System.IO;
using System.Linq;
using Xunit;

namespace IxMilia.Shx.Test
{
    public class ShpTests : TestBase
    {
        [Theory]
        [InlineData("ISO3098B.SHP")]
        public void CompileAndLoadSampleFont(string fontName)
        {
            var fontPath = GetPathToSampleFile(fontName);
            var shp = ShpFont.Parse(File.ReadAllText(fontPath));
            var compiled = shp.Compile();
            var shx = ShxFont.Load(compiled);
        }

        [Fact]
        public void ParseBigFont()
        {
            var content = @"
; lead comment

; blank line above and below

*0,4,test-font-name
6,3,2,0

; character 'A', name is given
*65,6,upper-case-A
; pen down, move 2,3, pen up, null
1,8,2,3,2,0

; character 'B', name is blank
*66,6,
1,8,5,
; data is split up
6,2,0
";
            var font = ShpFont.Parse(content);
            Assert.Equal("test-font-name", font.Name);
            Assert.Equal(6.0, font.UpperCaseBaselineOffset);
            Assert.Equal(3.0, font.LowerCaseBaselineDropOffset);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, font.FontMode);
            Assert.Equal(ShxFontEncoding.PackedMultibyte1, font.FontEncoding);
            Assert.Equal(ShxFontEmbeddingType.Embeddable, font.EmbeddingType);
            Assert.Equal(2, font.Shapes.Count);

            var letterA = font.Shapes[65];
            Assert.Equal("upper-case-A", letterA.Name);
            Assert.Equal(new byte[] { 1, 8, 2, 3, 2 }, letterA.Data);

            var letterB = font.Shapes[66];
            Assert.Equal("", letterB.Name);
            Assert.Equal(new byte[] { 1, 8, 5, 6, 2 }, letterB.Data);
        }

        [Fact]
        public void ParseUniFont()
        {
            var content = @"
*UNIFONT,6,test-font-name
6,3,2,0,0,0

*65,6,upper-case-A
1,8,2,3,2,0
";
            var font = ShpFont.Parse(content);
            Assert.Equal("test-font-name", font.Name);
            Assert.Equal(6.0, font.UpperCaseBaselineOffset);
            Assert.Equal(3.0, font.LowerCaseBaselineDropOffset);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, font.FontMode);
            Assert.Equal(ShxFontEncoding.Unicode, font.FontEncoding);
            Assert.Equal(ShxFontEmbeddingType.Embeddable, font.EmbeddingType);
        }

        [Fact]
        public void ParseMultiByteShapeAsOneToken()
        {
            var lines = new[]
            {
                "*1,4,",
                "7,020AC,0",
            };
            var shape = ShpShapeDescription.Parse(lines, 0, true, out var _);
            Assert.Equal(new byte[] { 0x07, 0x20, 0xAC }, shape.Data);
        }

        [Fact]
        public void ParseMultiByteShapeAsTwoTokens()
        {
            var lines = new[]
            {
                "*1,4,",
                "7,020,0AC,0",
            };
            var shape = ShpShapeDescription.Parse(lines, 0, true, out var _);
            Assert.Equal(new byte[] { 0x07, 0x20, 0xAC }, shape.Data);
        }

        [Fact]
        public void ParseParenthesizedBytes()
        {
            var lines = new[]
            {
                "*1,4,",
                "8,(1,2),0",
            };
            var shape = ShpShapeDescription.Parse(lines, 0, false, out var _);
            Assert.Equal(new byte[] { 0x08, 0x01, 0x02 }, shape.Data);
        }

        [Fact]
        public void ParseShapeNumberAsHex()
        {
            var lines = new[]
            {
                "*0A,1,",
                "0",
            };
            var shape = ShpShapeDescription.Parse(lines, 0, false, out var _);
            Assert.Equal(10, shape.ShapeNumber);
        }

        [Fact]
        public void ParseNegativeByteValues()
        {
            var lines = new[]
            {
                "*1,4,",
                "8,(-128,-5),0",
            };
            var shape = ShpShapeDescription.Parse(lines, 0, false, out var _);
            Assert.Equal(new byte[] { 0x08, 0x80, 0xFB }, shape.Data);
        }

        [Fact]
        public void ParseCode10NegativeValues()
        {
            var lines = new[]
            {
                "*1,4,",
                "10,(2,-062),0", // the negative sign here sets the high bit
            };
            var shape = ShpShapeDescription.Parse(lines, 0, false, out var _);
            Assert.Equal(new byte[] { 0x0A, 0x02, 0b1110_0010 }, shape.Data);
        }

        [Fact]
        public void ShapeToString()
        {
            var shape = new ShpShapeDescription(42, "SHAPE_NAME", new byte[] { 0x01, 0x02, 0x0A });
            var expected = "*42,4,SHAPE_NAME\n001,002,00A,0";
            var actual = shape.ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LoadCompiledUniFont()
        {
            var shpFont = ShpFont.Parse(@"
*UNIFONT,6,test font name
6,2,2,0,0,0

*65,6,uca
1,8,(2,3),2,0
");
            var compiled = shpFont.Compile();

            var shxFont = ShxFont.Load(compiled);
            Assert.Equal(ShxFontEncoding.Unicode, shxFont.FontEncoding);
            Assert.IsType<ShxUniFont>(shxFont);
            Assert.Equal("test font name", shxFont.Name);
            Assert.Equal(6.0, shxFont.UpperCaseBaselineOffset);
            Assert.Equal(2.0, shxFont.LowerCaseBaselineDropOffset);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, shxFont.FontMode);
            Assert.Equal(ShxFontEmbeddingType.Embeddable, shxFont.EmbeddingType);
            var glyphKvp = shxFont.Glyphs.Single();
            Assert.Equal(65, glyphKvp.Key);
            var glyph = glyphKvp.Value;
            var pathItem = glyph.Paths.Single();
            var line = Assert.IsType<ShxLine>(pathItem);
            Assert.Equal(new ShxPoint(0, 0), line.P1);
            Assert.Equal(new ShxPoint(2, 3), line.P2);
        }

        [Fact]
        public void LoadCompiledBigFont()
        {
            var shpFont = ShpFont.Parse(@"
*0,4,test font name
6,2,2,0

*65,6,uca
1,8,(2,3),2,0
");
            var compiled = shpFont.Compile();

            var shxFont = ShxFont.Load(compiled);
            Assert.Equal(ShxFontEncoding.PackedMultibyte1, shxFont.FontEncoding);
            Assert.IsType<ShxBigFont>(shxFont);
            Assert.Equal("test font name", shxFont.Name);
            Assert.Equal(6.0, shxFont.UpperCaseBaselineOffset);
            Assert.Equal(2.0, shxFont.LowerCaseBaselineDropOffset);
            Assert.Equal(ShxFontMode.HorizontalAndVertical, shxFont.FontMode);
            Assert.Equal(ShxFontEmbeddingType.Embeddable, shxFont.EmbeddingType);
            var glyphKvp = shxFont.Glyphs.Single();
            Assert.Equal(65, glyphKvp.Key);
            var glyph = glyphKvp.Value;
            var pathItem = glyph.Paths.Single();
            var line = Assert.IsType<ShxLine>(pathItem);
            Assert.Equal(new ShxPoint(0, 0), line.P1);
            Assert.Equal(new ShxPoint(2, 3), line.P2);
        }
    }
}
