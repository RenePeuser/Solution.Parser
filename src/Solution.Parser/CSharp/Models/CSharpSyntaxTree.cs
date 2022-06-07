using System.Collections.Immutable;
using System.Diagnostics;
using Argument.Check;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public class CSharpSyntaxTree
    {
        private static readonly CSharpSyntaxTree sEmptyNameSpace = new(new NameSpace(string.Empty),
                                                                        ImmutableList<Using>.Empty,
                                                                        ImmutableList<Class>.Empty,
                                                                        ImmutableList<Record>.Empty,
                                                                        ImmutableList<Interface>.Empty,
                                                                        ImmutableList<Enum>.Empty,
                                                                        ImmutableList<Struct>.Empty);

        internal CSharpSyntaxTree(NameSpace nameSpace,
                                IImmutableList<Using> usings,
                                IImmutableList<Class> classes,
                                IImmutableList<Record> records,
                                IImmutableList<Interface> interfaces,
                                IImmutableList<Enum> enums,
                                IImmutableList<Struct> structs)
        {
            Throw.IfNull(nameSpace);
            Throw.IfNull(usings);
            Throw.IfNull(classes);
            Throw.IfNull(records);
            Throw.IfNull(interfaces);
            Throw.IfNull(enums);
            Throw.IfNull(structs);

            NameSpace = nameSpace;
            Classes = classes;
            FileName = nameSpace.Name;
            Usings = usings;
            Interfaces = interfaces;
            Enums = enums;
            Structs = structs;
            Records = records;
        }

        public string FileName { get; }

        public NameSpace NameSpace { get; }

        public IImmutableList<Class> Classes { get; }

        public IImmutableList<Record> Records { get; }

        public IImmutableList<Enum> Enums { get; }

        public IImmutableList<Struct> Structs { get; }

        public IImmutableList<Interface> Interfaces { get; }

        public IImmutableList<Using> Usings { get; }

        public static CSharpSyntaxTree Empty()
        {
            return sEmptyNameSpace;
        }
    }
}
