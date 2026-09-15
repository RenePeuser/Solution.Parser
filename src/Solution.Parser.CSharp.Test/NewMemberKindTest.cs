using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
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

            Assert.That.AreEqual("My.Sample.Transform",
                                 transform.FullQualifiedName,
                                 because: "a delegate is a type of its own and used to be skipped by the parser altogether",
                                 fix: "Include DelegateDeclarationSyntax in the top level walk and convert it");

            Assert.That.AreEqual("int",
                                 transform.ReturnParameter,
                                 because: "the signature is the whole content of a delegate declaration",
                                 fix: "Read the return type of the delegate declaration");

            Assert.That.AreEqual("input",
                                 transform.Parameters.Single().Name,
                                 because: "the signature is the whole content of a delegate declaration",
                                 fix: "Read the parameter list of the delegate declaration");

            Assert.That.AreEqual("Notify",
                                 parsed.Classes.Single().Delegates.Single().Name,
                                 because: "a delegate nested in a type belongs to that type",
                                 fix: "Collect DelegateDeclarationSyntax from the direct members of a type as well");
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

            Assert.That.AreEqual("string",
                                 indexer.Type,
                                 because: "an indexer is part of the public surface a rule guards and used to be invisible",
                                 fix: "Collect IndexerDeclarationSyntax from the direct members of a type");

            Assert.That.AreEqual("index",
                                 indexer.Parameters.Single().Name,
                                 because: "the parameter list is what distinguishes one indexer from another",
                                 fix: "Read the bracketed parameter list of the indexer");

            Assert.That.IsTrue(indexer.HasGetter,
                               because: "an indexer has accessors just like a property and rules about them should apply",
                               fix: "Reuse AccessorExtensions.ToAccessors for the indexer");

            Assert.That.IsTrue(indexer.HasSetter,
                               because: "an indexer has accessors just like a property and rules about them should apply",
                               fix: "Reuse AccessorExtensions.ToAccessors for the indexer");
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

            Assert.That.HasCount(3,
                                 holder.Operators,
                                 because: "operators and both conversion forms are members and used to be dropped",
                                 fix: "Collect OperatorDeclarationSyntax and ConversionOperatorDeclarationSyntax into Operators");

            var addition = holder.Operators.Single(o => o.OperatorKind == OperatorKind.Operator);

            Assert.That.AreEqual("+",
                                 addition.Symbol,
                                 because: "the symbol is what identifies an operator",
                                 fix: "Read the operator token of the declaration");

            Assert.That.HasCount(2,
                                 addition.Parameters,
                                 because: "a binary operator takes two operands",
                                 fix: "Read the parameter list of the operator declaration");

            Assert.That.AreEqual("decimal",
                                 holder.Operators.Single(o => o.OperatorKind == OperatorKind.ImplicitConversion).Symbol,
                                 because: "an implicit conversion is the one a rule is most likely to want to ban",
                                 fix: "Map the implicit keyword to OperatorKind.ImplicitConversion");

            Assert.That.AreEqual("Money",
                                 holder.Operators.Single(o => o.OperatorKind == OperatorKind.ExplicitConversion).Symbol,
                                 because: "the target type is what identifies a conversion operator",
                                 fix: "Map the explicit keyword to OperatorKind.ExplicitConversion");
        }

        [TestMethod]
        public void Finalizers_Are_Reported()
        {
            var holder = ParseCode("public class Holder { ~Holder() { } }").Classes.Single();

            Assert.That.AreEqual("Holder",
                                 holder.Finalizers.Single().Name,
                                 because: "a finalizer has real consequences for a type and a rule should be able to spot one",
                                 fix: "Collect DestructorDeclarationSyntax from the direct members of a type");
        }

        [TestMethod]
        public void Record_Struct_Is_Told_Apart_From_Record_Class()
        {
            var parsed = ParseCode("""
                public record Person(string Name);
                public record struct Point(int X, int Y);
                public record class Company(string Name);
                """);

            Assert.That.AreEqual(TypeKind.Record,
                                 parsed.Records.Single(r => r.Name == "Person").Kind,
                                 because: "a plain record is a reference type",
                                 fix: "Derive the kind from RecordDeclarationSyntax.ClassOrStructKeyword");

            Assert.That.AreEqual(TypeKind.RecordStruct,
                                 parsed.Records.Single(r => r.Name == "Point").Kind,
                                 because: "a record struct is a value type, which changes what an immutability rule should require",
                                 fix: "Derive the kind from RecordDeclarationSyntax.ClassOrStructKeyword");

            Assert.That.AreEqual(TypeKind.Record,
                                 parsed.Records.Single(r => r.Name == "Company").Kind,
                                 because: "record class is the explicit spelling of a plain record",
                                 fix: "Derive the kind from RecordDeclarationSyntax.ClassOrStructKeyword");
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

            Assert.That.AreEqual("Changed",
                                 holder.EventFields.Single().Name,
                                 because: "a field like event is declared without accessors and belongs in EventFields",
                                 fix: "Collect EventFieldDeclarationSyntax into EventFields");

            Assert.That.AreEqual("Explicit",
                                 holder.Events.Single().Name,
                                 because: "an event with accessors belongs in Events",
                                 fix: "Collect EventDeclarationSyntax into Events");

            Assert.That.IsTrue(holder.Events.Single().HasAdd,
                               because: "a rule about event subscription needs to see the accessors",
                               fix: "Read the accessor list of the event declaration");

            Assert.That.IsTrue(holder.Events.Single().HasRemove,
                               because: "a rule about event subscription needs to see the accessors",
                               fix: "Read the accessor list of the event declaration");
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

            Assert.That.IsOfType<Class>(parsed.Types.Single(t => t.Name == "A"),
                                        because: "a class must be modelled as a class",
                                        fix: "Check the seed switch in TypeDeclarationSyntaxExtensions");

            Assert.That.IsNotOfType<Interface>(parsed.Types.Single(t => t.Name == "A"),
                                               because: "Class used to derive from Interface, which made every is check against Interface true",
                                               fix: "Keep Class, Struct, Record and Interface as siblings below TypeDeclaration");

            Assert.That.IsNotOfType<Class>(parsed.Types.Single(t => t.Name == "C"),
                                           because: "Record used to derive from Class, so a rule over classes silently included records",
                                           fix: "Keep Class, Struct, Record and Interface as siblings below TypeDeclaration");

            Assert.That.IsNotOfType<Class>(parsed.Types.Single(t => t.Name == "D"),
                                           because: "Struct used to derive from Class, so a rule over classes silently included structs",
                                           fix: "Keep Class, Struct, Record and Interface as siblings below TypeDeclaration");

            Assert.That.HasCount(1, parsed.Classes, because: "the file declares exactly one class", fix: "Filter Types by the concrete model type");
            Assert.That.HasCount(1, parsed.Interfaces, because: "the file declares exactly one interface", fix: "Filter Types by the concrete model type");
            Assert.That.HasCount(1, parsed.Records, because: "the file declares exactly one record", fix: "Filter Types by the concrete model type");
            Assert.That.HasCount(1, parsed.Structs, because: "the file declares exactly one struct", fix: "Filter Types by the concrete model type");
            Assert.That.HasCount(1, parsed.Enums, because: "the file declares exactly one enum", fix: "Filter Types by the concrete model type");

            Assert.That.HasCount(5,
                                 parsed.Types,
                                 because: "Types is the one list that holds every declared type regardless of kind",
                                 fix: "Collect every BaseTypeDeclarationSyntax of the file into Types");
        }
    }
}
