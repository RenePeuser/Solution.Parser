using System.Collections.Immutable;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// A rule reads the code the way a developer opens it: the file, its classes, their methods, the
    /// loops in them. One file or the whole solution, the lists have the same names and hold
    /// everything, nested types included, so nobody has to know about AllTypes() or OfType&lt;T&gt;().
    /// </summary>
    [TestClass]
    public class ReadLikeCodeTest
    {
        private const string Shop = """
            namespace Shop;

            public class Order
            {
                private readonly int _count;

                public Order(int count) => _count = count;

                public string Id { get; init; } = "";

                public void Ship()
                {
                    foreach (var line in Lines())
                    {
                        while (line.Open) { line.Close(); }
                    }
                }

                public class Line
                {
                    public bool Open { get; set; }

                    public void Close() { var closed = true; }
                }

                public record Shipped(string Id);

                public enum State { Open, Done }
            }

            public interface IOrderStore { void Save(Order order); }

            public record Customer(string Name);

            public record struct Money(decimal Amount);

            public struct Weight { }

            public static class OrderExtensions
            {
                extension(Order order)
                {
                    public bool IsBig => true;

                    public void Archive() { }
                }
            }
            """;

        private static readonly CSharpSyntaxTree Tree = ParseCode(Shop);

        [TestMethod]
        public void A_File_Lists_Every_Class_Nested_Ones_Included()
        {
            Assert.That.AreEqual<string>(["Order", "Line", "OrderExtensions"],
                                         Tree.Classes.Select(c => c.Name),
                                         because: "Line is a class of the file, even though it is written inside Order",
                                         fix: "Build CSharpSyntaxTree.Classes from every type at any depth");

            Assert.That.AreEqual<string>(["Shipped", "Customer", "Money"],
                                         Tree.Records.Select(r => r.Name),
                                         because: "a record, nested or a record struct, is a record and no class or struct",
                                         fix: "Build CSharpSyntaxTree.Records from every type at any depth");

            Assert.That.IsTrue(Tree.Structs.Single().Name == "Weight" && Tree.Enums.Single().Name == "State" && Tree.Interfaces.Single().Name == "IOrderStore",
                               because: "every kind of type has its list, nested ones included",
                               fix: "Check Structs, Enums and Interfaces on CSharpSyntaxTree");

            Assert.That.AreEqual<string>(["Order", "IOrderStore", "Customer", "Money", "Weight", "OrderExtensions"],
                                         Tree.Types.Select(t => t.Name),
                                         because: "Types stays the structure as written, the types at file level holding their nested ones",
                                         fix: "Leave CSharpSyntaxTree.Types at file level");
        }

        [TestMethod]
        public void A_File_Lists_Every_Member()
        {
            Assert.That.AreEqual<string>(["Ship", "Close", "Save", "Archive"],
                                         Tree.Methods.Select(m => m.Name),
                                         because: "the methods of nested types, interfaces and extension blocks are methods of the file",
                                         fix: "Build CSharpSyntaxTree.Methods from every type at any depth and its extension blocks");

            Assert.That.AreEqual<string>(["Id", "Open", "Id", "Name", "Amount", "IsBig"],
                                         Tree.Properties.Select(p => p.Name),
                                         because: "positional record properties and extension properties count as well",
                                         fix: "Build CSharpSyntaxTree.Properties from every type at any depth and its extension blocks");

            Assert.That.IsTrue(Tree.Fields.Single().Name == "_count" && Tree.Constructors.Any(c => c.Name == "Order"),
                               because: "fields and constructors have their lists too",
                               fix: "Check Fields and Constructors on CSharpSyntaxTree");

            Assert.That.IsTrue(Tree.Invocations.Any(i => i.Name == "Close") &&
                               Tree.LocalVariables.Select(v => v.Name).SequenceEqual(["line", "closed"]) &&
                               Tree.MembersWithBody.Any(m => m is Constructor),
                               because: "what happens inside the bodies is reachable from the file",
                               fix: "Check MembersWithBody, Invocations and LocalVariables on CSharpSyntaxTree");
        }

        [TestMethod]
        public void A_Rule_Reads_File_Class_Method_Loop()
        {
            var findings = from @class in Tree.Classes
                           from method in @class.Methods
                           from loop in method.Loops
                           from inner in loop.Loops
                           select $"{@class.Name}.{method.Name}: {inner.Kind} in {loop.Kind}";

            Assert.That.AreEqual<string>(["Order.Ship: While in ForEach"],
                                         findings,
                                         because: "this is how a developer reads the code, and how the rule should read",
                                         fix: "Keep Classes, Methods, Loops and Statement.Loops in step");
        }

        [TestMethod]
        public void Many_Files_Read_Like_One()
        {
            ImmutableList<CSharpSyntaxTree> trees =
            [
                ParseCode("public class A { public void Go() { } public class Nested { public void Deep() { } } }"),
                ParseCode("public record B(int Id) { public void Run() { Go(); } }")
            ];

            Assert.That.AreEqual<string>(["A", "Nested"],
                                         trees.Classes.Select(c => c.Name),
                                         because: "trees.Classes is every class of every file",
                                         fix: "Check SyntaxTreeListExtensions.Classes");

            Assert.That.AreEqual<string>(["Go", "Deep", "Run"],
                                         trees.Methods.Select(m => m.Name),
                                         because: "trees.Methods is every method of every file",
                                         fix: "Check SyntaxTreeListExtensions.Methods");

            Assert.That.IsTrue(trees.Records.Single().Name == "B" && trees.Properties.Single().Name == "Id" &&
                               trees.Invocations.Single().Name == "Go" && trees.MembersWithBody.OfType<Method>().Count() == 3,
                               because: "every list of a file exists for many files as well",
                               fix: "Check SyntaxTreeListExtensions");

            var array = trees.ToArray();

            Assert.That.AreEqual<string>(["Go", "Deep", "Run"],
                                         array.Methods.Select(m => m.Name),
                                         because: "any sequence of files works, not only an immutable list",
                                         fix: "Compute the lists for a sequence that is no ImmutableList");
        }

        [TestMethod]
        public void The_Lists_Are_Built_Once()
        {
            ImmutableList<CSharpSyntaxTree> trees = [ParseCode(Shop)];

            Assert.That.IsTrue(ReferenceEquals(Tree.Classes, Tree.Classes) &&
                               ReferenceEquals(Tree.Methods, Tree.Methods) &&
                               ReferenceEquals(Tree.AllTypes(), Tree.AllTypes()),
                               because: "a file is parsed once, so its lists are built once, whichever way a rule asks",
                               fix: "Cache the views of CSharpSyntaxTree with Cache.For");

            Assert.That.IsTrue(ReferenceEquals(trees.Methods, trees.Methods) &&
                               ReferenceEquals(trees.MembersWithBody, trees.MembersWithBody),
                               because: "a rule suite keeps its list of files and asks it again and again",
                               fix: "Remember the views of an ImmutableList in SyntaxTreeListExtensions");
        }

        [TestMethod]
        public void A_Copy_Made_With_With_Computes_Its_Own_Lists()
        {
            _ = Tree.Classes;

            var copy = Tree with { Types = ImmutableList<TypeDeclaration>.Empty };

            Assert.That.IsTrue(copy.Classes.IsEmpty && copy.Methods.IsEmpty && !Tree.Classes.IsEmpty,
                               because: "with copies the cache along with the fields, so the cache must know whom it belongs to",
                               fix: "Check the owner in Cache.For");
        }
    }
}
