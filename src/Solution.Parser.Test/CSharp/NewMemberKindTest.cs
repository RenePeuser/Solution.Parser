using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.Test.CSharp.ParseHelper;

namespace Solution.Parser.Test.CSharp
{
    /// <summary>
    /// Delegates, indexers, operators, conversion operators and finalizers were dropped entirely, and a
    /// record struct was indistinguishable from a record class.
    /// </summary>
    [TestClass]
    public class NewMemberKindTest
    {
        [TestMethod]
        public void Delegates_Are_Reported_At_File_Level_And_Nested()
        {
            var parsed = ParseCode("""
                namespace My.Sample;

                public delegate int Transform(string input);

                public class Holder
                {
                    public delegate void Notify();
                }
                """);

            var transform = parsed.Delegates.Single();

            Assert.AreEqual("My.Sample.Transform", transform.FullQualifiedName);
            Assert.AreEqual("int", transform.ReturnParameter);
            Assert.AreEqual("input", transform.Parameters.Single().Name);
            Assert.AreEqual("Notify", parsed.Classes.Single().Delegates.Single().Name);
        }

        [TestMethod]
        public void Indexers_Are_Reported()
        {
            var indexer = ParseCode("""
                public class Holder
                {
                    public string this[int index] { get => "x"; set { } }
                }
                """).Classes.Single().Indexers.Single();

            Assert.AreEqual("string", indexer.Type);
            Assert.AreEqual("index", indexer.Parameters.Single().Name);
            Assert.IsTrue(indexer.HasGetter);
            Assert.IsTrue(indexer.HasSetter);
        }

        [TestMethod]
        public void Operators_And_Conversions_Are_Reported()
        {
            var holder = ParseCode("""
                public class Money
                {
                    public static Money operator +(Money left, Money right) => left;

                    public static implicit operator decimal(Money money) => 0m;

                    public static explicit operator Money(decimal value) => new Money();
                }
                """).Classes.Single();

            Assert.AreEqual(3, holder.Operators.Count);

            var addition = holder.Operators.Single(o => o.OperatorKind == OperatorKind.Operator);

            Assert.AreEqual("+", addition.Symbol);
            Assert.AreEqual(2, addition.Parameters.Count);
            Assert.AreEqual("decimal", holder.Operators.Single(o => o.OperatorKind == OperatorKind.ImplicitConversion).Symbol);
            Assert.AreEqual("Money", holder.Operators.Single(o => o.OperatorKind == OperatorKind.ExplicitConversion).Symbol);
        }

        [TestMethod]
        public void Finalizers_Are_Reported()
        {
            var holder = ParseCode("public class Holder { ~Holder() { } }").Classes.Single();

            Assert.AreEqual("Holder", holder.Finalizers.Single().Name);
        }

        [TestMethod]
        public void Record_Struct_Is_Told_Apart_From_Record_Class()
        {
            var parsed = ParseCode("""
                public record Person(string Name);
                public record struct Point(int X, int Y);
                public record class Company(string Name);
                """);

            Assert.AreEqual(TypeKind.Record, parsed.Records.Single(r => r.Name == "Person").Kind);
            Assert.AreEqual(TypeKind.RecordStruct, parsed.Records.Single(r => r.Name == "Point").Kind);
            Assert.AreEqual(TypeKind.Record, parsed.Records.Single(r => r.Name == "Company").Kind);
        }

        [TestMethod]
        public void Events_Are_Told_Apart_From_Event_Fields()
        {
            var holder = ParseCode("""
                using System;

                public class Holder
                {
                    public event EventHandler? Changed;

                    public event EventHandler Explicit
                    {
                        add { }
                        remove { }
                    }
                }
                """).Classes.Single();

            Assert.AreEqual("Changed", holder.EventFields.Single().Name);
            Assert.AreEqual("Explicit", holder.Events.Single().Name);
            Assert.IsTrue(holder.Events.Single().HasAdd);
            Assert.IsTrue(holder.Events.Single().HasRemove);
        }

        /// <summary>
        /// The model hierarchy used to make Record derive from Class and Class from Interface, so an
        /// <c>is</c> check could not tell a class from an interface.
        /// </summary>
        [TestMethod]
        public void Type_Kinds_Are_Siblings()
        {
            var parsed = ParseCode("""
                public class A { }
                public interface IB { }
                public record C();
                public struct D { }
                public enum E { X }
                """);

            Assert.IsInstanceOfType<Class>(parsed.Types.Single(t => t.Name == "A"));
            Assert.IsNotInstanceOfType<Interface>(parsed.Types.Single(t => t.Name == "A"));
            Assert.IsNotInstanceOfType<Class>(parsed.Types.Single(t => t.Name == "C"));
            Assert.IsNotInstanceOfType<Class>(parsed.Types.Single(t => t.Name == "D"));

            Assert.AreEqual(1, parsed.Classes.Count);
            Assert.AreEqual(1, parsed.Interfaces.Count);
            Assert.AreEqual(1, parsed.Records.Count);
            Assert.AreEqual(1, parsed.Structs.Count);
            Assert.AreEqual(1, parsed.Enums.Count);
            Assert.AreEqual(5, parsed.Types.Count);
        }
    }
}
