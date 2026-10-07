using System;
using System.Runtime.CompilerServices;

namespace Solution.Parser.CSharp.Semantic.Test
{
    internal static class SemanticHelper
    {
        /// <summary>
        /// The code as one in-memory project with symbols. Every call gets its own file name, which
        /// keeps the failure output of parallel tests apart.
        /// </summary>
        internal static CodeBase WithSymbols(string code, [CallerMemberName] string test = "")
        {
            return CodeBase.FromSources(($"{test}_{Guid.NewGuid():N}.cs", code)).WithSymbols();
        }

        /// <summary>The same, without symbols: what the fast mode sees.</summary>
        internal static CodeBase SyntaxOnly(string code, [CallerMemberName] string test = "")
        {
            return CodeBase.FromSources(($"{test}_{Guid.NewGuid():N}.cs", code));
        }
    }
}
