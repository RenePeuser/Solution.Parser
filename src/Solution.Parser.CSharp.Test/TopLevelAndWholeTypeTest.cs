using System.Collections.Immutable;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// Two places where code sits outside a single type of a single file: the top level statements of
    /// Program.cs, and a partial type written in several files. A rule asks both the same questions it
    /// asks anywhere else.
    /// </summary>
    [TestClass]
    public class TopLevelAndWholeTypeTest
    {
        [TestMethod]
        public void A_Top_Level_Statement_Answers_Like_One_In_A_Method()
        {
            var tree = ParseCode("""
                var client = new HttpClient();

                foreach (var url in urls)
                {
                    await client.GetAsync(url);
                    urls.ForEach(u => { if (u.Length == 0) { return; } });
                }

                Console.WriteLine($"done at {DateTime.Now}");
                """);

            var loop = tree.Statements.OfType<LoopStatement>().Single();

            Assert.That.IsTrue(loop.IsForEach && loop.Awaits.Count == 1 && loop.IfStatements.Count == 1 &&
                               loop.Lambdas.Count == 1 && loop.LocalVariables.Single().Name == "url",
                               because: "a loop in Program.cs holds its awaits, its lambdas and what they contain, as in a method",
                               fix: "Build the top level statements with ToTopLevelBody, which completes the body index");

            Assert.That.IsTrue(tree.Statements[0].ObjectCreations.Single().TypeName == "HttpClient" &&
                               tree.Statements[^1].MemberAccesses.Select(a => a.Name).SequenceEqual(["WriteLine", "Now"]) &&
                               tree.Statements[^1].StringLiterals.Single().IsInterpolated,
                               because: "every top level statement cuts its contents out of the file's top level body",
                               fix: "Check StatementSyntaxExtensions.ToTopLevelStatements");
        }

        [TestMethod]
        public void A_Local_Function_At_Top_Level_Keeps_Its_Code_With_The_Top_Level()
        {
            var tree = ParseCode("""
                Run();

                void Run()
                {
                    Log("run");
                    int Inner() => Count();
                }
                """);

            var function = tree.Statements[1];

            Assert.That.AreEqual<string>(["Log"],
                                         function.Invocations.Select(i => i.Name),
                                         because: "a local function at top level has no member of its own, so its calls stay with it; one nested in it reports its own",
                                         fix: "Descend into a top level local function in BodyScan.OfTopLevel, not into the ones nested in it");
        }

        [TestMethod]
        public void The_Parts_Of_A_Partial_Type_Are_One_Type()
        {
            ImmutableList<CSharpSyntaxTree> trees =
            [
                ParseCode("""
                    namespace Shop;
                    public partial class Order : IEntity { public void Save() { } }
                    public class Invoice { public void Print() { } }
                    """),
                ParseCode("""
                    namespace Shop;
                    public partial class Order : IValidatable { public bool IsValid { get; } public void Validate() { } }
                    public partial class Order<T> { public void Map() { } }
                    """),
                ParseCode("""
                    namespace Billing;
                    public partial class Order { public void Charge() { } }
                    """)
            ];

            Assert.That.AreEqual<string>(["Shop.Order", "Shop.Invoice", "Shop.Order", "Billing.Order"],
                                         trees.WholeTypes.Select(t => t.FullyQualifiedName),
                                         because: "the two parts of Shop.Order are one type; Order<T> and Billing.Order are other types of the same name",
                                         fix: "Join only partial types of the same kind, name and number of type parameters in WholeType.Join");

            var order = trees.WholeTypes[0];

            Assert.That.IsTrue(order.IsPartial && order.Parts.Count == 2 &&
                               order.Methods.Select(m => m.Name).SequenceEqual(["Save", "Validate"]) &&
                               order.Properties.Single().Name == "IsValid" &&
                               order.BaseTypes.Select(b => b.TypeName).SequenceEqual(["IEntity", "IValidatable"]),
                               because: "a rule about the whole type sees the members and base types of every part",
                               fix: "Check the member lists of WholeType");

            Assert.That.IsTrue(order.Location == trees[0].Classes[0].Location && trees.Classes.Count(c => c.FullyQualifiedName == "Shop.Order" && !c.IsGeneric) == 2,
                               because: "the whole type points at its first part, while Classes still lists each part where it is written",
                               fix: "Keep CSharpSyntaxTree.Classes per part and WholeType.Location on the first part");

            var invoice = trees.WholeTypes[1];

            Assert.That.IsTrue(!invoice.IsPartial && invoice.Parts.Count == 1 && invoice.Methods.Single().Name == "Print",
                               because: "a type that is not partial is a whole type of one part",
                               fix: "Give every type that is not partial a WholeType of its own");
        }

        [TestMethod]
        public void A_Rule_Counts_The_Methods_Of_The_Whole_Type()
        {
            ImmutableList<CSharpSyntaxTree> trees =
            [
                ParseCode("public partial class Big { public void A() { } public void B() { } }"),
                ParseCode("public partial class Big { public void C() { } }"),
                ParseCode("public class Small { public void A() { } public void B() { } }")
            ];

            var findings = from type in trees.WholeTypes
                           where type.Methods.Count > 2
                           select $"{type.Name} has {type.Methods.Count} methods in {type.Parts.Count} files";

            Assert.That.AreEqual<string>(["Big has 3 methods in 2 files"],
                                         findings,
                                         because: "neither part of Big has more than two methods, the class does",
                                         fix: "Check trees.WholeTypes");
        }

        [TestMethod]
        public void A_Modifier_On_One_Part_Applies_To_The_Whole_Type()
        {
            ImmutableList<CSharpSyntaxTree> trees =
            [
                ParseCode("public static partial class Helpers { }"),
                ParseCode("partial class Helpers { }"),
                ParseCode("sealed partial class Order { }"),
                ParseCode("abstract partial class Order { }")
            ];

            var helpers = trees.WholeTypes.Single(t => t.Name == "Helpers");
            var order = trees.WholeTypes.Single(t => t.Name == "Order");

            Assert.That.IsTrue(helpers.IsPublic && helpers.IsStatic,
                               because: "public and static on one part make the whole type public and static",
                               fix: "Check WholeType.IsPublic and IsStatic read every part");

            Assert.That.IsTrue(!order.IsPublic && order.IsSealed && order.IsAbstract && !helpers.IsSealed,
                               because: "each modifier counts when any part writes it, and only then",
                               fix: "Check WholeType.IsSealed and IsAbstract read every part");
        }
    }
}
