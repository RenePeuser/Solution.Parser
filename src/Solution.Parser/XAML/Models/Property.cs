using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{Name}")]
    public class Property
    {
        internal Property(int lineNumber, string name, PropertyValue propertyValue)
        {
            LineNumber = lineNumber;
            Name = name;
            PropertyValue = propertyValue;
        }

        public int LineNumber { get; }

        public string Name { get; }

        public PropertyValue PropertyValue { get; }
    }
}
