﻿using System;
using System.Collections.Generic;
using System.Net;

namespace IxMilia.Shx
{
    internal class ShxCommandProcessorState
    {
        private static readonly double PI4 = Math.Sqrt(2.0) * 0.2;
        private static readonly IReadOnlyList<ShxPoint> OctantArcVectors = new ShxPoint[]
        {
            new ShxPoint(1.0, 0.0), // right
            new ShxPoint(PI4, PI4),
            new ShxPoint(0.0, 1.0), // up
            new ShxPoint(-PI4, PI4),
            new ShxPoint(-1.0, 0.0), // left
            new ShxPoint(-PI4, -PI4),
            new ShxPoint(0.0, -1.0), // down
            new ShxPoint(PI4, -PI4),
        };

        public List<ShxGlyphPath> Paths { get; } = new List<ShxGlyphPath>();
        public double Width { get; private set; }
        public double Height { get; private set; }

        private Stack<ShxPoint> _pointStack = new Stack<ShxPoint>();
        private ShxPoint _lastPoint;
        private bool _isDrawing;
        private double _vectorScale = 1.0;
        private bool _skipNext;

        public void ApplyVectorScale(double scale)
        {
            _vectorScale *= scale;
        }

        public void SetDrawingState(bool isDrawing)
        {
            _isDrawing = isDrawing;
        }

        public void PushPoint()
        {
            _pointStack.Push(_lastPoint);
        }

        public void PopPoint()
        {
            if (_pointStack.Count > 0)
            {
                // TODO: some fonts contain a single pop instruction for the character '\r'
                _lastPoint = _pointStack.Pop();
            }
        }

        public bool MoveNext()
        {
            var result = _skipNext;
            _skipNext = false;
            return !result;
        }

        public void SkipNextCommand()
        {
            _skipNext = true;
        }

        public void ProcessNewPosition(ShxPoint delta)
        {
            var nextPoint = _lastPoint + (delta * _vectorScale);
            if (_isDrawing)
            {
                Paths.Add(new ShxLine(_lastPoint, nextPoint));
            }

            _lastPoint = nextPoint;
        }

        public void ProcessArc(ShxGlyphCommandOctantArc a)
        {
            var arc = FromArcCommand(a, ref _lastPoint);
            if (_isDrawing)
            {
                Paths.Add(arc);
            }
        }

        public void ProcessArc(ShxGlyphCommandFractionalArc a)
        {
            var arc = FromArcCommand(a, ref _lastPoint);
            if (_isDrawing)
            {
                Paths.Add(arc);
            }
        }

        public void ProcessArc(ShxGlyphCommandArc a)
        {
            var glyphPath = FromArcCommand(a, ref _lastPoint);
            if (_isDrawing)
            {
                Paths.Add(glyphPath);
            }
        }

        public void SetSize(double width, double height)
        {
            Width += width;
            Height += height;
        }

        public ShxGlyph CreateGlyph(string name)
        {
            var glyph = new ShxGlyph(name, Paths, Width, Height);
            return glyph;
        }

        public static ShxArc FromArcCommand(ShxGlyphCommandOctantArc a, ref ShxPoint lastPoint)
        {
            var octantCount = a.OctantCount;
            if (octantCount == 0)
            {
                // 0 means full circle
                octantCount = 8;
            }

            var endingOctant = a.IsCounterClockwise
                ? a.StartingOctant + octantCount
                : a.StartingOctant - octantCount;
            while (endingOctant < 0)
            {
                endingOctant += 8;
            }

            var startVector = OctantArcVectors[a.StartingOctant % 8] * a.Radius;
            var endVector = OctantArcVectors[endingOctant % 8] * a.Radius;
            if (a.OctantCount < 0)
            {
                var temp = startVector;
                startVector = endVector;
                endVector = temp;
            }

            var center = lastPoint - startVector;
            lastPoint = center + endVector;
            var startAngle = Math.Atan2(startVector.Y, startVector.X);
            var endAngle = Math.Atan2(endVector.Y, endVector.X);
            if (!a.IsCounterClockwise)
            {
                var temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }

            while (endAngle <= startAngle)
            {
                endAngle += Math.PI * 2.0;
            }

            return new ShxArc(center, a.Radius, startAngle, endAngle);
        }

        public static ShxArc FromArcCommand(ShxGlyphCommandFractionalArc a, ref ShxPoint lastPoint)
        {
            var radius = a.HighRadius * 256.0 + a.Radius;
            var startOctantAngle = a.StartingOctant * 45.0;
            var endOctantAngle = (a.StartingOctant + a.OctantCount - 1) * 45.0;
            var startAngleDegrees = (45.0 * a.StartOffset / 256.0) + startOctantAngle;
            var startAngleRadians = startAngleDegrees * Math.PI / 180.0;
            var endAngleDegrees = (45.0 * a.EndOffset / 256.0) + endOctantAngle;
            var endAngleRadians = endAngleDegrees * Math.PI / 180.0;

            var startVector = ShxPoint.FromAngleRadians(startAngleRadians) * radius;
            var endVector = ShxPoint.FromAngleRadians(endAngleRadians) * radius;
            var center = lastPoint - startVector;
            lastPoint = center + endVector;
            var arc = new ShxArc(center, radius, startAngleRadians, endAngleRadians);
            return arc;
        }

