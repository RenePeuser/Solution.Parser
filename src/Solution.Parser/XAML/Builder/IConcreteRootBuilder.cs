using System;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal interface IConcreteRootBuilder
    {
        Predicate<string> IsThisTheSelectorFor { get; }

        Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo);
    }
}
