using System.Collections.Generic;
using System.Collections.Immutable;
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
                    case "abstract":
                        yield return Modifier.Abstract;
                        break;
                }
            }
        }

        internal static Struct ToStruct(this StructDeclarationSyntax structDeclarationSyntax)
        {
            var modifiers = structDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = structDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors().ToImmutableList();
            var properties = structDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToImmutableList();
            var methods = structDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToImmutableList();
            var nameSpace = structDeclarationSyntax.SyntaxTree.GetNamespace();
            var attributesOfClass = structDeclarationSyntax.AttributeLists.ToAttributes().ToImmutableList();
            var fields = structDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields().ToImmutableList();
            var baseTypes = structDeclarationSyntax.BaseList != null ? structDeclarationSyntax.BaseList.ToBaseTypes().ToImmutableList() : ImmutableList<BaseType>.Empty;
            var interfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var events = structDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToImmutableList();
            var eventFields = structDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields().ToImmutableList();
            var nestedClasses = structDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToImmutableList();
            var nestedStructs = structDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToImmutableList();
            var nestedEnums = structDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToImmutableList();
            var nestedInterfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces().ToImmutableList();
            var name = structDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = structDeclarationSyntax.ToString();
            var fullQualifiedName = $"{nameSpace.Name}.{name}";

            var parameters = constructors.MaxBy(c => c.Parameters.Count)?.Parameters ?? ImmutableList<Parameter>.Empty;

            return new Struct(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass,
                fields, interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, syntaxTree, fullQualifiedName);
        }

        internal static IImmutableList<Struct> ToStructs(
            this IImmutableList<StructDeclarationSyntax> classDeclarationSyntaxes)
        {
            return classDeclarationSyntaxes.Select(ToStruct).ToImmutableList();
        }
    }
}
