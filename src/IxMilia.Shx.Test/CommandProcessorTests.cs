using Xunit;

namespace IxMilia.Shx.Test
{
    public class CommandProcessorTests
    {
        [Fact]
        public void FromOctantArc()
        {
            var octantArcCommand = new ShxGlyphCommandOctantArc(1.0, 3, 2, false);
            var lastPoint = ShxPoint.Origin;
            var arc = ShxCommandProcessorState.FromArcCommand(octantArcCommand, ref lastPoint);
            Assert.Equal(new ShxPoint(0.5656854249492381, 0.0), lastPoint);
            Assert.Equal(new ShxPoint(0.28284271247461906, -0.28284271247461906), arc.Center);
            Assert.Equal(1.0, arc.Radius);
            Assert.Equal(0.7853981633974483, arc.StartAngle);
            Assert.Equal(2.356194490192345, arc.EndAngle);
        }

        [Fact]
        public void FromFractionalArc()
        {
            var fractionalArcCommand = new ShxGlyphCommandFractionalArc(56.0, 28.0, 0.0, 3.0, 1, 2, true);
            var lastPoint = ShxPoint.Origin;
            var arc = ShxCommandProcessorState.FromArcCommand(fractionalArcCommand, ref lastPoint);
            Assert.Equal(new ShxPoint(-1.9848165112868559, 0.5361833970935828), lastPoint);
            Assert.Equal(new ShxPoint(-1.727424574253536, -2.4527544394547514), arc.Center);
            Assert.Equal(3.0, arc.Radius);
            Assert.Equal(0.9572040116406401, arc.StartAngle);
            Assert.Equal(1.6566992509164926, arc.EndAngle);
        }

        [Fact]
        public void FromBulgeArc()
        {
            var bulgeArc = new ShxGlyphCommandArc(0.0, 5.0, 127.0);
            var lastPoint = ShxPoint.Origin;
            var path = ShxCommandProcessorState.FromArcCommand(bulgeArc, ref lastPoint);
            Assert.Equal(new ShxPoint(0.0, 5.0), lastPoint);
            var arc = Assert.IsType<ShxArc>(path);
            Assert.Equal(new ShxPoint(0.0, 2.5), arc.Center);
            Assert.Equal(2.5, arc.Radius);
            Assert.Equal(-1.5707963267948966, arc.StartAngle);
            Assert.Equal(1.5707963267948966, arc.EndAngle);
        }

        [Fact]
        public void FromArcWithZeroBulge()
        {
            // the spec states that a bulge of 0 indicates a straight line segment
            var bulgeArc = new ShxGlyphCommandArc(2.0, 2.0, 0.0);
            var lastPoint = new ShxPoint(1.0, 1.0);
            var path = ShxCommandProcessorState.FromArcCommand(bulgeArc, ref lastPoint);
            Assert.Equal(new ShxPoint(3.0, 3.0), lastPoint);
            var line = Assert.IsType<ShxLine>(path);
            Assert.Equal(new ShxPoint(1.0, 1.0), line.P1);
            Assert.Equal(new ShxPoint(3.0, 3.0), line.P2);
        }
    }
}
