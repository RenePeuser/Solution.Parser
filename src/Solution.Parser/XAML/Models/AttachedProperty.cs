using System.Diagnostics;

namespace Solution.Parser.XAML
{
    [DebuggerDisplay("{FullQualifiedName}")]
    public class AttachedProperty : Property
    {
        internal AttachedProperty(int lineNumber, string className, string propertyName, string clrPropertyName,
            PropertyValue propertyValue) : base(lineNumber, propertyName, propertyValue)
        {
            ClassName = className;
            FullQualifiedName = $"{className}.{propertyName}";
            CLRPropertyName = clrPropertyName;
        }

        public string ClassName { get; }

        public string FullQualifiedName { get; }

        public string CLRPropertyName { get; }
    }
}
