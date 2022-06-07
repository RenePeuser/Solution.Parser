using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace Solution.Parser.XAML
{
    internal class RootSelector : IRootSelector
    {
        private readonly IImmutableList<IConcreteRootBuilder> _rootBuilders;

        internal RootSelector() : this(ImmutableList.Create<IConcreteRootBuilder>(new WindowRootBuilder(),
                                                                                  new ResourceDictionaryRootBuilder(),
                                                                                  new UserControlRootBuilder(),
                                                                                  new DefaultRootBuilder()))
        {
        }

        private RootSelector(IImmutableList<IConcreteRootBuilder> rootBuilders)
        {
            _rootBuilders = rootBuilders.ToImmutableList();
        }

        public IConcreteRootBuilder GetRootBuilderFor(XDocument document)
        {
            return _rootBuilders.FirstOrDefault(builder => builder.IsThisTheSelectorFor(document.Root.Name.LocalName));
        }
    }
}
