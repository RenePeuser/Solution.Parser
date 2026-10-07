using System;
using System.IO;
using System.Linq;
using System.Text;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Sln;
using static Solution.Parser.CSharp.Semantic.Test.SemanticHelper;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// A place to try things out and to debug: put a breakpoint on a <c>Dump</c> line, or read the
    /// output in the test explorer. Change the snippet or the constants and run again.
    /// </summary>
    [TestClass]
    public class PlaygroundTest
    {
        /// <summary>The project of this solution <see cref="Dump_The_Calls_Of_A_Project"/> looks at.</summary>
        private const string ProjectName = "Solution.Parser.Architecture.Test";

        /// <summary>The method name <see cref="Dump_The_Calls_Of_A_Project"/> looks for; empty for every call.</summary>
        private const string MethodName = "HasCount";

        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public void Dump_Every_Call_Of_A_Snippet()
        {
            var code = WithSymbols("""
                using System.Net.Http;
                using System.Threading.Tasks;

                public static class ClientExtensions
                {
                    public static Task AssertPostAsync(this HttpClient client, string url, string body, bool writeResponse = false) => Task.CompletedTask;
                }

                public class Tests
                {
                    private const bool Yes = true;

                    public async Task Post(HttpClient client)
                    {
                        await client.AssertPostAsync("url", "body", true);
                        await client.AssertPostAsync("url", "body");
                        await client.AssertPostAsync(writeResponse: Yes, body: "body", url: "url");
                        System.Console.WriteLine($"done {client.BaseAddress}");
                    }
                }
                """);

            var calls = code.AllTrees.AllInvocations();

            foreach (var call in calls)
            {
                Dump(call);
            }

            Assert.That.All(calls, c => c.IsResolved, "is resolved", because: "the snippet compiles", fix: "Fix the snippet, see the output");
        }

        [TestMethod]
        public void Dump_The_Calls_Of_A_Project()
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));
            var solution = solutionFile.Parse(ParseMode.WithSymbols);
            var project = solution.Projects.Single(p => p.ProjectFileInfo.FileNameWithoutExtenion == ProjectName);

            foreach (var diagnostic in project.CompilationDiagnostics().Where(d => d.IsError))
            {
                TestContext.WriteLine($"ERROR {diagnostic.Location}: {diagnostic.Id} {diagnostic.Message}");
            }

            var calls = project.Trees
                            .AllInvocations()
                            .Where(c => MethodName.Length == 0 || c.Name == MethodName)
                            .Take(20)
                            .ToList();

            foreach (var call in calls)
            {
                Dump(call);
            }

            Assert.That.IsTrue(calls.Count > 0, because: "the project should call the method", fix: "Change ProjectName or MethodName");
        }

        private void Dump(Invocation call)
        {
            var text = new StringBuilder();

            text.AppendLine($"{call.Location}: {call.SyntaxTree}");
            text.AppendLine($"  Resolution : {call.Resolution}");

            if (call.Method is { } method)
            {
                text.AppendLine($"  Method     : {method.FullQualifiedName}({string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))})");
                text.AppendLine($"  Origin     : {method.Origin.Kind} {method.Origin.Name} {method.Origin.Version}");
            }

            foreach (var candidate in call.Candidates)
            {
                text.AppendLine($"  Candidate  : {candidate.SyntaxTree}");
            }

            foreach (var argument in call.BoundArguments)
            {
                var how = argument switch
                {
                    { IsReceiver: true } => "receiver",
                    { IsExplicit: false } => "default",
                    { IsNamed: true } => "named",
                    _ => "positional"
                };

                var constant = argument.HasConstantValue ? $" = {SymbolToModel.FormatConstant(argument.ConstantValue)}" : string.Empty;

                text.AppendLine($"  {argument.Parameter.Name,-14} <- {argument.Expression ?? "(left out)"} [{how}]{constant}");
            }

            TestContext.WriteLine(text.ToString());
        }
    }
}
