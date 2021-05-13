using System.Collections.Generic;

namespace IxMilia.Shx
{
    public class ShxGlyph
    {
        public string Name { get; }
        public IReadOnlyList<ShxGlyphPath> Paths { get; }
        public double Width { get; }
        public double Height { get; }

        // https://help.autodesk.com/view/ACD/2020/ENU/?guid=GUID-0A8E12A1-F4AB-44AD-8A9B-2140E0D5FD23
        private static readonly IReadOnlyList<ShxPoint> DirectionVectors = new ShxPoint[]
            {
                new ShxPoint(1.0, 0.0), // 0 degrees (right)
                new ShxPoint(1.0, 0.5),
                new ShxPoint(1.0, 1.0), // 45 degrees
                new ShxPoint(0.5, 1.0),
                new ShxPoint(0.0, 1.0), // 90 degrees (up)
                new ShxPoint(-0.5, 1.0),
                new ShxPoint(-1.0, 1.0), // 135 degrees
                new ShxPoint(-1.0, 0.5),
                new ShxPoint(-1.0, 0.0), // 180 degreesn (left)
                new ShxPoint(-1.0, -0.5),
                new ShxPoint(-1.0, -1.0), // 225 degrees
                new ShxPoint(-0.5, -1.0),
                new ShxPoint(0.0, -1.0), // 270 degrees (down)
                new ShxPoint(0.5, -1.0),
                new ShxPoint(1.0, -1.0), // 315 degrees
                new ShxPoint(1.0, -0.5)
            };

        public ShxGlyph(string name, IReadOnlyList<ShxGlyphPath> paths, double width, double height)
        {
            Name = name;
            Paths = paths;
            Width = width;
            Height = height;
        }

        internal static List<ShxGlyphCommand> ParseCommands(ByteReader reader, ShxFontEncoding fontEncoding)
        {
            var commands = new List<ShxGlyphCommand>();
            while (reader.TryReadByte(out var command) && command != 0)
            {
                var distance = (command & 0xF0) >> 4;
                var direction = command & 0x0F;
                var isBareCommand = distance == 0;
                if (isBareCommand)
                {
                    // special command handling for no movement
                    switch (command)
                    {
                        case 1:
                            // pen down
                            commands.Add(new ShxGlyphCommandPenDown());
                            break;
                        case 2:
                            // pen up
                            commands.Add(new ShxGlyphCommandPenUp());
                            break;
                        case 3:
                            // divide scaling vector by specified value
                            if (reader.TryReadByte(out var divr))
                            {
                                commands.Add(new ShxGlyphCommandUpdateScaleVector(1.0 / divr));
                            }
                            break;
                        case 4:
                            // multiply scaling vector by specified value
                            if (reader.TryReadByte(out var mulr))
                            {
                                commands.Add(new ShxGlyphCommandUpdateScaleVector(mulr)); ;
                            }
                            break;
                        case 5:
                            // push location
                            commands.Add(new ShxGlyphCommandPushPoint());
                            break;
                        case 6:
                            // pop location
                            commands.Add(new ShxGlyphCommandPopPoint());
                            break;
                        case 7:
                            {
                                // replay the given character code
                                if (fontEncoding == ShxFontEncoding.Unicode)
                                {
                                    if (reader.TryReadUInt16BigEndian(out var replayCode))
                                    {
                                        commands.Add(new ShxGlyphCommandReplayCharacter(replayCode));
                                    }
                                }
                                else
                                {
                                    if (reader.TryReadByte(out var replayCode))
                                    {
                                        commands.Add(new ShxGlyphCommandReplayCharacter(replayCode));
                                    }
                                }
                            }
                            break;
                        case 8:
                            {
                                // move x, y
                                if (reader.TryReadSByte(out var xOffset) &&
                                    reader.TryReadSByte(out var yOffset))
                                {
                                    commands.Add(new ShxGlyphCommandMoveCursor(xOffset, yOffset));
                                }
                            }
                            break;
                        case 9:
                            {
                                // move x, y until 0, 0
                                while (reader.TryReadSByte(out var xOffset)
                                    && reader.TryReadSByte(out var yOffset))
                                {
                                    if (xOffset == 0.0 && yOffset == 0.0)
                                    {
                                        // done
                                        break;
                                    }

                                    commands.Add(new ShxGlyphCommandMoveCursor(xOffset, yOffset));
                                }
                            }
                            break;
                        case 0xA:
                            {
                                // octagonal arc
                                if (reader.TryReadByte(out var radius) &&
                                    reader.TryReadByte(out var sc))
                                {
                                    var isCounterClockwise = (sc & 0b10000000) == 0;
                                    var startingOctant = (sc & 0b01110000) >> 4;
                                    var octantCount = sc & 0b00000111;
                                    commands.Add(new ShxGlyphCommandOctantArc(radius, startingOctant, octantCount, isCounterClockwise));
                                }
                            }
                            break;
                        case 0xB:
                            {
                                // fractional arc
                                if (reader.TryReadByte(out var startOffset) &&
                                    reader.TryReadByte(out var endOffset) &&
                                    reader.TryReadByte(out var highRadius) &&
                                    reader.TryReadByte(out var radius) &&
                                    reader.TryReadByte(out var sc))
                                {
                                    var isCounterClockwise = (sc & 0b10000000) == 0;
                                    var startingOctant = (sc & 0b01110000) >> 4;
                                    var octantCount = sc & 0b00000111;
                                    commands.Add(new ShxGlyphCommandFractionalArc(startOffset, endOffset, highRadius, radius, startingOctant, octantCount, isCounterClockwise));
                                }
                            }
                            break;
                        case 0xC:
                            {
                                // arc
                                if (reader.TryReadSByte(out var xDisplacement) &&
                                    reader.TryReadSByte(out var yDisplacement) &&
                                    reader.TryReadSByte(out var bulge))
                                {
                                    commands.Add(new ShxGlyphCommandArc(xDisplacement, yDisplacement, bulge));
                                }
                            }
                            break;
                        case 0xD:
                            {
                                // continuous arcs
                                while (reader.TryReadSByte(out var xDisplacement)
                                    && reader.TryReadSByte(out var yDisplacement))
                                {
                                    if (xDisplacement == 0 && yDisplacement == 0)
                                    {
                                        break;
                                    }

                                    if (reader.TryReadSByte(out var bulge))
                                    {
                                        commands.Add(new ShxGlyphCommandArc(xDisplacement, yDisplacement, bulge));
                                    }
                                }
                            }
                            break;
                        case 0xE:
                            // skip next command if in horizontal mode
                            commands.Add(new ShxGlyphCommandSkipNextIfHorizontal());
                            break;
                        case 0xF:
                            // TODO: unknown/unsupported
                            break;
                        case 0:
                            // shouldn't happen
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var vector = DirectionVectors[direction];
                    var delta = vector * distance;
                    commands.Add(new ShxGlyphCommandMoveCursor(delta.X, delta.Y));
                }
            }

            return commands;
        }
    }
}
