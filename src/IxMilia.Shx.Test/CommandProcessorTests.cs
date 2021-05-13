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
    }
}
