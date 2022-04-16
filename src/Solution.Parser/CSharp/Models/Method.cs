using System.Collections.Generic;
using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Method : DeclarationWithModifiers
    {
        internal Method(
            string name,
            IEnumerable<Parameter> parameters,
            string returnParameter,
            string methodValue,
            string methodBody,
            IEnumerable<string> statements,
            IEnumerable<Attribute> attributes,
            IEnumerable<Modifier> modifiers) : base(name, modifiers)
        {
            Parameters = parameters;
            ReturnParameter = returnParameter;
            MethodValue = methodValue;
            MethodBody = methodBody;
            Statements = statements;
            Attributes = attributes;
        }

        public IEnumerable<Parameter> Parameters { get; }

        public string ReturnParameter { get; }

        public string MethodValue { get; }

        public string MethodBody { get; }

        public IEnumerable<string> Statements { get; }

        public IEnumerable<Attribute> Attributes { get; }
    }
}
