using System.Collections.Immutable;
using System.Linq;

namespace Solution.Parser.CSharp
{
    /// <summary>
    /// What the compiler knows about the records of the syntax model. These members only answer for
    /// a solution parsed with <c>Parse(ParseMode.WithSymbols)</c> or code in memory opened with
    /// <c>CodeBase.FromSources(...).WithSymbols()</c>; anywhere else they throw rather than guess.
    /// </summary>
    /// <example>
    /// <code>
    /// from call in tree.AllInvocations().Named("AssertPostAsync")
    /// where call.Argument("writeResponse")?.Is(true) == true
    /// select $"{call.Location}: writeResponse is true";
    /// </code>
    /// </example>
    public static class InvocationSemantics
    {
        extension(Invocation call)
        {
            /// <summary>The method the call binds to, null unless the resolution is <see cref="ResolutionStatus.Resolved"/>.</summary>
            public Method? Method => InvocationResolver.Resolve(call, "call.Method").Method;

            public ResolutionStatus Resolution => InvocationResolver.Resolve(call, "call.Resolution").Status;

            public bool IsResolved => call.Resolution == ResolutionStatus.Resolved;

            /// <summary>The overloads the compiler considered but could not decide between, for an ambiguous call.</summary>
            public ImmutableList<Method> Candidates => InvocationResolver.Resolve(call, "call.Candidates").Candidates;

            /// <summary>Every parameter of the called method with what the call passes for it, in parameter order.</summary>
            public ImmutableList<BoundArgument> BoundArguments => InvocationResolver.Resolve(call, "call.BoundArguments").Arguments;

            /// <summary>The way out to Roslyn: node, semantic model, symbol and operation of the call.</summary>
            public RoslynInvocation? Roslyn => InvocationResolver.Resolve(call, "call.Roslyn").Roslyn;

            /// <summary>
            /// What the call passes for the named parameter, by position or by name alike; null when the
            /// call is not resolved or the method has no such parameter.
            /// </summary>
            public BoundArgument? Argument(string parameterName)
            {
                return InvocationResolver.Resolve(call, "call.Argument(name)").Arguments.FirstOrDefault(a => a.Parameter.Name == parameterName);
            }
        }
    }

    /// <summary>What the compiler knows about a <see cref="Method"/> that came out of a resolution.</summary>
    public static class MethodSemantics
    {
        extension(Method method)
        {
            /// <summary>Whether the method is declared in the code base, in a package or in the framework.</summary>
            public SymbolOrigin Origin => SymbolToModel.OriginOf(method);
        }
    }
}
