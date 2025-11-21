using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.CSharp.Models;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{FileName}")]
    public record CSharpSyntaxTree(NameSpace NameSpace,
                                   string FileName,
                                   ImmutableList<Using> Usings,
                                   ImmutableList<Class> Classes,
                                   ImmutableList<Record> Records,
                                   ImmutableList<Interface> Interfaces,
                                   ImmutableList<Enum> Enums,
                                   ImmutableList<Struct> Structs,
                                   ImmutableList<Statement> Statements,
                                   string SyntaxTree)
    {
    }
}
