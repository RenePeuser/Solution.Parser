using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// A member belongs to the type that declares it. Before nesting was handled, every member list was
    /// built from a recursive descendant walk, so a type reported the members of its nested types as
    /// its own and a nested type showed up again at file level.
    /// </summary>
    [TestClass]
    public class ContainmentTest
    {
        private const string Code = """
            namespace My.Sample;

            public class Outer
            {
                public void OuterMethod() { }

                public class Inner
                {
                    public void InnerMethod() { }

                    public class Deepest
                    {
                        public void DeepMethod() { }
                    }
                }
            }
            """;

        [TestMethod]
        public void File_Reports_Only_Top_Level_Types()
        {
            var parsed = ParseCode(Code);

            Assert.That.AreEqual<string>(["Outer"],
                                         parsed.Types.Select(t => t.Name),
                                         because: "Inner and Deepest are declared inside Outer, so Outer is the only type at file level",
                                         fix: "Build the file level list by descending through namespaces only, never into a type");
        }

        [TestMethod]
        public void Type_Reports_Only_Its_Own_Methods()
        {
            var outer = ParseCode(Code).Classes.Single();

            Assert.That.AreEqual<string>(["OuterMethod"],
                                         outer.Methods.Select(m => m.Name),
                                         because: "InnerMethod and DeepMethod belong to the nested types that declare them",
                                         fix: "Collect members from TypeDeclarationSyntax.Members instead of from DescendantNodes()");
        }

        [TestMethod]
        public void Type_Reports_Only_Its_Direct_Nested_Types()
        {
            var outer = ParseCode(Code).Classes.Single();

            Assert.That.AreEqual<string>(["Inner"],
                                         outer.NestedTypes.Select(t => t.Name),
                                         because: "Deepest is a grandchild of Outer and belongs to Inner",
                                         fix: "Take nested types from the direct members and let the recursion happen one level at a time");

            Assert.That.AreEqual<string>(["Deepest"],
                                         outer.NestedTypes.Single().NestedTypes.Select(t => t.Name),
                                         because: "Deepest is declared directly inside Inner",
                                         fix: "Check that a nested type is converted with the same walk as a top level one");
        }

        [TestMethod]
        public void Descendant_Types_Walks_Every_Level()
        {
            var outer = ParseCode(Code).Classes.Single();

            Assert.That.AreEqual<string>(["Inner", "Deepest"],
                                         outer.DescendantTypes().Select(t => t.Name),
                                         because: "DescendantTypes is how a rule opts into the recursive walk that used to be the default",
                                         fix: "Check QueryExtensions.DescendantTypes, it must yield nested types depth first and exclude the type itself");
        }

        [TestMethod]
        public void All_Methods_Reproduces_The_Recursive_Walk_Explicitly()
        {
            var outer = ParseCode(Code).Classes.Single();

            Assert.That.AreEqual<string>(["OuterMethod", "InnerMethod", "DeepMethod"],
                                         outer.AllMethods().Select(m => m.Name),
                                         because: "AllMethods is the named replacement for the old implicit recursion, so nothing is lost by the fix",
                                         fix: "Check QueryExtensions.AllMethods, it must flatten the type and all of its nested types");
        }

        [TestMethod]
        public void All_Types_Flattens_The_Whole_File()
        {
            var parsed = ParseCode(Code);

            Assert.That.AreEqual<string>(["Outer", "Inner", "Deepest"],
                                         parsed.AllTypes().Select(t => t.Name),
                                         because: "A rule over every type of a file needs one call that reaches nested types too",
                                         fix: "Check QueryExtensions.AllTypes, it must flatten Types with all of their nested types");
        }

        [TestMethod]
        public void Members_Of_A_Nested_Type_Are_Not_Counted_Twice()
        {
            var parsed = ParseCode(Code);

            Assert.That.HasCount(3,
                                 parsed.AllMethods(),
                                 because: "the file declares three methods, and a member must be reported by exactly one type",
                                 fix: "Make sure nested types are not collected at file level as well as by their declaring type");
        }
    }
}
