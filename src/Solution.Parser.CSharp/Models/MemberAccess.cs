using System.Diagnostics;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// A <c>Target.Name</c> in a body, as in <c>DateTime.Now</c>, <c>_logger.LogInformation</c> or
    /// <c>user?.Name</c>. A chain <c>a.b.c</c> reports <c>a.b</c> and <c>a.b.c</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// from access in method.MemberAccesses
    /// where access.Target == "DateTime" &amp;&amp; access.Name is "Now" or "UtcNow"
    /// select $"{access.Location}: inject TimeProvider instead";
    /// </code>
    /// </example>
    [DebuggerDisplay("{SourceCode}")]
    public sealed record MemberAccess : CodeElement
    {
        /// <summary>What the member is accessed on, as written: <c>DateTime</c>, <c>this._logger</c>, <c>GetUser()</c>.</summary>
        public required string Target { get; init; }

        /// <summary>The accessed member, without type arguments.</summary>
        public required string Name { get; init; }

        /// <summary>True when the member is called right away, as <c>NewGuid</c> in <c>Guid.NewGuid()</c>.</summary>
        public bool IsInvoked { get; init; }

        /// <summary>True for an access through <c>?.</c>.</summary>
        public bool IsConditional { get; init; }
    }
}
