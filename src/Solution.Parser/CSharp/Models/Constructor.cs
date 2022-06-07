using System.Collections.Immutable;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    public class Constructor
    {
        internal Constructor(IImmutableList<Parameter> parameters, IImmutableList<string> arguments, IImmutableList<Modifier> modifiers)
        {
            Throw.IfNull(parameters);
            Throw.IfNull(arguments);
            Throw.IfNull(modifiers);

            Parameters = parameters;
            Arguments = arguments;
            Modifiers = modifiers;
        }

        public IImmutableList<Parameter> Parameters { get; }

        public IImmutableList<string> Arguments { get; }

        public IImmutableList<Modifier> Modifiers { get; }
    }
}
