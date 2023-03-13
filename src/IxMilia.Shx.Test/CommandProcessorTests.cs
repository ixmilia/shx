using System;
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
            AssertClose(new ShxPoint(0.0, 2.5), arc.Center);
            AssertClose(2.5, arc.Radius);
            AssertClose(4.71238898038469, arc.StartAngle);
            AssertClose(7.853981633974483, arc.EndAngle);
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

        [Theory]
        [InlineData(10.0, 1.0, 5.0, 1.0, 13.0, 10.08328285887341, 13.583585705632949, 12.583861301110721, 4.705770704414347, 5.1137983760547945)]
        [InlineData(1.0, 2.0, 5.0, -1.0, 17.0, 5.334182491894396, 10.670912459471976, 9.6938052772309327, 4.248858896517695, 4.7811279445519226)]
        [InlineData(6.0, 1.0, 3.0, 1.0, 16.0, 5.547121062992126, 7.358636811023622, 6.3747440282798848, 4.7834915617982388, 5.2847875077644249)]
        [InlineData(10.0, 8.0, -4.0, 2.0, -18.0, 11.456911636045495, 15.91382327209099, 8.04681243084078, 3.9671523329469012, 4.5303304098208663)]
        [InlineData(3.0, 9.0, -1.0, 3.0, -33.0, 5.191481746599856, 11.397160582199952, 3.2479179642554974, 2.954902119643068, 3.9717842963298025)]
        public void ProblematicArcBulges(double lastX, double lastY, double xDisplacement, double yDisplacement, double bulge, double expectedCenterX, double expectedCenterY, double expectedRadius, double expectedStartAngle, double expectedEndAngle)
        {
            var bulgeArc = new ShxGlyphCommandArc(xDisplacement, yDisplacement, bulge);
            var lastPoint = new ShxPoint(lastX, lastY);
            var path = ShxCommandProcessorState.FromArcCommand(bulgeArc, ref lastPoint);
            var expectedNewLastPoint = new ShxPoint(lastX + xDisplacement, lastY + yDisplacement);
            Assert.Equal(expectedNewLastPoint, lastPoint);
            var arc = Assert.IsType<ShxArc>(path);
            var expectedCenter = new ShxPoint(expectedCenterX, expectedCenterY);
            AssertClose(expectedCenter, arc.Center);
            AssertClose(expectedRadius, arc.Radius);
            AssertClose(expectedStartAngle, arc.StartAngle);
            AssertClose(expectedEndAngle, arc.EndAngle);
        }
        
        [Theory]
        [InlineData(-1.0, 1.0, false, 135.0, 315.0)]
        [InlineData(-1.0, -1.0, false, 225.0, 405.0)]
        [InlineData(1.0, -1.0, false, 315.0, 495.0)]
        [InlineData(1.0, 1.0, false, 45.0, 225.0)]
        [InlineData(1.0, -1.0, true, 135.0, 315.0)]
        [InlineData(1.0, 1.0, true, 225.0, 405.0)]
        [InlineData(-1.0, 1.0, true, 315.0, 495.0)]
        [InlineData(-1.0, -1.0, true, 45.0, 225.0)]
        [InlineData(2.0, 0.0, false, 0.0, 180.0)]
        [InlineData(-2.0, 0.0, false, 180.0, 360.0)]
        [InlineData(-2.0, 0.0, true, 0.0, 180.0)]
        [InlineData(2.0, 0.0, true, 180.0, 360.0)]

        public void SemiCircleArcs(double xDisplacement, double yDisplacement, bool isCounterClockwise, double expectedStartAngle, double expectedEndAngle)
        {
            var startPoint = new ShxPoint(0.0, 0.0);
            var lastPoint = startPoint;
            var delta = new ShxPoint(xDisplacement, yDisplacement);
            var expectedEnd = lastPoint + delta;
            var expectedCenter = startPoint + (delta * 0.5);
            var bulge = isCounterClockwise ? 127.0 : -127.0;
            var bulgeArg = new ShxGlyphCommandArc(xDisplacement, yDisplacement, bulge);
            var path = ShxCommandProcessorState.FromArcCommand(bulgeArg, ref lastPoint);
            Assert.Equal(expectedEnd, lastPoint);
            var arc = Assert.IsType<ShxArc>(path);
            AssertClose(expectedCenter, arc.Center);
            var actualStartAngle = arc.StartAngle * 180.0 / Math.PI;
            var actualEndAngle = arc.EndAngle * 180.0 / Math.PI;
            AssertClose(expectedStartAngle, actualStartAngle);
            AssertClose(expectedEndAngle, actualEndAngle);
        }

        [Theory]
        [InlineData(-128.0)]
        [InlineData(-129.0)]
        [InlineData(128.0)]
        [InlineData(129.0)]
        public void UnsupportedBulgeValuesThrowException(double bulge)
        {
            var startPoint = new ShxPoint(0.0, 0.0);
            var bulgeArc = new ShxGlyphCommandArc(1.0, 1.0, bulge);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var path = ShxCommandProcessorState.FromArcCommand(bulgeArc, ref startPoint);
            });
        }

        private static void AssertClose(double expected, double actual, double epsilon = 1.0E-10, string message = null)
        {
            message ??= $"Expected {expected}, actual {actual}";
            Assert.True(Math.Abs(expected - actual) < epsilon, message);
        }

        private static void AssertClose(ShxPoint expected, ShxPoint actual, double epsilon = 1.0E-10)
        {
            var message = $"Expected {expected}, actual {actual}";
            AssertClose(expected.X, actual.X, message: message);
            AssertClose(expected.Y, actual.Y, message: message);
        }
    }
}
