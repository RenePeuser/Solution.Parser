using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.Test.CSharp.ParseHelper;

namespace Solution.Parser.Test.CSharp
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

        [TestMethod]
        public void Implements_Matches_An_Interface_On_The_Base_List()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.IsTrue(repository.Implements("IDisposable"));
            Assert.IsTrue(repository.Implements("IEnumerable"), "An open generic name matches the closed one.");
            Assert.IsTrue(repository.Implements("IEnumerable<int>"));
            Assert.IsFalse(repository.Implements("IComparable"));
        }

        [TestMethod]
        public void Inherits_From_Matches_A_Base_Class()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.IsTrue(repository.InheritsFrom("RepositoryBase"));
            Assert.IsFalse(repository.InheritsFrom("OtherBase"));
        }

        [TestMethod]
        public void Predicates_Read_The_Way_A_Rule_Means()
        {
            var repository = ParseCode(Code).Classes.Single();

            Assert.IsTrue(repository.IsPublic());
            Assert.IsTrue(repository.IsSealed());
            Assert.IsFalse(repository.IsStatic());
            Assert.IsTrue(repository.Properties.Single().IsStatic());
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
                            where property.IsReadOnly.Equals(false)
                            select $"{property.Location}: {type.FullQualifiedName}.{property.Name} is mutable").ToList();

            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual($@"{FilePath}(5,5): My.Sample.Holder.Mutable is mutable", findings[0]);
        }

        [TestMethod]
        public void All_Types_Over_Several_Files_Is_One_Call()
        {
            var trees = new[]
            {
                ParseCode("public class A { public class Nested { } }"),
                ParseCode("public class B { }")
            };

            CollectionAssert.AreEqual(new[] { "A", "Nested", "B" }, trees.AllTypes().Select(t => t.Name).ToArray());
        }
    }
}
