using System.Linq;

namespace SolutionParser.XAML
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
                Enumerable.Empty<ElementBase>(),
                Enumerable.Empty<Style>(),
                Enumerable.Empty<DataTemplate>());
        }
    }
}
