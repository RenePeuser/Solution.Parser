using System.Collections.Immutable;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public record Method : DeclarationWithModifiers
    {
        internal Method(
            string name,
            IImmutableList<Parameter> parameters,
            string returnParameter,
            string methodValue,
            string methodBody,
            IImmutableList<string> statements,
            IImmutableList<Attribute> attributes,
            IImmutableList<Modifier> modifiers) : base(name, modifiers)
        {
            Parameters = parameters;
            ReturnParameter = returnParameter;
            MethodValue = methodValue;
            MethodBody = methodBody;
            Statements = statements;
            Attributes = attributes;
        }

        public IImmutableList<Parameter> Parameters { get; }

        public string ReturnParameter { get; }

        public string MethodValue { get; }

        public string MethodBody { get; }

        public IImmutableList<string> Statements { get; }

        public IImmutableList<Attribute> Attributes { get; }
    }
}
