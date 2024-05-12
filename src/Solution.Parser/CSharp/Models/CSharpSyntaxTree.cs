using System.Collections.Immutable;
using System.Diagnostics;
using Solution.Parser.CSharp.Models;

namespace Solution.Parser.CSharp
{
    [DebuggerDisplay("{FileName}")]
    public record CSharpSyntaxTree(NameSpace NameSpace,
                                   string FileName,
                                   IImmutableList<Using> Usings,
                                   IImmutableList<Class> Classes,
                                   IImmutableList<Record> Records,
                                   IImmutableList<Interface> Interfaces,
                                   IImmutableList<Enum> Enums,
                                   IImmutableList<Struct> Structs,
                                   IImmutableList<Statement> Statements,
                                   string SyntaxTree)
    {
    }
}