        public static ShxGlyphPath FromArcCommand(ShxGlyphCommandArc a, ref ShxPoint lastPoint)
        {
            if (a.Bulge < -127.0 || a.Bulge > 127.0)
            {
                throw new ArgumentOutOfRangeException(nameof(ShxGlyphCommandArc.Bulge), "Bulge must be in the range [-127, 127].");
            }

            var offset = new ShxPoint(a.XDisplacement, a.YDisplacement);
            if (a.Bulge == 0.0)
            {
                // according to the spec, a bulge of 0 is valid and means a straight line
                // see code `00C` at https://help.autodesk.com/view/OARX/2020/ENU/?guid=GUID-06832147-16BE-4A66-A6D0-3ADF98DC8228
                var lineStart = lastPoint;
                var lineEnd = lineStart + offset;
                lastPoint = lineEnd;
                var linePath = new ShxLine(lineStart, lineEnd);
                return linePath;
            }

            var chordLength = offset.Length;
            var perpendicularHeight = Math.Abs(a.Bulge) * chordLength / 254.0;
            if (perpendicularHeight > chordLength / 2.0)
            {
                throw new InvalidOperationException("Arc is too big");
            }

            var isCounterClockwise = a.Bulge >= 0.0;
            var perpendicularVector = offset.Perpendicular;
            if (!isCounterClockwise)
            {
                perpendicularVector *= -1.0;
            }

            var normalizedPerpendicularVector = perpendicularVector.Normalized * perpendicularHeight;

            var startPoint = lastPoint;
            var midPoint = startPoint + offset.MidPoint + normalizedPerpendicularVector;
            var endPoint = startPoint + offset;
            lastPoint = endPoint;

            var radius = (perpendicularHeight / 2.0) + (chordLength * chordLength / (8.0 * perpendicularHeight));
            var center = midPoint - (normalizedPerpendicularVector.Normalized * radius);
            var arcAngle = 2.0 * Math.Asin(chordLength / (2.0 * radius));
            if (!isCounterClockwise)
            {
                var temp = startPoint;
                startPoint = endPoint;
                endPoint = temp;
            }

            var startPointVector = startPoint - center;
            var startAngle = Math.Atan2(startPointVector.Y, startPointVector.X);
            var endPointVector = endPoint - center;
            var endAngle = Math.Atan2(endPointVector.Y, endPointVector.X);

            var fullCircle = Math.PI * 2.0;
            while (endAngle < startAngle)
            {
                endAngle += fullCircle;
            }

            while (startAngle < 0.0)
            {
                startAngle += fullCircle;
                endAngle += fullCircle;
            }

            var actualAngle = endAngle - startAngle;
            var angleDiff = Math.Abs(actualAngle - arcAngle);
            if (angleDiff >= 1.0E-6)
            {
                throw new InvalidOperationException("Calculated and actual angles don't match.");
            }

            var angleCircleDiff = actualAngle - fullCircle / 2.0;
            var isTooBig = angleCircleDiff > 1.0E-6;
            if (isTooBig)
            {
                throw new InvalidOperationException("Angle cannot be larger than half of a circle.");
            }

            var arc = new ShxArc(center, radius, startAngle, endAngle);
            return arc;
        }
    }

    internal class ShxCommandProcessor
    {
        public static ShxGlyph Process(string glyphName, IEnumerable<ShxGlyphCommand> commands, IReadOnlyDictionary<ushort, IEnumerable<ShxGlyphCommand>> characters)
        {
            var state = new ShxCommandProcessorState();
            Process(state, commands, characters);
            var glyph = state.CreateGlyph(glyphName);
            return glyph;
        }

        private static void Process(ShxCommandProcessorState state, IEnumerable<ShxGlyphCommand> commands, IReadOnlyDictionary<ushort, IEnumerable<ShxGlyphCommand>> characters)
        {
            foreach (var command in commands)
            {
                if (state.MoveNext())
                {
                    switch (command)
                    {
                        case ShxGlyphCommandPenDown _:
                            state.SetDrawingState(true);
                            break;
                        case ShxGlyphCommandPenUp _:
                            state.SetDrawingState(false);
                            break;
                        case ShxGlyphCommandUpdateScaleVector sc:
                            state.ApplyVectorScale(sc.Scale);
                            break;
                        case ShxGlyphCommandPushPoint _:
                            state.PushPoint();
                            break;
                        case ShxGlyphCommandPopPoint _:
                            state.PopPoint();
                            break;
                        case ShxGlyphCommandReplayCharacter r:
                            if (characters.TryGetValue(r.Character, out var subCommands))
                            {
                                // TODO: handle offset/scale
                                Process(state, subCommands, characters);
                            }
                            break;
                        case ShxGlyphCommandMoveCursor m:
                            state.ProcessNewPosition(new ShxPoint(m.DeltaX, m.DeltaY));
                            break;
                        case ShxGlyphCommandOctantArc a:
                            state.ProcessArc(a);
                            break;
                        case ShxGlyphCommandFractionalArc a:
                            state.ProcessArc(a);
                            break;
                        case ShxGlyphCommandArc a:
                            state.ProcessArc(a);
                            break;
                        case ShxGlyphCommandSkipNextIfHorizontal _:
                            // always assuming horizontal mode
                            state.SkipNextCommand();
                            break;
                        default:
                            throw new NotSupportedException($"Unexpected glyph command '{command?.GetType().Name}'");
                    }
                }
                else if (command is ShxGlyphCommandMoveCursor move)
                {
                    // heuristic: the vertical command skip usually preceeds a move that corresponds to the negative
                    // height and half the width of the character
                    state.SetSize(Math.Abs(move.DeltaX), Math.Abs(move.DeltaY));
                }
            }
        }
    }
}
