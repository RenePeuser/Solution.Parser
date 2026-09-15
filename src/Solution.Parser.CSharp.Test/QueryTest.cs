using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class QueryTest
    {
        private const string Code = """
            using System;
            using System.Collections.Generic;

            namespace My.Sample;

            public sealed class Repository : RepositoryBase, IDisposable, IEnumerable<int>
            {
                public static int Count { get; private set; }

                public void Dispose() { }
            }
            """;

        private const string BaseListFix =
            "Check QueryExtensions, both predicates search BaseTypes by name after stripping the namespace and the generic argument list";

        [TestMethod]
        public void Implements_Matches_An_Interface_On_The_Base_List()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.That.IsTrue(repository.Implements("IDisposable"),
                               because: "matching an interface by name is the most common thing a code rule does",
                               fix: BaseListFix);

            Assert.That.IsTrue(repository.Implements("IEnumerable"),
                               because: "a rule should be able to name an open generic without spelling out the type argument",
                               fix: BaseListFix);

            Assert.That.IsTrue(repository.Implements("IEnumerable<int>"),
                               because: "naming the closed generic must work as well as naming the open one",
                               fix: BaseListFix);

            Assert.That.IsFalse(repository.Implements("IComparable"),
                                because: "an interface that is not on the base list must not match, or every rule would pass",
                                fix: BaseListFix);
        }

        [TestMethod]
        public void Inherits_From_Matches_A_Base_Class()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.That.IsTrue(repository.InheritsFrom("RepositoryBase"),
                               because: "rules about layering are written in terms of the base class",
                               fix: BaseListFix);

            Assert.That.IsFalse(repository.InheritsFrom("OtherBase"),
                                because: "an unrelated base class must not match",
                                fix: BaseListFix);
        }

        [TestMethod]
        public void Predicates_Read_The_Way_A_Rule_Means()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.That.IsTrue(repository.IsPublic(),
                               because: "a rule guarding the public surface filters on this predicate",
                               fix: "Check QueryExtensions.IsPublic reads the resolved Accessibility");

            Assert.That.IsTrue(repository.IsSealed(),
                               because: "sealed is read from the modifier list, which used to drop the keyword",
                               fix: "Check the shared modifier mapper covers the sealed keyword");

            Assert.That.IsFalse(repository.IsStatic(),
                                because: "the class is not static and must not be reported as such",
                                fix: "Check QueryExtensions.IsStatic reads the modifier list");

            Assert.That.IsTrue(repository.Properties.Single().IsStatic(),
                               because: "the predicates apply to members as well as to types",
                               fix: "Declare the predicates on DeclarationWithModifiers so every member gets them");
        }

        /// <summary>
        /// The shape a code rule takes in practice: walk everything, filter, and report the finding with
        /// a location the reader can click.
        /// </summary>
        [TestMethod]
        public void A_Rule_Can_Report_A_Clickable_Finding()
        {
            var parsed = ParseCode("""
                namespace My.Sample;

                public class Holder
                {
                    public int Mutable { get; set; }
                }
                """);

            var findings = (from type in parsed.AllTypes()
                            from property in type.Properties
                            where !property.IsReadOnly
                            select $"{property.Location}: {type.FullQualifiedName}.{property.Name} is mutable").ToList();

            Assert.That.HasCount(1,
                                 findings,
                                 because: "the file declares exactly one mutable property",
                                 fix: "Check AllTypes and Property.IsReadOnly");

            Assert.That.AreEqual($"{FilePath}(5,5): My.Sample.Holder.Mutable is mutable",
                                 findings[0],
                                 because: "this is the end to end shape of a code rule finding: clickable location, qualified name, reason",
                                 fix: "Check CodeLocation.ToString and the fully qualified name of the declaring type");
        }

        [TestMethod]
        public void All_Types_Over_Several_Files_Is_One_Call()
        {
            var trees = new[]
            {
                ParseCode("public class A { public class Nested { } }"),
                ParseCode("public class B { }")
            };

            Assert.That.AreEqual<string>(["A", "Nested", "B"],
                                         trees.AllTypes().Select(t => t.Name),
                                         because: "a rule runs over a whole solution, so the overload over many files saves a SelectMany in every rule",
                                         fix: "Check the IEnumerable overload of QueryExtensions.AllTypes");
        }
    }
}
