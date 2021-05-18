using System.Collections.Generic;

namespace IxMilia.Shx
{
    internal class ShxGlyphCommandData
    {
        private Dictionary<ushort, string> _names = new Dictionary<ushort, string>();
        private Dictionary<ushort, IEnumerable<ShxGlyphCommand>> _commands = new Dictionary<ushort, IEnumerable<ShxGlyphCommand>>();

        public IReadOnlyDictionary<ushort, string> Names => _names;
        public IReadOnlyDictionary<ushort, IEnumerable<ShxGlyphCommand>> Commands => _commands;

        public void AddGlyphCommands(ushort code, string name, IEnumerable<ShxGlyphCommand> commands)
        {
            _names.Add(code, name);
            _commands.Add(code, commands);
        }
    }
}
