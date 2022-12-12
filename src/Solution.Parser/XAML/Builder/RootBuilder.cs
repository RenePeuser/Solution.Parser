using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal sealed class RootBuilder : IRootBuilder
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
