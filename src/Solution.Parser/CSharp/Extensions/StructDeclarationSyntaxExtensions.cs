using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class StructDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this StructDeclarationSyntax classDeclarationSyntax)
        {
            foreach (var syntaxToken in classDeclarationSyntax.Modifiers)
            {
                switch (syntaxToken.Text)
                {
                    case "public":
                        yield return Modifier.Public;
                        break;
                    case "internal":
                        yield return Modifier.Internal;
                        break;
                    case "protected":
                        yield return Modifier.Protected;
                        break;
                    case "private":
                        yield return Modifier.Private;
                        break;
                    case "static":
                        yield return Modifier.Static;
                        break;
                    case "const":
                        yield return Modifier.Const;
                        break;
                }
            }
        }

        internal static Struct ToStruct(this StructDeclarationSyntax structDeclarationSyntax)
        {
            var modifiers = structDeclarationSyntax.ToModifiers().ToList();
            var constructors = structDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToList();
            var properties = structDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToList();
            var methods = structDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToList();
            var nameSpace = structDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>().First().ToNamespace();
            var attributesOfClass = structDeclarationSyntax.AttributeLists.ToAttributes().ToList();
            var fields = structDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToList();
            var baseTypes = structDeclarationSyntax.BaseList != null ? structDeclarationSyntax.BaseList.ToBaseTypes().ToList() : Enumerable.Empty<BaseType>();
            var interfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var events = structDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToList();
            var eventFields = structDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToList();
            var nestedClasses = structDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToList();
            var nestedStructs = structDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToList();
            var nestedEnums = structDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToList();
            var nestedInterfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var name = structDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = structDeclarationSyntax.ToString();

            return new Struct(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass,
                fields, interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, syntaxTree);
        }

        internal static IEnumerable<Struct> ToStructs(
            this IEnumerable<StructDeclarationSyntax> classDeclarationSyntaxes)
        {
            return classDeclarationSyntaxes.Select(ToStruct);
        }
    }
}
