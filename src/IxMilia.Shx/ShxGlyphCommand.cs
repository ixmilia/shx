namespace IxMilia.Shx
{
    internal abstract class ShxGlyphCommand
    {
    }

    internal class ShxGlyphCommandPenDown : ShxGlyphCommand
    {
    }

    internal class ShxGlyphCommandPenUp : ShxGlyphCommand
    {
    }

    internal class ShxGlyphCommandUpdateScaleVector : ShxGlyphCommand
    {
        public double Scale { get; }

        public ShxGlyphCommandUpdateScaleVector(double scale)
        {
            Scale = scale;
        }
    }

    internal class ShxGlyphCommandPushPoint : ShxGlyphCommand
    {
    }

    internal class ShxGlyphCommandPopPoint : ShxGlyphCommand
    {
    }

    internal class ShxGlyphCommandReplayCharacter : ShxGlyphCommand
    {
        public ushort Character { get; }

        public ShxGlyphCommandReplayCharacter(ushort character)
        {
            Character = character;
        }
    }

    internal class ShxGlyphCommandMoveCursor : ShxGlyphCommand
    {
        public double DeltaX { get; }
        public double DeltaY { get; }

        public ShxGlyphCommandMoveCursor(double deltaX, double deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }

    internal class ShxGlyphCommandOctantArc : ShxGlyphCommand
    {
        public double Radius { get; }
        public int StartingOctant { get; }
        public int OctantCount { get; }
        public bool IsCounterClockwise { get; }

        public ShxGlyphCommandOctantArc(double radius, int startingOctant, int octantCount, bool isCounterClockwise)
        {
            Radius = radius;
            StartingOctant = startingOctant;
            OctantCount = octantCount;
            IsCounterClockwise = isCounterClockwise;
        }
    }

    internal class ShxGlyphCommandFractionalArc : ShxGlyphCommand
    {
        public double StartOffset { get; }
        public double EndOffset { get; }
        public double HighRadius { get; }
        public double Radius { get; }
        public int StartingOctant { get; }
        public int OctantCount { get; }
        public bool IsCounterClockwise { get; }

        public ShxGlyphCommandFractionalArc(double startOffset, double endOffset, double highRadius, double radius, int startingOctant, int octantCount, bool isCounterClockwise)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            HighRadius = highRadius;
            Radius = radius;
            StartingOctant = startingOctant;
            OctantCount = octantCount;
            IsCounterClockwise = isCounterClockwise;
        }
    }

    internal class ShxGlyphCommandArc : ShxGlyphCommand
    {
        public double XDisplacement { get; }
        public double YDisplacement { get; }
        public double Bulge { get; }

        public ShxGlyphCommandArc(double xDisplacement, double yDisplacement, double bulge)
        {
            XDisplacement = xDisplacement;
            YDisplacement = yDisplacement;
            Bulge = bulge;
        }
    }

    internal class ShxGlyphCommandSkipNextIfHorizontal : ShxGlyphCommand
    {
    }
}
