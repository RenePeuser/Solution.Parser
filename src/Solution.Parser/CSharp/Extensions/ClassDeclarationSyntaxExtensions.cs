using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class ClassDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this ClassDeclarationSyntax classDeclarationSyntax)
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

        internal static Class ToClass(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            var modifiers = classDeclarationSyntax.ToModifiers().ToList();
            var constructors = classDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToList();
            var properties = classDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToList();
            var methods = classDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToList();
            var nameSpace = classDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>().First().ToNamespace();
            var attributesOfClass = classDeclarationSyntax.AttributeLists.ToAttributes().ToList();
            var fields = classDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToList();
            var baseTypes = classDeclarationSyntax.BaseList != null ? classDeclarationSyntax.BaseList.ToBaseTypes().ToList() : Enumerable.Empty<BaseType>();
            var interfaces = classDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var events = classDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToList();
            var eventFields = classDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToList();
            var nestedClasses = classDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToList();
            var nestedStructs = classDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToList();
            var nestedEnums = classDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToList();
            var nestedInterfaces = classDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToList();
            var name = classDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = classDeclarationSyntax.ToString();

            return new Class(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, syntaxTree);
        }

        internal static IEnumerable<Class> ToClasses(this IEnumerable<ClassDeclarationSyntax> classDeclarationSyntaxes)
        {
            return classDeclarationSyntaxes.Select(ToClass);
        }
    }
}
