using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp;

public static class CSharpParser
{
    public static CSharpSyntaxTree Parse(this SyntaxTree syntaxTree, string filePath)
    {
        Throw.IfNull(syntaxTree);

        var root = syntaxTree.GetRoot();
        var nameSpaces = syntaxTree.GetNamespaces();
        var nameSpace = nameSpaces.FirstOrDefault() ?? new NameSpace(string.Empty);
        var fixedFilePath = filePath.IsNullOrWhiteSpace() ? nameSpace.Name : filePath;

        // Descends through namespaces but not into types, so a nested type is reported by its
        // declaring type rather than a second time at file level.
        var topLevelDeclarations = root.TopLevelTypeDeclarations();

        var types = topLevelDeclarations.Select(d => d.ToTypeDeclarationOrDefault(fixedFilePath))
                                        .Where(t => t is not null)
                                        .Select(t => t!)
                                        .ToImmutableList();

        var delegates = topLevelDeclarations.OfType<DelegateDeclarationSyntax>()
                                            .Select(d => d.ToDelegate(fixedFilePath))
                                            .ToImmutableList();

        var compilationUnit = root as CompilationUnitSyntax;

        return new CSharpSyntaxTree
        {
            NameSpace = nameSpace,
            NameSpaces = nameSpaces,
            FileName = fixedFilePath,
            Usings = root.ToUsings(fixedFilePath),
            Types = types,
            Delegates = delegates,
            Statements = compilationUnit?.ToTopLevelStatements(fixedFilePath) ?? ImmutableList<Statement>.Empty,
            AssemblyAttributes = compilationUnit?.AttributeLists.ToAttributes(fixedFilePath) ?? ImmutableList<Attribute>.Empty,
            Diagnostics = syntaxTree.ToParseDiagnostics(fixedFilePath),
            SyntaxTree = syntaxTree.ToString()
        };
    }

    public static CSharpSyntaxTree Parse(this CSharpFileInfo csharpFileInfo)
    {
        Throw.IfNull(csharpFileInfo);

        var code = File.ReadAllText(csharpFileInfo.Value.FullName);
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);

        return Parse(syntaxTree, csharpFileInfo.Value.FullName);
    }
}
