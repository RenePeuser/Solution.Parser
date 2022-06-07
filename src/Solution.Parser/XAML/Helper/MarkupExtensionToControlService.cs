using System.Collections.Immutable;

namespace Solution.Parser.XAML
{
    public static class MarkupExtensionToControlService
    {
        public static Control ConvertFrom<TMarkupExtension>(this Control owner, MarkupExtension markupExtension)
            where TMarkupExtension : class
        {
            return new Control(owner.LineNumber,
                owner.DataContext,
                owner,
                null,
                typeof(TMarkupExtension).Name,
                null,
                markupExtension.Properties,
                ImmutableList<ElementBase>.Empty,
                ImmutableList<Style>.Empty,
                ImmutableList<DataTemplate>.Empty);
        }
    }
}
