using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Connects the plain records with the compilation they came from. The records carry their file and
    /// span but no reference to Roslyn, which keeps them comparable and cheap; this table is how
    /// <c>call.Method</c> still finds the compilation that knows the answer.
    /// </summary>
    /// <remarks>
    /// A call is known by its instance, not by its file: two code bases over the same files never answer
    /// for each other, and a call of a code base without symbols stays unknown however many others have
    /// them. A copy made with <c>with</c> is a new instance and therefore unknown too. The table holds
    /// nothing alive; an entry goes with its call.
    /// </remarks>
    internal static class SemanticRegistry
    {
        private static readonly ConditionalWeakTable<Invocation, Owner> Owners = new();

        /// <summary>Makes the calls of the tree known as calls of the project in the workspace.</summary>
        internal static void Register(CSharpSyntaxTree tree, Workspace workspace, string projectKey)
        {
            var owner = new Owner(workspace, projectKey);

            foreach (var call in tree.AllInvocations())
            {
                Owners.AddOrUpdate(call, owner);
            }
        }

        internal static bool TryGet(Invocation call, [NotNullWhen(true)] out Workspace? workspace, [NotNullWhen(true)] out string? projectKey)
        {
            if (!Owners.TryGetValue(call, out var owner))
            {
                workspace = null;
                projectKey = null;

                return false;
            }

            workspace = owner.Workspace;
            projectKey = owner.ProjectKey;

            return true;
        }

        private sealed record Owner(Workspace Workspace, string ProjectKey);
    }
}
