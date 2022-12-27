using System.Collections.Generic;
using System.Collections.Immutable;
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
                    case "abstract":
                        yield return Modifier.Abstract;
                        break;
                }
            }
        }

        internal static Class ToClass(this ClassDeclarationSyntax classDeclarationSyntax)
        {
            var modifiers = classDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = classDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToImmutableList();
            var properties = classDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToImmutableList();
            var methods = classDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToImmutableList();
            var nameSpace = classDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>()[0].ToNamespace();
            var attributesOfClass = classDeclarationSyntax.AttributeLists.ToAttributes().ToImmutableList();
            var fields = classDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToImmutableList();
            var baseTypes = classDeclarationSyntax.BaseList != null ? classDeclarationSyntax.BaseList.ToBaseTypes().ToImmutableList() : ImmutableList<BaseType>.Empty;
            var interfaces = classDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var events = classDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToImmutableList();
            var eventFields = classDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToImmutableList();
            var nestedClasses = classDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToImmutableList();
            var nestedStructs = classDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToImmutableList();
            var nestedEnums = classDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToImmutableList();
            var nestedInterfaces = classDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var name = classDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = classDeclarationSyntax.ToString();
            var fullQualifiedName = $"{nameSpace.Name}.{name}";
            var parameters = constructors.MaxBy(c => c.Parameters.Count)?.Parameters ?? ImmutableList<Parameter>.Empty;

            return new Class(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass, fields,
                interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, fullQualifiedName, syntaxTree);
        }

        internal static IImmutableList<Class> ToClasses(this IImmutableList<ClassDeclarationSyntax> classDeclarationSyntaxes)
        {
            return classDeclarationSyntaxes.Select(ToClass).ToImmutableList();
        }
    }
}
