using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solution.Parser.CSharp
{
    internal static class AccessorExtensions
    {
        /// <summary>
        /// Reads the accessors from the declaration itself rather than from its source text, so an
        /// expression bodied setter, a setter with a block and a setter mentioned in a comment are told
        /// apart correctly.
        /// </summary>
        internal static PropertyAccessors ToAccessors(this AccessorListSyntax? accessorList,
                                                      ArrowExpressionClauseSyntax? expressionBody,
                                                      string filePath)
        {
            // An expression bodied member is a getter and nothing else.
            if (accessorList is null)
            {
                return expressionBody is null
                           ? PropertyAccessors.None
                           : new PropertyAccessors(expressionBody.ToExpressionBodiedGetter(filePath), null, null);
            }

            return new PropertyAccessors(accessorList.Find(SyntaxKind.GetAccessorDeclaration, filePath),
                                         accessorList.Find(SyntaxKind.SetAccessorDeclaration, filePath),
                                         accessorList.Find(SyntaxKind.InitAccessorDeclaration, filePath));
        }

        /// <summary>True when every accessor is a bare semicolon, so the compiler supplies the backing field.</summary>
        internal static bool IsAutoImplemented(this AccessorListSyntax? accessorList)
        {
            return accessorList is not null &&
                   accessorList.Accessors.Count > 0 &&
                   accessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null);
        }

        internal static Accessor? Find(this AccessorListSyntax? accessorList, SyntaxKind kind, string filePath)
        {
            return accessorList?.Accessors.FirstOrDefault(a => a.IsKind(kind)) is { } accessor
                       ? accessor.ToAccessor(filePath)
                       : null;
        }

        private static Accessor ToAccessor(this AccessorDeclarationSyntax accessor, string filePath)
        {
            var kind = accessor.Kind() switch
            {
                SyntaxKind.SetAccessorDeclaration => AccessorKind.Set,
                SyntaxKind.InitAccessorDeclaration => AccessorKind.Init,
                SyntaxKind.AddAccessorDeclaration => AccessorKind.Add,
                SyntaxKind.RemoveAccessorDeclaration => AccessorKind.Remove,
                _ => AccessorKind.Get
            };

            var name = accessor.Keyword.ValueText;

            return Build(kind,
                         name,
                         accessor.BuildFullyQualifiedName(name),
                         accessor.Body.ToMemberBody(accessor.ExpressionBody, filePath),
                         accessor,
                         filePath) with
                   {
                       Modifiers = accessor.Modifiers.ToModifiers(),
                       Accessibility = accessor.Modifiers.ToAccessibility(accessor),
                       Attributes = accessor.AttributeLists.ToAttributes(filePath),
                       IsAutoImplemented = accessor.Body is null && accessor.ExpressionBody is null
                   };
        }

        private static Accessor ToExpressionBodiedGetter(this ArrowExpressionClauseSyntax expressionBody, string filePath)
        {
            const string Name = "get";

            return Build(AccessorKind.Get,
                         Name,
                         expressionBody.BuildFullyQualifiedName(Name),
                         ((BlockSyntax?)null).ToMemberBody(expressionBody, filePath),
                         expressionBody,
                         filePath) with
                   {
                       Accessibility = Accessibility.NotApplicable
                   };
        }

        private static Accessor Build(AccessorKind kind, string name, string fullyQualifiedName, MemberBody body, SyntaxNode node, string filePath)
        {
            return new Accessor
            {
                Kind = kind,
                Name = name,
                FullyQualifiedName = fullyQualifiedName,
                Body = body.Text,
                Statements = body.Statements,
                LineStatements = body.LineStatements,
                LocalFunctions = body.LocalFunctions,
                Invocations = body.Invocations,
                LocalVariables = body.LocalVariables,
                ObjectCreations = body.ObjectCreations,
                StringLiterals = body.StringLiterals,
                MemberAccesses = body.MemberAccesses,
                BodyStatements = body.BodyStatements,
                AllStatements = body.AllStatements,
                SwitchExpressions = body.SwitchExpressions,
                Lambdas = body.Lambdas,
                Awaits = body.Awaits,
                ExpressionBody = body.ExpressionBody,
                IsExpressionBodied = body.IsExpressionBodied,
                IsIterator = body.IsIterator,
                SourceNode = node,
                FilePath = filePath,
                Location = node.ToCodeLocation(filePath)
            };
        }
    }
}
