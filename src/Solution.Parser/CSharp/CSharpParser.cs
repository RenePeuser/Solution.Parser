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

        var nameSpace = syntaxTree.GetNamespaceOrDefault();

        var usings = syntaxTree.AllOfType<UsingDirectiveSyntax>().Select(u =>
        {
            var value = u.Name?.As<NameSyntax>()?.GetText().ToString() ?? string.Empty;
            return new Using(value);
        }).ToImmutableList();

        var fixedFilePath = filePath.IsNullOrWhiteSpace() ? nameSpace.Name : filePath;

        var classes = syntaxTree.AllOfType<ClassDeclarationSyntax>().ToClasses(fixedFilePath);
        var records = syntaxTree.AllOfType<RecordDeclarationSyntax>().ToRecords(fixedFilePath);
        var interfaces = syntaxTree.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(fixedFilePath);
        var enums = syntaxTree.AllOfType<EnumDeclarationSyntax>().ToEnums(fixedFilePath);
        var structs = syntaxTree.AllOfType<StructDeclarationSyntax>().ToStructs(fixedFilePath);
        var statements = syntaxTree.AllOfType<StatementSyntax>().ToStatements(fixedFilePath);

        return new CSharpSyntaxTree(nameSpace, fixedFilePath, usings, classes, records, interfaces, enums, structs, statements, syntaxTree.ToString());
    }

    public static CSharpSyntaxTree Parse(this CSharpFileInfo csharpFileInfo)
    {
        Throw.IfNull(csharpFileInfo);

        var code = File.ReadAllText(csharpFileInfo.Value.FullName);
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        return Parse(syntaxTree, csharpFileInfo.Value.FullName);
    }
}
