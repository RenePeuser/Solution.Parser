using System.Collections.Immutable;

namespace Solution.Parser.XAML
{
    public static class MarkupExtensionToControlService
    {
        public static Control ConvertFrom<TMarkupExtension>(this Control? owner, MarkupExtension markupExtension)
            where TMarkupExtension : class
        {
            return new Control(owner?.LineNumber ?? 0,
                owner?.DataContext,
                owner,
                string.Empty,
                typeof(TMarkupExtension).Name,
                string.Empty,
                markupExtension.Properties,
                ImmutableList<ElementBase>.Empty,
                ImmutableList<Style>.Empty,
                ImmutableList<DataTemplate>.Empty);
        }
    }
}
