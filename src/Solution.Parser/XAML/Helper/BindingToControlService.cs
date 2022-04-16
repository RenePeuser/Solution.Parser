using System.Collections.Generic;
using System.Linq;

namespace Solution.Parser.XAML
{
    public static class BindingToControlService
    {
        public static Control ConvertFrom<TMarkupExtension>(this Control owner, Binding binding, int lineNumber)
            where TMarkupExtension : class
        {
            var properties = binding.ToProperties(lineNumber).ToList();

            return new Control(owner.LineNumber,
                owner.DataContext,
                owner,
                null,
                typeof(TMarkupExtension).Name,
                null,
                properties,
                Enumerable.Empty<ElementBase>(),
                Enumerable.Empty<Style>(),
                Enumerable.Empty<DataTemplate>());
        }

        private static IEnumerable<Property> ToProperties(this Binding binding, int lineNumber)
        {
            yield return new Property(lineNumber, nameof(binding.Path), binding.Path);
            yield return new Property(lineNumber, nameof(binding.Source), binding.Source);
            yield return new Property(lineNumber, nameof(binding.Converter), binding.Converter);
            yield return new Property(lineNumber, nameof(binding.ConverterParameter), binding.ConverterParameter);
            yield return new Property(lineNumber, nameof(binding.Mode), new UnknownPropertyValue(binding.Mode));
            yield return new Property(lineNumber, nameof(binding.UpdateSourceTrigger),
                new UnknownPropertyValue(binding.UpdateSourceTrigger));
            yield return new Property(lineNumber, nameof(binding.ElementName),
                new UnknownPropertyValue(binding.ElementName));
            yield return new Property(lineNumber, nameof(binding.RelativeSource), binding.RelativeSource);
            yield return new Property(lineNumber, nameof(binding.FallbackValue), binding.FallbackValue);
            yield return new Property(lineNumber, nameof(binding.StringFormat), binding.StringFormat);
        }
    }
}
