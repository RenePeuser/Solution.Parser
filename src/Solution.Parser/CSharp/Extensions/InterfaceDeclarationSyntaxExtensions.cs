using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class InterfaceDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifiers(this InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            foreach (var syntaxToken in interfaceDeclarationSyntax.Modifiers)
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
                }
            }
        }

        internal static IEnumerable<Interface> ToInterfaces(
            this IEnumerable<InterfaceDeclarationSyntax> interfaceDeclarationSyntaxes)
        {
            return interfaceDeclarationSyntaxes.Select(ToInterface);
        }

        internal static Interface ToInterface(this InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            Throw.IfNull(() => interfaceDeclarationSyntax);

            var name = interfaceDeclarationSyntax.Identifier.ValueText;
            var modifiers = interfaceDeclarationSyntax.ToModifiers().ToList();
            var properties = interfaceDeclarationSyntax.AllOfType<PropertyDeclarationSyntax>().ToProperties().ToList();
            var methods = interfaceDeclarationSyntax.AllOfType<MethodDeclarationSyntax>().ToMethods().ToList();
            var nameSpace = interfaceDeclarationSyntax.SyntaxTree.AllOfType<NamespaceDeclarationSyntax>().First()
                .ToNamespace();
            var attributesOfClass = interfaceDeclarationSyntax.AttributeLists.ToAttributes().ToList();
            var baseTypes = interfaceDeclarationSyntax.BaseList != null
                ? interfaceDeclarationSyntax.BaseList.ToBaseTypes().ToList()
                : Enumerable.Empty<BaseType>();
            var events = interfaceDeclarationSyntax.AllOfType<EventDeclarationSyntax>().ToEvents().ToList();
            var eventFields = interfaceDeclarationSyntax.AllOfType<EventFieldDeclarationSyntax>().ToEventFields()
                .ToList();
            var nestedClasses = interfaceDeclarationSyntax.AllOfType<ClassDeclarationSyntax>().ToClasses().ToList();
            var nestedStructs = interfaceDeclarationSyntax.AllOfType<StructDeclarationSyntax>().ToStructs().ToList();
            var nestedEnums = interfaceDeclarationSyntax.AllOfType<EnumDeclarationSyntax>().ToEnums().ToList();
            var nestedInterfaces = interfaceDeclarationSyntax.AllOfType<InterfaceDeclarationSyntax>().ToInterfaces()
                .ToList();

            return new Interface(nameSpace, name, modifiers, properties, methods, attributesOfClass, baseTypes,
                events, eventFields, nestedClasses, nestedStructs, nestedEnums, nestedInterfaces);
        }
    }
}
