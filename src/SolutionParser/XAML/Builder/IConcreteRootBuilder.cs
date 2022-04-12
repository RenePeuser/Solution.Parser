using System;
using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal interface IConcreteRootBuilder
    {
        Predicate<string> IsThisTheSelectorFor { get; }

        Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo);
    }
}
