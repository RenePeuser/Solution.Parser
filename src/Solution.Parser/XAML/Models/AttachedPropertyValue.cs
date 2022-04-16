namespace Solution.Parser.XAML
{
    public class AttachedPropertyValue : PropertyValue
    {
        internal AttachedPropertyValue(string value, string className, string propertyName) : base(value)
        {
            ClassName = className;
            PropertyName = propertyName;
            FullQualifiedName = $"{className}.{propertyName}";
        }

        public string ClassName { get; }

        public string FullQualifiedName { get; }

        public string PropertyName { get; }
    }
}
