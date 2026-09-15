using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// The fully qualified name used to be built by six near identical walks, five of which did not know
    /// the file scoped namespace and none of which handled a nested namespace.
    /// </summary>
    [TestClass]
    public class FullQualifiedNameTest
    {
        private const string FileScopedFix =
            "Check QualifiedNameExtensions.BuildFullQualifiedName, it must match BaseNamespaceDeclarationSyntax, " +
            "which covers the block scoped and the file scoped namespace alike";

        [TestMethod]
        public void File_Scoped_Namespace_Is_Part_Of_The_Name()
        {
            var parsed = ParseCode("""
                namespace My.Sample;

                public record Person(string First);
                public interface IThing { }
                public enum Color { Red }
                public struct Point { }
                public class Holder { }
                """);

            Assert.That.AreEqual<string>(["My.Sample.Person", "My.Sample.IThing", "My.Sample.Color", "My.Sample.Point", "My.Sample.Holder"],
                                         parsed.Types.Select(t => t.FullQualifiedName),
                                         because: "a file scoped namespace qualifies every declaration kind, not only classes",
                                         fix: FileScopedFix);
        }

        [TestMethod]
        public void Block_Scoped_Namespace_Is_Part_Of_The_Name()
        {
            var parsed = ParseCode("namespace My.Sample { public class Holder { } }");

            Assert.That.AreEqual("My.Sample.Holder",
                                 parsed.Classes.Single().FullQualifiedName,
                                 because: "the classic namespace form must qualify the name just like the file scoped one",
                                 fix: FileScopedFix);
        }

        [TestMethod]
        public void Nested_Namespaces_Are_Joined()
        {
            var parsed = ParseCode("namespace A { namespace B { public class C { } } }");

            Assert.That.AreEqual("A.B.C",
                                 parsed.Classes.Single().FullQualifiedName,
                                 because: "both enclosing namespaces belong to the qualified name",
                                 fix: "The parent walk must keep climbing after the first namespace instead of stopping at it");
        }

        [TestMethod]
        public void A_File_Without_A_Namespace_Has_No_Leading_Dot()
        {
            var parsed = ParseCode("public class Holder { }");

            Assert.That.AreEqual("Holder",
                                 parsed.Classes.Single().FullQualifiedName,
                                 because: "there is no namespace to prefix, so the name must not start with a separator",
                                 fix: "Join the qualifiers with a dot instead of interpolating a dot before the name");
        }

        [TestMethod]
        public void Nested_Types_And_Members_Carry_The_Whole_Path()
        {
            var parsed = ParseCode("""
                namespace My.Sample;

                public class Outer
                {
                    public class Inner
                    {
                        public int Value { get; set; }

                        public void Go() { }
                    }
                }
                """);

            var inner = parsed.Classes.Single().NestedTypes.Single();

            Assert.That.AreEqual("My.Sample.Outer.Inner",
                                 inner.FullQualifiedName,
                                 because: "a nested type is qualified by its declaring type as well as by its namespace",
                                 fix: "The parent walk must yield every enclosing BaseTypeDeclarationSyntax");

            Assert.That.AreEqual("My.Sample.Outer.Inner.Value",
                                 inner.Properties.Single().FullQualifiedName,
                                 because: "a rule reporting a property needs the path that identifies it uniquely",
                                 fix: "Build member names with the same shared walk used for types");

            Assert.That.AreEqual("My.Sample.Outer.Inner.Go",
                                 inner.Methods.Single().FullQualifiedName,
                                 because: "a rule reporting a method needs the path that identifies it uniquely",
                                 fix: "Build member names with the same shared walk used for types");
        }

        [TestMethod]
        public void Each_Declaration_Reports_Its_Own_Namespace()
        {
            var parsed = ParseCode("""
                namespace First { public class A { } }
                namespace Second { public class B { } }
                """);

            Assert.That.AreEqual("First",
                                 parsed.Types.Single(t => t.Name == "A").NameSpace.Name,
                                 because: "the namespace is a property of the declaration, not of the file",
                                 fix: "Resolve the namespace by walking up from the declaration instead of taking the first one in the file");

            Assert.That.AreEqual("Second",
                                 parsed.Types.Single(t => t.Name == "B").NameSpace.Name,
                                 because: "the second namespace of a file must not be reported as the first one",
                                 fix: "Resolve the namespace by walking up from the declaration instead of taking the first one in the file");

            Assert.That.AreEqual<string>(["First", "Second"],
                                         parsed.NameSpaces.Select(n => n.Name),
                                         because: "a file may declare more than one namespace and all of them are worth reporting",
                                         fix: "Check CSharpSyntaxTree.NameSpaces, it must list every namespace in source order");
        }
    }
}
