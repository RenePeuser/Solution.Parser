using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public class CSharpSyntaxTree
    {
        private static readonly CSharpSyntaxTree sEmptyNameSpace = new CSharpSyntaxTree(new NameSpace(string.Empty),
                                                                                        Enumerable.Empty<Using>(),
                                                                                        Enumerable.Empty<Class>(),
                                                                                        Enumerable.Empty<Interface>(),
                                                                                        Enumerable.Empty<Enum>(),
                                                                                        Enumerable.Empty<Struct>());

        internal CSharpSyntaxTree(NameSpace nameSpace,
                                IEnumerable<Using> usings,
                                IEnumerable<Class> classes,
                                IEnumerable<Interface> interfaces,
                                IEnumerable<Enum> enums,
                                IEnumerable<Struct> structs)
        {
            Throw.IfNull(() => nameSpace);
            Throw.IfNull(() => usings);
            Throw.IfNull(() => classes);
            Throw.IfNull(() => interfaces);
            Throw.IfNull(() => enums);
            Throw.IfNull(() => structs);

            NameSpace = nameSpace;
            Classes = classes;
            FileName = nameSpace.Name;
            Usings = usings;
            Interfaces = interfaces;
            Enums = enums;
            Structs = structs;
        }

        public string FileName { get; }

        public NameSpace NameSpace { get; }

        public IEnumerable<Class> Classes { get; }

        public IEnumerable<Enum> Enums { get; }

        public IEnumerable<Struct> Structs { get; }

        public IEnumerable<Interface> Interfaces { get; }

        public IEnumerable<Using> Usings { get; }

        public static CSharpSyntaxTree Empty()
        {
            return sEmptyNameSpace;
        }
    }
}
