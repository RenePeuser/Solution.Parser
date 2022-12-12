using Argument.Check;

namespace Solution.Parser.XAML
{
    public class TemplateBinding : PropertyValue
    {
        public TemplateBinding(string value, PropertyValue? propertyValue) : base(value)
        {
            Throw.IfNull(propertyValue);

            Property = propertyValue;
        }

        public PropertyValue? Property { get; }
    }
}
