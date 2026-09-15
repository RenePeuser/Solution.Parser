using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.Test.CSharp.ParseHelper;

namespace Solution.Parser.Test.CSharp
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

            Assert.AreEqual(3, holder.Location.StartLine);
            Assert.AreEqual(5, holder.Methods.Single().Location.StartLine);
            Assert.AreEqual(FilePath, holder.Location.FilePath);
            Assert.AreEqual($"{FilePath}(5,5)", holder.Methods.Single().Location.ToString());
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

            Assert.AreEqual(7, holder.LineCount);
            Assert.AreEqual(4, holder.Methods.Single().LineCount);
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

            Assert.IsTrue(load.Documentation.IsDocumented);
            Assert.AreEqual("Loads the thing.", load.Documentation.Summary);
            Assert.AreEqual("The thing.", load.Documentation.Returns);
            Assert.AreEqual("Which thing.", load.Documentation.Parameters.Single(p => p.Name == "id").Description);
            Assert.IsFalse(holder.Methods.Single(m => m.Name == "Undocumented").Documentation.IsDocumented);
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

            Assert.AreEqual("Go", holder.Methods.Single().Name);
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

            Assert.IsTrue(store.IsGeneric);
            CollectionAssert.AreEqual(new[] { "class", "IDisposable", "new()" }, store.TypeParameters.Single().Constraints.ToArray());
            Assert.AreEqual(VarianceKind.In, contract.TypeParameters[0].Variance);
            Assert.AreEqual(VarianceKind.Out, contract.TypeParameters[1].Variance);

            var map = store.Methods.Single();

            Assert.AreEqual("TResult", map.TypeParameters.Single().Name);
            CollectionAssert.AreEqual(new[] { "struct" }, map.TypeParameters.Single().Constraints.ToArray());
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

            CollectionAssert.AreEqual(new[] { "1", "10" }, attribute.PositionalArguments.ToArray());
            Assert.AreEqual("ErrorMessage", attribute.NamedArguments.Single().Name);
            Assert.AreEqual("\"out of range\"", attribute.NamedArguments.Single().Value);
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

            Assert.IsTrue(holder.Methods.Single().HasAttribute("Obsolete"));
            Assert.IsTrue(holder.Methods.Single().HasAttribute("ObsoleteAttribute"));
            Assert.IsFalse(holder.Methods.Single().HasAttribute("Obsolet"));
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

            Assert.IsTrue(parsed.Usings.Single(u => u.Value == "System").IsGlobal);
            Assert.IsTrue(parsed.Usings.Single(u => u.Value == "System.Math").IsStatic);

            var alias = parsed.Usings.Single(u => u.IsAlias);

            Assert.AreEqual("Text", alias.Alias);
            Assert.AreEqual("System.Text.StringBuilder", alias.Value);
            Assert.IsFalse(parsed.Usings.Single(u => u.Value == "System.Linq").IsGlobal);
        }

        [TestMethod]
        public void Parse_Errors_Are_Reported_Instead_Of_Silently_Swallowed()
        {
            var broken = ParseCode("public class Holder { public void Go( }");
            var sound = ParseCode("public class Holder { }");

            Assert.IsTrue(broken.HasParseErrors);
            Assert.IsFalse(sound.HasParseErrors);
            Assert.IsTrue(broken.Diagnostics.Any(d => d.IsError));
        }

        /// <summary>
        /// The body used to be split on the platform line ending, so a file checked out with the other
        /// platform's endings produced no lines at all.
        /// </summary>
        [DataTestMethod]
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

            CollectionAssert.AreEqual(new[] { "        var x = 1;", "        var y = 2;" },
                                      go.LineStatements.ToArray());
            Assert.AreEqual(2, go.Statements.Count);
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

            CollectionAssert.AreEqual(new[] { "Outer", "Inner" }, go.LocalFunctions.Select(l => l.Name).ToArray());
        }

        [TestMethod]
        public void Top_Level_Statements_Are_Reported()
        {
            var parsed = ParseCode("""
                using System;

                Console.WriteLine("one");
                Console.WriteLine("two");
                """);

            Assert.AreEqual(2, parsed.Statements.Count);
            Assert.AreEqual("Console.WriteLine(\"one\");", parsed.Statements[0].SyntaxTree);
        }

        [TestMethod]
        public void Assembly_Attributes_Are_Reported()
        {
            var parsed = ParseCode("""
                using System.Runtime.CompilerServices;

                [assembly: InternalsVisibleTo("Other")]

                public class Holder { }
                """);

            Assert.AreEqual("assembly", parsed.AssemblyAttributes.Single().Target);
            Assert.IsTrue(parsed.AssemblyAttributes.HasAttribute("InternalsVisibleTo"));
        }
    }
}
