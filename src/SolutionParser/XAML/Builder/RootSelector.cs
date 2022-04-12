using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SolutionParser.XAML
{
    internal class RootSelector : IRootSelector
    {
        private readonly IEnumerable<IConcreteRootBuilder> _rootBuilders;

        internal RootSelector() : this(new IConcreteRootBuilder[]
        {
            new WindowRootBuilder(), new ResourceDictionaryRootBuilder(), new UserControlRootBuilder(),
            new DefaultRootBuilder()
        })
        {
        }

        private RootSelector(IEnumerable<IConcreteRootBuilder> rootBuilders)
        {
            _rootBuilders = rootBuilders.ToList();
        }

        public IConcreteRootBuilder GetRootBuilderFor(XDocument document)
        {
            return _rootBuilders.FirstOrDefault(builder => builder.IsThisTheSelectorFor(document.Root.Name.LocalName));
        }
    }
}
