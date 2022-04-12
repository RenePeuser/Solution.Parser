using System.Collections.Generic;
using Argument.Check;

namespace SolutionParser.CSharp
{
    public class Constructor
    {
        internal Constructor(IEnumerable<Parameter> parameters, IEnumerable<string> arguments,
            IEnumerable<Modifier> modifiers)
        {
            Throw.IfNull(() => parameters);
            Throw.IfNull(() => arguments);
            Throw.IfNull(() => modifiers);

            Parameters = parameters;
            Arguments = arguments;
            Modifiers = modifiers;
        }

        public IEnumerable<Parameter> Parameters { get; }

        public IEnumerable<string> Arguments { get; }

        public IEnumerable<Modifier> Modifiers { get; }
    }
}
