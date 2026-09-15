using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class EnrichmentTest
    {
        /// <summary>
        /// Without a location a failing rule can only name the type, leaving the reader to search for it.
        /// </summary>
        [TestMethod]
        public void Every_Declaration_Knows_Where_It_Is()
        {
            var holder = ParseCode("""
                namespace My.Sample;

                public class Holder
                {
                    public void Go() { }
                }
                """).Classes.Single();

            Assert.That.AreEqual(3,
                                 holder.Location.StartLine,
                                 because: "lines are one based so they match what an editor shows",
                                 fix: "Add one to the zero based line of the Roslyn FileLinePositionSpan");

            Assert.That.AreEqual(5,
                                 holder.Methods.Single().Location.StartLine,
                                 because: "a rule reporting a member must point at the member, not at the file",
                                 fix: "Take the location from the member node rather than from the declaring type");

            Assert.That.AreEqual(FilePath,
                                 holder.Location.FilePath,
                                 because: "the path is half of a finding a reader can click",
                                 fix: "Pass the parsed file path through into every CodeLocation");

            Assert.That.AreEqual($"{FilePath}(5,5)",
                                 holder.Methods.Single().Location.ToString(),
                                 because: "test runners and IDEs turn the path(line,col) form into a clickable link",
                                 fix: "Check CodeLocation.ToString renders path(line,column)");
        }

        [TestMethod]
        public void Line_Count_Spans_The_Declaration()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    public void Go()
                    {
                        var x = 1;
                    }
                }
                """).Classes.Single();

            Assert.That.AreEqual(7,
                                 holder.LineCount,
                                 because: "a rule limiting the size of a type needs the number of lines it spans",
                                 fix: "Derive LineCount from the end line minus the start line plus one");

            Assert.That.AreEqual(4,
                                 holder.Methods.Single().LineCount,
                                 because: "a rule limiting the length of a method needs the same count per member",
                                 fix: "Derive LineCount from the end line minus the start line plus one");
        }

        [TestMethod]
        public void Xml_Documentation_Is_Read()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    /// <summary>
                    /// Loads the thing.
                    /// </summary>
                    /// <param name="id">Which thing.</param>
                    /// <returns>The thing.</returns>
                    public string Load(int id) => "x";

                    public void Undocumented() { }
                }
                """).Classes.Single();

            var load = holder.Methods.Single(m => m.Name == "Load");

            Assert.That.IsTrue(load.Documentation.IsDocumented,
                               because: "a rule requiring documentation on the public surface starts from this flag",
                               fix: "Look for a DocumentationCommentTriviaSyntax in the leading trivia of the declaration");

            Assert.That.AreEqual("Loads the thing.",
                                 load.Documentation.Summary,
                                 because: "the summary is what a rule about meaningful documentation inspects",
                                 fix: "Parse the summary element and collapse its lines into one");

            Assert.That.AreEqual("The thing.",
                                 load.Documentation.Returns,
                                 because: "a rule can require a returns tag on every non void member",
                                 fix: "Parse the returns element of the documentation comment");

            Assert.That.AreEqual("Which thing.",
                                 load.Documentation.Parameters.Single(p => p.Name == "id").Description,
                                 because: "a rule can require a param tag for every parameter",
                                 fix: "Parse each param element and key it by its name attribute");

            Assert.That.IsFalse(holder.Methods.Single(m => m.Name == "Undocumented").Documentation.IsDocumented,
                                because: "an undocumented member must not appear documented, or the rule would never fire",
                                fix: "Return DocumentationComment.None when there is no documentation trivia");
        }

        [TestMethod]
        public void Malformed_Documentation_Does_Not_Break_The_Parse()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    /// <summary>Unclosed
                    public void Go() { }
                }
                """).Classes.Single();

            Assert.That.AreEqual("Go",
                                 holder.Methods.Single().Name,
                                 because: "a broken comment in one file must not take down the parse of the whole solution",
                                 fix: "Catch XmlException while parsing documentation and fall back to the raw text");
        }

        [TestMethod]
        public void Generics_Report_Their_Parameters_And_Constraints()
        {
            var parsed = ParseCode("""
                using System;

                public interface IStore<in TKey, out TValue> { }

                public class Store<T> where T : class, IDisposable, new()
                {
                    public TResult Map<TResult>(T source) where TResult : struct => default;
                }
                """);

            var store = parsed.Classes.Single();
            var contract = parsed.Interfaces.Single();

            Assert.That.IsTrue(store.IsGeneric,
                               because: "a rule that treats generic types differently needs to recognise them",
                               fix: "Derive IsGeneric from a non empty TypeParameters list");

            Assert.That.AreEqual<string>(["class", "IDisposable", "new()"],
                                         store.TypeParameters.Single().Constraints,
                                         because: "constraints are part of the contract a rule may want to enforce",
                                         fix: "Match the constraint clauses to the type parameter by name");

            Assert.That.AreEqual(VarianceKind.In,
                                 contract.TypeParameters[0].Variance,
                                 because: "variance changes what a generic interface may be substituted with",
                                 fix: "Map the variance keyword of the type parameter");

            Assert.That.AreEqual(VarianceKind.Out,
                                 contract.TypeParameters[1].Variance,
                                 because: "variance changes what a generic interface may be substituted with",
                                 fix: "Map the variance keyword of the type parameter");

            var map = store.Methods.Single();

            Assert.That.AreEqual("TResult",
                                 map.TypeParameters.Single().Name,
                                 because: "a generic method has type parameters of its own, separate from those of the type",
                                 fix: "Read the type parameter list of the method declaration");

            Assert.That.AreEqual<string>(["struct"],
                                         map.TypeParameters.Single().Constraints,
                                         because: "the constraint belongs to the method type parameter, not to the one of the type",
                                         fix: "Match the constraint clauses of the method to its own type parameters");
        }

        [TestMethod]
        public void Attribute_Arguments_Are_Split_By_Kind()
        {
            var holder = ParseCode("""
                using System.ComponentModel.DataAnnotations;

                public class Holder
                {
                    [Range(1, 10, ErrorMessage = "out of range")]
                    public int Value { get; set; }
                }
                """).Classes.Single();

            var attribute = holder.Properties.Single().Attributes.Single();

            Assert.That.AreEqual<string>(["1", "10"],
                                         attribute.PositionalArguments,
                                         because: "a rule inspecting an attribute argument by position must not trip over the named one",
                                         fix: "Split the argument list on NameEquals and NameColon");

            Assert.That.AreEqual("ErrorMessage",
                                 attribute.NamedArguments.Single().Name,
                                 because: "a named argument is addressed by its name, not by its position",
                                 fix: "Read the name from the NameEquals of the attribute argument");

            Assert.That.AreEqual("\"out of range\"",
                                 attribute.NamedArguments.Single().Value,
                                 because: "the value as written is what a rule about message texts inspects",
                                 fix: "Read the value from the expression of the attribute argument");
        }

        [TestMethod]
        public void Attribute_Matching_Ignores_The_Suffix_And_The_Namespace()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    [System.ObsoleteAttribute("gone")]
                    public void Go() { }
                }
                """).Classes.Single();

            Assert.That.IsTrue(holder.Methods.Single().HasAttribute("Obsolete"),
                               because: "the same attribute can be written four ways and a rule should not have to spell out each one",
                               fix: "Compare through Attribute.IsNamed, which strips the namespace and the Attribute suffix");

            Assert.That.IsTrue(holder.Methods.Single().HasAttribute("ObsoleteAttribute"),
                               because: "the full name must match just as well as the short one",
                               fix: "Compare through Attribute.IsNamed, which strips the namespace and the Attribute suffix");

            Assert.That.IsFalse(holder.Methods.Single().HasAttribute("Obsolet"),
                                because: "the match must stay exact after normalising, or unrelated attributes would match",
                                fix: "Compare the normalised names with an ordinal equality check, not with a contains check");
        }

        [TestMethod]
        public void Usings_Report_How_They_Were_Written()
        {
            var parsed = ParseCode("""
                global using System;
                using static System.Math;
                using Text = System.Text.StringBuilder;
                using System.Linq;
                """);

            Assert.That.IsTrue(parsed.Usings.Single(u => u.Value == "System").IsGlobal,
                               because: "a rule about global usings needs to tell them from ordinary ones",
                               fix: "Read the global keyword of the using directive");

            Assert.That.IsTrue(parsed.Usings.Single(u => u.Value == "System.Math").IsStatic,
                               because: "a using static imports members rather than a namespace",
                               fix: "Read the static keyword of the using directive");

            var alias = parsed.Usings.Single(u => u.IsAlias);

            Assert.That.AreEqual("Text",
                                 alias.Alias,
                                 because: "the alias used to be dropped entirely, leaving the directive indistinguishable from a plain import",
                                 fix: "Read the alias from the using directive");

            Assert.That.AreEqual("System.Text.StringBuilder",
                                 alias.Value,
                                 because: "an alias may point at a type, which Name does not cover but NamespaceOrType does",
                                 fix: "Read the target from UsingDirectiveSyntax.NamespaceOrType");

            Assert.That.IsFalse(parsed.Usings.Single(u => u.Value == "System.Linq").IsGlobal,
                                because: "a plain using must not be reported as global",
                                fix: "Read the global keyword of the using directive");
        }

        [TestMethod]
        public void Parse_Errors_Are_Reported_Instead_Of_Silently_Swallowed()
        {
            var broken = ParseCode("public class Holder { public void Go( }");
            var sound = ParseCode("public class Holder { }");

            Assert.That.IsTrue(broken.HasParseErrors,
                               because: "malformed source produces an incomplete model, and a rule should be able to say so instead of trusting it",
                               fix: "Project syntaxTree.GetDiagnostics() onto CSharpSyntaxTree.Diagnostics");

            Assert.That.IsFalse(sound.HasParseErrors,
                                because: "sound source must not be flagged, or the rule would fire on every file",
                                fix: "Only treat a diagnostic with error severity as a parse error");

            Assert.That.Any(broken.Diagnostics,
                            d => d.IsError,
                            "is an error diagnostic",
                            because: "the diagnostics themselves are what tells the reader what is wrong with the file",
                            fix: "Project syntaxTree.GetDiagnostics() onto CSharpSyntaxTree.Diagnostics");
        }

        /// <summary>
        /// The body used to be split on the platform line ending, so a file checked out with the other
        /// platform's endings produced no lines at all.
        /// </summary>
        [TestMethod]
        [DataRow("\r\n")]
        [DataRow("\n")]
        public void Line_Statements_Do_Not_Depend_On_The_Line_Ending(string lineEnding)
        {
            var code = string.Join(lineEnding,
                                   "public class Holder",
                                   "{",
                                   "    public void Go()",
                                   "    {",
                                   "        var x = 1;",
                                   "",
                                   "        var y = 2;",
                                   "    }",
                                   "}");

            var go = ParseCode(code).Classes.Single().Methods.Single();

            Assert.That.AreEqual<string>(["        var x = 1;", "        var y = 2;"],
                                         go.LineStatements,
                                         because: "a repository holding the other platform's line endings must parse the same way",
                                         fix: "Split on any line ending instead of on Environment.NewLine, see StringLineExtensions.SplitLines");

            Assert.That.HasCount(2,
                                 go.Statements,
                                 because: "the body holds two statements regardless of how its lines are separated",
                                 fix: "Read the statements from the block rather than from the text of the body");
        }

        [TestMethod]
        public void Local_Functions_Are_Found_At_Any_Depth()
        {
            var go = ParseCode("""
                public class Holder
                {
                    public void Go()
                    {
                        void Outer()
                        {
                            void Inner() { }
                        }
                    }
                }
                """).Classes.Single().Methods.Single();

            Assert.That.AreEqual<string>(["Outer", "Inner"],
                                         go.LocalFunctions.Select(l => l.Name),
                                         because: "a rule over local functions must reach the ones nested inside another",
                                         fix: "Collect local functions from the descendants of the body, and report each of them once");
        }

        [TestMethod]
        public void Top_Level_Statements_Are_Reported()
        {
            var parsed = ParseCode("""
                using System;

                Console.WriteLine("one");
                Console.WriteLine("two");
                """);

            Assert.That.HasCount(2,
                                 parsed.Statements,
                                 because: "Statements means the top level statements of the file, not every statement in it",
                                 fix: "Read the GlobalStatementSyntax members of the compilation unit");

            Assert.That.AreEqual("Console.WriteLine(\"one\");",
                                 parsed.Statements[0].SyntaxTree,
                                 because: "the statement as written is what a rule about an entry point inspects",
                                 fix: "Report the statement of the global statement, not the global statement itself");
        }

        [TestMethod]
        public void Assembly_Attributes_Are_Reported()
        {
            var parsed = ParseCode("""
                using System.Runtime.CompilerServices;

                [assembly: InternalsVisibleTo("Other")]

                public class Holder { }
                """);

            Assert.That.AreEqual("assembly",
                                 parsed.AssemblyAttributes.Single().Target,
                                 because: "the target is what separates an assembly attribute from one on a declaration",
                                 fix: "Read the target from the attribute list that holds the attribute");

            Assert.That.IsTrue(parsed.AssemblyAttributes.HasAttribute("InternalsVisibleTo"),
                               because: "a rule guarding what a test project may reach starts from this attribute",
                               fix: "Collect the attribute lists of the compilation unit into AssemblyAttributes");
        }
    }
}
