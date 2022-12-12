using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using Argument.Check;

namespace Solution.Parser.XAML
{
    internal sealed class RootSelector : IRootSelector
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
            Throw.IfNull(document.Root);

            var builder = _rootBuilders.FirstOrDefault(builder => builder.IsThisTheSelectorFor(document.Root.Name.LocalName));
            return Throw.IfNull(builder);
        }
    }
}
