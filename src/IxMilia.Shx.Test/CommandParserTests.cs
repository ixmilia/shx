using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IxMilia.Shx.Test
{
    public class CommandParserTests
    {
        private List<ShxGlyphCommand> ParseCommands(byte[] data, ShxFontEncoding encoding = ShxFontEncoding.Unicode)
        {
            var reader = new ByteReader(data);
            var commands = ShxGlyph.ParseCommands(reader, encoding);
            return commands;
        }

        private ShxGlyphCommand ParseCommand(byte[] data, ShxFontEncoding encoding = ShxFontEncoding.Unicode)
        {
            var commands = ParseCommands(data, encoding);
            return commands.Single();
        }

        private ShxGlyphCommand ParseCommand(params byte[] data)
        {
            return ParseCommand(data, encoding: ShxFontEncoding.Unicode);
        }

        [Fact]
        public void ParsePenDown()
        {
            var _ = (ShxGlyphCommandPenDown)ParseCommand(0x01);
        }

        [Fact]
        public void ParsePenUp()
        {
            var _ = (ShxGlyphCommandPenUp)ParseCommand(0x02);
        }

        [Fact]
        public void ParseScalingDivision()
        {
            var scale = (ShxGlyphCommandUpdateScaleVector)ParseCommand(0x03, 0x02);
            Assert.Equal(0.5, scale.Scale);
        }

        [Fact]
        public void ParseScalingMultiplication()
        {
            var scale = (ShxGlyphCommandUpdateScaleVector)ParseCommand(0x04, 0x02);
            Assert.Equal(2.0, scale.Scale);
        }

        [Fact]
        public void ParsePushPoint()
        {
            var _ = (ShxGlyphCommandPushPoint)ParseCommand(0x05);
        }

        [Fact]
        public void ParsePopPoint()
        {
            var _ = (ShxGlyphCommandPopPoint)ParseCommand(0x06);
        }

        [Fact]
        public void ParseCharacterReplay()
        {
            var replay1 = (ShxGlyphCommandReplayCharacter)ParseCommand(new byte[] { 0x07, 0x01, 0x02 }, encoding: ShxFontEncoding.Unicode);
            Assert.Equal(0x0102, replay1.Character);

            var replay2 = (ShxGlyphCommandReplayCharacter)ParseCommand(new byte[] { 0x07, 0x01 }, encoding: ShxFontEncoding.PackedMultibyte1);
            Assert.Equal(0x01, replay2.Character);
        }

        [Fact]
        public void ParseMove()
        {
            var move = (ShxGlyphCommandMoveCursor)ParseCommand(0x08, 0x01, 0xFF);
            Assert.Equal(1.0, move.DeltaX);
            Assert.Equal(-1.0, move.DeltaY);
        }

        [Fact]
        public void ParseManyMoves()
        {
            var moves = ParseCommands(new byte[]
            {
                0x09, // many moves
                0x01, 0xFF, // p1
                0x00, 0xFF, // p2
                0x01, 0x00, // p3
                0x00, 0x00, // end
            });
            Assert.Equal(3, moves.Count);
            Assert.Equal(1.0, ((ShxGlyphCommandMoveCursor)moves[0]).DeltaX);
            Assert.Equal(-1.0, ((ShxGlyphCommandMoveCursor)moves[0]).DeltaY);
            Assert.Equal(0.0, ((ShxGlyphCommandMoveCursor)moves[1]).DeltaX);
            Assert.Equal(-1.0, ((ShxGlyphCommandMoveCursor)moves[1]).DeltaY);
            Assert.Equal(1.0, ((ShxGlyphCommandMoveCursor)moves[2]).DeltaX);
            Assert.Equal(0.0, ((ShxGlyphCommandMoveCursor)moves[2]).DeltaY);
        }

        [Fact]
        public void ParseOctantArc()
        {
            var arc = (ShxGlyphCommandOctantArc)ParseCommand(0x0A, 0x01, 0b1011_0010);
            Assert.Equal(1.0, arc.Radius);
            Assert.False(arc.IsCounterClockwise);
            Assert.Equal(3, arc.StartingOctant);
            Assert.Equal(2, arc.OctantCount);
        }

        [Fact]
        public void ParseFractionalArc()
        {
            var arc = (ShxGlyphCommandFractionalArc)ParseCommand(0x0B, 0x01, 0x02, 0x03, 0x04, 0b1011_0010);
            Assert.Equal(1.0, arc.StartOffset);
            Assert.Equal(2.0, arc.EndOffset);
            Assert.Equal(3.0, arc.HighRadius);
            Assert.Equal(4.0, arc.Radius);
            Assert.False(arc.IsCounterClockwise);
            Assert.Equal(3, arc.StartingOctant);
            Assert.Equal(2, arc.OctantCount);
        }

        [Fact]
        public void ParseBulgeArc()
        {
            var arc = (ShxGlyphCommandArc)ParseCommand(0x0C, 0x01, 0xFF, 0xFF);
            Assert.Equal(1.0, arc.XDisplacement);
            Assert.Equal(-1.0, arc.YDisplacement);
            Assert.Equal(-1.0, arc.Bulge);
        }

        [Fact]
        public void ParseManyBulgeArcs()
        {
            var arcs = ParseCommands(new byte[]
            {
                0x0D, // many bulge arcs
                0x00, 0xFF, 0x00, // p1 + bulge
                0x01, 0x00, 0xFF, // p2 + bulge
                0x00, 0x00, // end
            });
            Assert.Equal(2, arcs.Count);
            Assert.Equal(0.0, ((ShxGlyphCommandArc)arcs[0]).XDisplacement);
            Assert.Equal(-1.0, ((ShxGlyphCommandArc)arcs[0]).YDisplacement);
            Assert.Equal(0.0, ((ShxGlyphCommandArc)arcs[0]).Bulge);
            Assert.Equal(1.0, ((ShxGlyphCommandArc)arcs[1]).XDisplacement);
            Assert.Equal(0.0, ((ShxGlyphCommandArc)arcs[1]).YDisplacement);
            Assert.Equal(-1.0, ((ShxGlyphCommandArc)arcs[1]).Bulge);
        }

        [Fact]
        public void ParseSkipNextCommand()
        {
            var _ = (ShxGlyphCommandSkipNextIfHorizontal)ParseCommand(0x0E);
        }
    }
}
