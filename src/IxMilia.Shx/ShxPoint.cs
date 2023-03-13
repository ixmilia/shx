using System;

namespace IxMilia.Shx
{
    public struct ShxPoint : IEquatable<ShxPoint>
    {
        public double X { get; }
        public double Y { get; }

        public ShxPoint(double x, double y)
            : this()
        {
            this.X = x;
            this.Y = y;
        }

        public double LengthSquared => X * X + Y * Y;

        public double Length => Math.Sqrt(LengthSquared);

        public ShxPoint Normalized => this / Length;

        public ShxPoint Perpendicular => new ShxPoint(Y, -X);

        public ShxPoint MidPoint => this * 0.5;

        public static bool operator ==(ShxPoint p1, ShxPoint p2)
        {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(ShxPoint p1, ShxPoint p2)
        {
            return !(p1 == p2);
        }

        public static ShxPoint operator +(ShxPoint p1, ShxPoint p2)
        {
            return new ShxPoint(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static ShxPoint operator -(ShxPoint p1, ShxPoint p2)
        {
            return new ShxPoint(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static ShxPoint operator *(ShxPoint p, double scalar)
        {
            return new ShxPoint(p.X * scalar, p.Y * scalar);
        }

        public static ShxPoint operator /(ShxPoint p, double scalar)
        {
            return new ShxPoint(p.X / scalar, p.Y / scalar);
        }

        public override bool Equals(object obj)
        {
            return obj is ShxPoint && this == (ShxPoint)obj;
        }

        public bool Equals(ShxPoint other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        public static ShxPoint FromAngleDegrees(double angle)
        {
            return FromAngleRadians(angle * Math.PI / 180.0);
        }

        public static ShxPoint FromAngleRadians(double angle)
        {
            return new ShxPoint(Math.Cos(angle), Math.Sin(angle));
        }

        public static ShxPoint Origin
        {
            get { return new ShxPoint(0.0, 0.0); }
        }
    }
}
