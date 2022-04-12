using System.Diagnostics;

namespace SolutionParser.XAML
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
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
