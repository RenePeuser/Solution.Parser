namespace Solution.Parser.XAML
{
    public class Binding : PropertyValue
    {
        internal Binding(string value, PropertyValue path, PropertyValue source, PropertyValue converter,
            PropertyValue converterParameter, string mode, string updateSourceTrigger, string elementName,
            RelativeSource relativeSource, PropertyValue fallbackValue, StringFormat stringFormat) : base(value)
        {
            Path = path;
            Source = source;
            Converter = converter;
            ConverterParameter = converterParameter;
            Mode = mode;
            UpdateSourceTrigger = updateSourceTrigger;
            ElementName = elementName;
            FallbackValue = fallbackValue;
            RelativeSource = relativeSource;
            StringFormat = stringFormat;
        }


        public PropertyValue Path { get; }

        public PropertyValue Source { get; }

        public PropertyValue Converter { get; }

        public PropertyValue ConverterParameter { get; }

        public string Mode { get; }

        public string UpdateSourceTrigger { get; }

        public string ElementName { get; }

        public RelativeSource RelativeSource { get; }

        public PropertyValue FallbackValue { get; }

        public StringFormat StringFormat { get; }
    }
}
