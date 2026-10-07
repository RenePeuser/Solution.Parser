using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// Connects the plain records with the compilation they came from. The records carry their file and
    /// span but no reference to Roslyn, which keeps them comparable and cheap; this table is how
    /// <c>call.Method</c> still finds the compilation that knows the answer.
    /// </summary>
    /// <remarks>
    /// The last <see cref="CodeBase.WithSymbols"/> for a file wins. The workspace is held weakly, so a
    /// code base nobody uses any more can still be collected.
    /// </remarks>
    internal static class SemanticRegistry
    {
        private static readonly ConcurrentDictionary<string, (WeakReference<Workspace> Workspace, string ProjectKey)> Files = new(Workspace.PathComparer);

        internal static void Register(Workspace workspace)
        {
            var reference = new WeakReference<Workspace>(workspace);
            var seen = new ConcurrentDictionary<string, bool>(Workspace.PathComparer);

            // A file linked into two projects belongs to the first one that lists it.
            foreach (var project in workspace.Projects)
            {
                foreach (var file in project.SourceFiles)
                {
                    if (seen.TryAdd(file, true))
                    {
                        Files[file] = (reference, project.Key);
                    }
                }
            }
        }

        internal static bool TryGet(string filePath, [NotNullWhen(true)] out Workspace? workspace, [NotNullWhen(true)] out string? projectKey)
        {
            workspace = null;
            projectKey = null;

            if (!Files.TryGetValue(filePath, out var entry) || !entry.Workspace.TryGetTarget(out workspace))
            {
                return false;
            }

            projectKey = entry.ProjectKey;

            return true;
        }
    }
}
