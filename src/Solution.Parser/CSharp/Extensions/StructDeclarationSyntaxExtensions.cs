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
                    case "partial":
                        yield return Modifier.Partial;
                        break;
                    case "required":
                        yield return Modifier.Required;
                        break;
                }
            }
        }

        internal static Struct ToStruct(this StructDeclarationSyntax structDeclarationSyntax, string filePath)
        {
            var modifiers = structDeclarationSyntax.ToModifiers().ToImmutableList();
            var constructors = structDeclarationSyntax.AllOfType<ConstructorDeclarationSyntax>().ToConstructors(filePath);
            var properties = structDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties(filePath);
            var methods = structDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods(filePath);
            var nameSpace = structDeclarationSyntax.SyntaxTree.GetNamespace();
            var attributesOfClass = structDeclarationSyntax.AttributeLists.ToAttributes(filePath);
            var fields = structDeclarationSyntax.AllOfType<FieldDeclarationSyntax>().ToFields(filePath);
            var baseTypes = structDeclarationSyntax.BaseList?.ToBaseTypes() ?? ImmutableList<BaseType>.Empty;
            var interfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath);
            var events = structDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents(filePath);
            var eventFields = structDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields(filePath);
            var nestedClasses = structDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses(filePath);
            var nestedStructs = structDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs(filePath);
            var nestedEnums = structDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums(filePath);
            var nestedInterfaces = structDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces(filePath);
            var name = structDeclarationSyntax.Identifier.ValueText;
            var syntaxTree = structDeclarationSyntax.ToString();
            var fullQualifiedName = $"{nameSpace.Name}.{name}";

            var parameters = constructors.MaxBy(c => c.Parameters.Count)?.Parameters ?? ImmutableList<Parameter>.Empty;

            return new Struct(nameSpace, name, modifiers, constructors, properties, methods, attributesOfClass,
                fields, interfaces, baseTypes, events, eventFields, nestedClasses, nestedStructs, nestedEnums,
                nestedInterfaces, parameters, syntaxTree, fullQualifiedName, filePath);
        }

        internal static ImmutableList<Struct> ToStructs(this ImmutableList<StructDeclarationSyntax> classDeclarationSyntaxes,
                                                         string filePath)
        {
            return classDeclarationSyntaxes.Select(item => item.ToStruct(filePath)).ToImmutableList();
        }
    }
}
