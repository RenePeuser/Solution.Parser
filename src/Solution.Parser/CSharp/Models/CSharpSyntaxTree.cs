using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// One parsed C# file. Every list holds the declarations made at file level; a nested type belongs
    /// to its declaring type. Use <c>AllTypes()</c> and the other query extensions to walk everything.
    /// </summary>
    [DebuggerDisplay("{FileName}")]
    public record CSharpSyntaxTree
    {
        public required NameSpace NameSpace { get; init; }

        public required string FileName { get; init; }

        public ImmutableList<Using> Usings { get; init; } = ImmutableList<Using>.Empty;

        /// <summary>Every type declared at file level, in source order, regardless of kind.</summary>
        public ImmutableList<TypeDeclaration> Types { get; init; } = ImmutableList<TypeDeclaration>.Empty;

        public ImmutableList<Delegate> Delegates { get; init; } = ImmutableList<Delegate>.Empty;

        /// <summary>The top level statements of the file, empty unless it is the program entry file.</summary>
        public ImmutableList<Statement> Statements { get; init; } = ImmutableList<Statement>.Empty;

        /// <summary>Assembly and module level attributes, as in <c>[assembly: InternalsVisibleTo(...)]</c>.</summary>
        public ImmutableList<Attribute> AssemblyAttributes { get; init; } = ImmutableList<Attribute>.Empty;

        /// <summary>What Roslyn reported while parsing, see <see cref="ParseDiagnostic"/>.</summary>
        public ImmutableList<ParseDiagnostic> Diagnostics { get; init; } = ImmutableList<ParseDiagnostic>.Empty;

        /// <summary>The source of the whole file.</summary>
        public required string SyntaxTree { get; init; }

        /// <summary>Every namespace declared in the file. A file may hold more than one.</summary>
        public ImmutableList<NameSpace> NameSpaces { get; init; } = ImmutableList<NameSpace>.Empty;

        public ImmutableList<Class> Classes => Types.OfType<Class>().ToImmutableList();

        public ImmutableList<Record> Records => Types.OfType<Record>().ToImmutableList();

        public ImmutableList<Interface> Interfaces => Types.OfType<Interface>().ToImmutableList();

        public ImmutableList<Enum> Enums => Types.OfType<Enum>().ToImmutableList();

        public ImmutableList<Struct> Structs => Types.OfType<Struct>().ToImmutableList();

        public bool HasParseErrors => Diagnostics.Any(d => d.IsError);
    }
}
