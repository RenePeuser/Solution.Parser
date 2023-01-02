using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            Throw.IfNull(methodDeclarationSyntax);

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

        internal static Method ToMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
        {
            Throw.IfNull(methodDeclarationSyntax);

            var parameters = methodDeclarationSyntax.ParameterList.ToParameters().ToImmutableList();
            var returnType = methodDeclarationSyntax.ReturnType.ToString();
            var methodName = methodDeclarationSyntax.Identifier.ValueText;

            if (methodName.Contains("AddAuthorizeWithUserSecretsMiddleware"))
            {

            }

            var methodValue = methodDeclarationSyntax.ToString();
            var methodBody = methodDeclarationSyntax.Body?.ToString() == null ? string.Empty : methodDeclarationSyntax.Body.ToString();
            var statements = methodBody.IsEmpty() ? ImmutableList<string>.Empty : methodDeclarationSyntax.Body?.Statements.Select(s => s.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty;
            var attributes = GetAttributes(methodDeclarationSyntax).ToImmutableList();
            var modifiers = methodDeclarationSyntax.ToModifier().ToImmutableList();
            var lineStatementsRaw = methodBody.Split(Environment.NewLine).ToImmutableList();
            var lineStatements = lineStatementsRaw.Take(new Range(1, lineStatementsRaw.Count - 1)).FilterNullOrWhitespace().ToImmutableList();


            return new Method(methodName, parameters, returnType, methodValue, methodBody, statements, attributes, modifiers, lineStatements, methodDeclarationSyntax.SyntaxTree.ToString());
        }

        internal static IImmutableList<Method> ToMethods(
            this IImmutableList<MethodDeclarationSyntax> methodDeclarationSyntaxes)
        {
            Throw.IfNull(methodDeclarationSyntaxes);

            return methodDeclarationSyntaxes.Select(m => m.ToMethod()).ToImmutableList();
        }

        private static IImmutableList<Attribute> GetAttributes(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return (from attrList in methodDeclarationSyntax.AttributeLists
                    from attr in attrList.Attributes
                    select new Attribute(attr.Name.ToString(), attr.ArgumentList?.Arguments.Select(arg => arg.ToString()).ToImmutableList() ?? ImmutableList<string>.Empty, attr.Parent?.ToString() ?? string.Empty)).ToImmutableList();
        }
    }
}
