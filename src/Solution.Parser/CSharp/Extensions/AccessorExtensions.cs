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
                if (expressionBody is null)
                {
                    return PropertyAccessors.None;
                }

                var getter = new Accessor(AccessorKind.Get,
                                          Accessibility.NotApplicable,
                                          IsAutoImplemented: false,
                                          IsExpressionBodied: true,
                                          expressionBody.Expression.ToString(),
                                          expressionBody.ToCodeLocation(filePath));

                return new PropertyAccessors(getter, null, null);
            }

            return new PropertyAccessors(accessorList.Find(SyntaxKind.GetAccessorDeclaration, AccessorKind.Get, filePath),
                                         accessorList.Find(SyntaxKind.SetAccessorDeclaration, AccessorKind.Set, filePath),
                                         accessorList.Find(SyntaxKind.InitAccessorDeclaration, AccessorKind.Init, filePath));
        }

        /// <summary>True when every accessor is a bare semicolon, so the compiler supplies the backing field.</summary>
        internal static bool IsAutoImplemented(this AccessorListSyntax? accessorList)
        {
            return accessorList is not null &&
                   accessorList.Accessors.Count > 0 &&
                   accessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null);
        }

        private static Accessor? Find(this AccessorListSyntax accessorList, SyntaxKind kind, AccessorKind accessorKind, string filePath)
        {
            var accessor = accessorList.Accessors.FirstOrDefault(a => a.IsKind(kind));

            if (accessor is null)
            {
                return null;
            }

            var body = accessor.Body?.ToString() ?? accessor.ExpressionBody?.Expression.ToString() ?? string.Empty;

            return new Accessor(accessorKind,
                                accessor.Modifiers.ToAccessibility(accessor),
                                accessor.Body is null && accessor.ExpressionBody is null,
                                accessor.ExpressionBody is not null,
                                body,
                                accessor.ToCodeLocation(filePath));
        }
    }
}
