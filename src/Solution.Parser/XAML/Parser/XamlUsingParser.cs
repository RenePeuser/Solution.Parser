using System;
using System.Linq;
using Extensions.Pack;

namespace Solution.Parser.XAML
{
    internal sealed class XamlUsingParser : PropertyValueParserBase
    {
        public override Predicate<string> IsThisTheCorrectParserFor { get; } =
            item => item.StartWith("clr-namespace:");

        public override PropertyValue? Parse(string value, int lineNumber)
        {
            var withoutXmlns = value.Replace("clr-namespace:", string.Empty);
            var nameSpaceWithAssembly = withoutXmlns.Split(';');
            var nameSpace = nameSpaceWithAssembly[0];
            var assembly = nameSpaceWithAssembly.Length > 1 ? nameSpaceWithAssembly[1].Split('=').Last() : string.Empty;

            return new XamlUsing(value, string.Empty, nameSpace, assembly);
        }
    }
}
