using System;
using System.Collections.Generic;

namespace IxMilia.Shx
{
    internal class ShxCommandProcessorState
    {
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
            _lastPoint = _pointStack.Pop();
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

        public void ProcessArc(ShxGlyphCommandOctagonalArc o)
        {
            // TODO: OctantCount == 0 means full circle
            var startAngle = o.StartingOctant * 0.25 * Math.PI;
            var endAngle = (o.StartingOctant + o.OctantCount) * 0.25 * Math.PI;
            var centerVector = new ShxPoint(Math.Cos(startAngle), Math.Sin(startAngle)) * o.Radius * -1.0;
            var center = _lastPoint + centerVector;
            var endVector = new ShxPoint(Math.Cos(endAngle), Math.Sin(endAngle)) * o.Radius;
            _lastPoint = center + endVector;
            if (_isDrawing)
            {
                Paths.Add(new ShxArc(center, o.Radius, startAngle, endAngle));
            }
        }

        public void SetSize(double width, double height)
        {
            if (Width == 0.0 && Height == 0.0)
            {
                // don't double set
                Width = width;
                Height = height;
            }
        }

        public ShxGlyph CreateGlyph(string name)
        {
            var glyph = new ShxGlyph(name, Paths, Width, Height);
            return glyph;
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
                            var subCommands = characters[r.Character];
                            Process(state, subCommands, characters);
                            break;
                        case ShxGlyphCommandMoveCursor m:
                            state.ProcessNewPosition(new ShxPoint(m.DeltaX, m.DeltaY));
                            break;
                        case ShxGlyphCommandOctagonalArc a:
                            state.ProcessArc(a);
                            break;
                        case ShxGlyphCommandFractionalArc a:
                            // TODO: draw arc
                            break;
                        case ShxGlyphCommandArc a:
                            // TODO: draw arc
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
                    state.SetSize(Math.Abs(move.DeltaX) * 2.0, Math.Abs(move.DeltaY));
                }
            }
        }
    }
}
