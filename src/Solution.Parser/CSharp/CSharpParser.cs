using System.IO;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    public static class CSharpParser
    {
        public static CSharpSyntaxTree Parse(this SyntaxTree syntaxTree)
        {
            Throw.IfNull(syntaxTree);

            var namespaceDeclarationSyntax = syntaxTree.AllOfType<NamespaceDeclarationSyntax>().FirstOrDefault();

            if (namespaceDeclarationSyntax.IsNull())
            {
                return CSharpSyntaxTree.Empty();
            }

            var nameSpace = namespaceDeclarationSyntax.ToNamespace();

            var usings = syntaxTree.AllOfType<UsingDirectiveSyntax>().Select(u => new Using(u.Name.Cast<NameSyntax>().GetText().ToString())).ToList();
            var classes = syntaxTree.AllOfType<ClassDeclarationSyntax>().ToClasses().ToList();
            var records = syntaxTree.AllOfType<RecordDeclarationSyntax>().ToRecords().ToList();
            var interfaces = syntaxTree.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var enums = syntaxTree.AllOfType<EnumDeclarationSyntax>().ToEnums().ToList();
            var structs = syntaxTree.AllOfType<StructDeclarationSyntax>().ToStructs().ToList();

            return new CSharpSyntaxTree(nameSpace, usings, classes, records, interfaces, enums, structs);
        }

        public static CSharpSyntaxTree Parse(this CSharpFileInfo csharpFileInfo)
        {
            Throw.IfNull(csharpFileInfo);

            var code = File.ReadAllText(csharpFileInfo.Value.FullName);
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            return Parse(syntaxTree);
        }
    }
}
