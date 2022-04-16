using System.Collections.Generic;
using System.Linq;
using Argument.Check;
using Extensions.Pack;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class MethodDeclarationSyntaxExtensions
    {
        internal static IEnumerable<Modifier> ToModifier(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            Throw.IfNull(() => methodDeclarationSyntax);

            foreach (var syntaxToken in methodDeclarationSyntax.Modifiers)
            {
                switch (syntaxToken.Text)
                {
                    case "public":
                        yield return Modifier.Public;
                        break;
                    case "internal":
                        yield return Modifier.Internal;
                        break;
                }
            }
        }

        internal static Method ToMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            Throw.IfNull(() => methodDeclarationSyntax);

            var parameters = methodDeclarationSyntax.ParameterList.ToParameters().ToList();
            var returnType = methodDeclarationSyntax.ReturnType.ToString();
            var methodName = methodDeclarationSyntax.Identifier.ValueText;
            var methodValue = methodDeclarationSyntax.ToString();
            var methodBody = methodDeclarationSyntax.Body?.ToString() == null
                ? string.Empty
                : methodDeclarationSyntax.Body.ToString();
            var statements = methodBody.IsEmpty()
                ? Enumerable.Empty<string>()
                : methodDeclarationSyntax.Body.Statements.Select(s => s.ToString()).ToList();
            var attributes = GetAttributes(methodDeclarationSyntax).ToList();
            var modifiers = methodDeclarationSyntax.ToModifier();

            return new Method(methodName, parameters, returnType, methodValue, methodBody, statements, attributes,
                modifiers);
        }

        internal static IEnumerable<Method> ToMethods(
            this IEnumerable<MethodDeclarationSyntax> methodDeclarationSyntaxes)
        {
            Throw.IfNull(() => methodDeclarationSyntaxes);

            return methodDeclarationSyntaxes.Select(m => m.ToMethod());
        }

        private static IEnumerable<Attribute> GetAttributes(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return from attrList in methodDeclarationSyntax.AttributeLists
                   from attr in attrList.Attributes
                   select new Attribute(attr.Name.ToString(), attr.ArgumentList?.Arguments.Select(arg => arg.ToString()).ToList());
        }
    }
}
