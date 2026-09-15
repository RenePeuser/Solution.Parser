using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A diagnostic Roslyn reported while parsing the file. Syntax errors do not stop the parse, so a
    /// rule asserting that no file has any lets malformed source surface instead of quietly producing
    /// an incomplete model.
    /// </summary>
    [DebuggerDisplay("{Severity} {Id}")]
    public record ParseDiagnostic(string Id, DiagnosticSeverity Severity, string Message, CodeLocation Location)
    {
        public bool IsError => Severity == DiagnosticSeverity.Error;
    }
}
