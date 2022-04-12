using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal class RootBuilder : IRootBuilder
    {
        private readonly IRootSelector _rootSelector;

        internal RootBuilder() : this(new RootSelector())
        {
        }

        private RootBuilder(IRootSelector rootSelector)
        {
            _rootSelector = rootSelector;
        }

        public Root BuildFrom(XDocument document, IXamlFileInfo xamlFileInfo)
        {
            var builder = _rootSelector.GetRootBuilderFor(document);
            return builder.BuildFrom(document, xamlFileInfo);
        }
    }
}
