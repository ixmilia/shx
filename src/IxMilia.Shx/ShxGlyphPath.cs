namespace IxMilia.Shx
{
    public abstract class ShxGlyphPath
    {
    }

    public class ShxLine : ShxGlyphPath
    {
        public ShxPoint P1 { get; }
        public ShxPoint P2 { get; }

        public ShxLine(ShxPoint p1, ShxPoint p2)
        {
            P1 = p1;
            P2 = p2;
        }

        public override string ToString()
        {
            return $"{P1} - {P2}";
        }
    }

    public class ShxArc : ShxGlyphPath
    {
        public ShxPoint Center { get; }
        public double Radius { get; }
        public double StartAngle { get; }
        public double EndAngle { get; }

        public ShxArc(ShxPoint center, double radius, double startAngle, double endAngle)
        {
            Center = center;
            Radius = radius;
            StartAngle = startAngle;
            EndAngle = endAngle;
        }
    }
}
