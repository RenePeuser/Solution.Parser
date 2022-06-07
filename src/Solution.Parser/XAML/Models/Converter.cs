using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Name}")]
    internal class Converter
    {
        internal Converter(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public string Type { get; }
    }
}
