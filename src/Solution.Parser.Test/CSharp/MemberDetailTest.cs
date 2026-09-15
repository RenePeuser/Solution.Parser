using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.Test.CSharp.ParseHelper;

namespace Solution.Parser.Test.CSharp
{
    [TestClass]
    public class MemberDetailTest
    {
        /// <summary>
        /// Methods, fields, events and enum members used to report the source of the whole file, because
        /// the Roslyn node property named SyntaxTree returns the tree the node belongs to, not the node.
        /// </summary>
        [TestMethod]
        public void Syntax_Tree_Is_The_Declaration_Not_The_File()
        {
            var parsed = ParseCode("""
                namespace My.Sample;

                public class Holder
                {
                    private int _value;

                    public void Go() { }
                }
                """);

            var holder = parsed.Classes.Single();

            Assert.That.AreEqual("public void Go() { }",
                                 holder.Methods.Single().SyntaxTree,
                                 because: "SyntaxTree must be the source of this declaration so a rule can quote just the offending member",
                                 fix: "Use node.ToString() instead of node.SyntaxTree.ToString(), which returns the whole file");

            Assert.That.AreEqual("private int _value;",
                                 holder.Fields.Single().SyntaxTree,
                                 because: "SyntaxTree must be the source of this declaration so a rule can quote just the offending member",
                                 fix: "Use node.ToString() instead of node.SyntaxTree.ToString(), which returns the whole file");

            Assert.That.DoesNotContain(holder.Methods.Single().SyntaxTree,
                                       "class Holder",
                                       because: "the source of a method must not drag in the declaring type",
                                       fix: "Use node.ToString() instead of node.SyntaxTree.ToString(), which returns the whole file");
        }

        /// <summary>Only the first variable of a shared declaration used to be reported.</summary>
        [TestMethod]
        public void Every_Variable_Of_A_Shared_Field_Declaration_Is_Reported()
        {
            var holder = ParseCode("public class Holder { private int _a, _b, _c; }").Classes.Single();

            Assert.That.AreEqual<string>(["_a", "_b", "_c"],
                                         holder.Fields.Select(f => f.Name),
                                         because: "all three variables are fields, and a rule over field names must see every one of them",
                                         fix: "Emit one Field per VariableDeclaratorSyntax instead of reading Declaration.Variables[0]");

            Assert.That.All(holder.Fields,
                            f => f.Type == "int",
                            "has the type int",
                            because: "every variable of a shared declaration has the type written once in front of them",
                            fix: "Read the type from the shared VariableDeclarationSyntax, not from the individual declarator");

            Assert.That.All(holder.Fields,
                            f => f.IsPartOfMultiVariableDeclaration,
                            "is flagged as part of a shared declaration",
                            because: "a rule that wants to ban shared declarations needs to recognise them",
                            fix: "Set IsPartOfMultiVariableDeclaration when the declaration holds more than one variable");
        }

        [TestMethod]
        public void Field_Initializer_And_Flags_Are_Reported()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    private const string Name = "x";
                    private readonly int? _count;
                }
                """).Classes.Single();

            var name = holder.Fields.Single(f => f.Name == "Name");
            var count = holder.Fields.Single(f => f.Name == "_count");

            Assert.That.AreEqual("\"x\"",
                                 name.Initializer?.Value,
                                 because: "the initializer is what a rule about magic values inspects",
                                 fix: "Read the initializer from the declarator, each variable may carry its own");

            Assert.That.IsTrue(name.IsConst,
                               because: "const is an accessibility relevant modifier that must survive the translation",
                               fix: "Check the shared modifier mapper covers the const keyword");

            Assert.That.IsTrue(count.IsReadOnly,
                               because: "readonly is what an immutability rule over fields asks about",
                               fix: "Check the shared modifier mapper covers the readonly keyword");

            Assert.That.IsTrue(count.IsNullable,
                               because: "int? is a nullable type and a rule over nullability must see it",
                               fix: "Decide nullability from NullableTypeSyntax rather than from the text of the type");

            Assert.That.IsNull(count.Initializer,
                               because: "the field has no initializer, and reporting one would be a false positive",
                               fix: "Leave Initializer null when the declarator carries no equals clause");
        }

        /// <summary>
        /// Read only used to be decided by searching the source of the property for the text "set;".
        /// </summary>
        [TestMethod]
        public void Read_Only_Follows_The_Accessors()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    public int Mutable { get; set; }
                    public int InitOnly { get; init; }
                    public int GetOnly { get; }
                    public int Expression => 42;
                    public int Blocks { get { return 1; } set { } }
                }
                """).Classes.Single();

            Assert.That.IsFalse(holder.Properties.Single(p => p.Name == "Mutable").IsReadOnly,
                                because: "a property with a set accessor is writable",
                                fix: "Decide IsReadOnly from the accessor list instead of from the source text");

            Assert.That.IsTrue(holder.Properties.Single(p => p.Name == "InitOnly").IsReadOnly,
                               because: "an init accessor cannot be used after construction, which is what the immutability rule in the readme asks about",
                               fix: "Treat init as read only and expose IsInitOnly separately for rules that need the distinction");

            Assert.That.IsTrue(holder.Properties.Single(p => p.Name == "InitOnly").IsInitOnly,
                               because: "a rule that requires init accessors needs to tell them from a plain getter",
                               fix: "Report the init accessor on Property.Accessors.Init");

            Assert.That.IsTrue(holder.Properties.Single(p => p.Name == "GetOnly").IsReadOnly,
                               because: "a get only property cannot be assigned from outside the constructor",
                               fix: "Decide IsReadOnly from the accessor list instead of from the source text");

            Assert.That.IsTrue(holder.Properties.Single(p => p.Name == "Expression").IsReadOnly,
                               because: "an expression bodied property is a getter and nothing else",
                               fix: "Treat an arrow clause as a get accessor when there is no accessor list");

            Assert.That.IsFalse(holder.Properties.Single(p => p.Name == "Blocks").IsReadOnly,
                                because: "a setter with a block is still a setter, even though the text never says set followed by a semicolon",
                                fix: "Decide IsReadOnly from the accessor list instead of from the source text");
        }

        [TestMethod]
        public void Property_Accessors_Are_Described()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    public int Value { get; private set; }
                    public int Computed => 1;
                }
                """).Classes.Single();

            var value = holder.Properties.Single(p => p.Name == "Value");
            var computed = holder.Properties.Single(p => p.Name == "Computed");

            Assert.That.IsTrue(value.IsAutoProperty,
                               because: "both accessors are bare semicolons, so the compiler supplies the backing field",
                               fix: "Set IsAutoProperty when no accessor has a body or an arrow clause");

            Assert.That.AreEqual(Accessibility.Private,
                                 value.Accessors.Set?.Accessibility,
                                 because: "a rule about publicly writable state has to see the accessor modifier, not only the property one",
                                 fix: "Resolve the accessibility of each accessor from its own modifier list");

            Assert.That.AreEqual(Accessibility.NotApplicable,
                                 value.Accessors.Get?.Accessibility,
                                 because: "an accessor without a modifier follows the property and must not be reported as private",
                                 fix: "Return NotApplicable when the accessor has no modifier of its own");

            Assert.That.IsTrue(computed.IsExpressionBodied,
                               because: "a rule about expression bodied members needs to recognise the arrow form",
                               fix: "Set IsExpressionBodied when the declaration carries an ArrowExpressionClauseSyntax");

            Assert.That.IsFalse(computed.IsAutoProperty,
                                because: "an expression bodied property computes its value and has no backing field",
                                fix: "Only treat a property as auto implemented when it has an accessor list of bare semicolons");
        }

        /// <summary>A nullable type argument used to make the whole property nullable.</summary>
        [TestMethod]
        public void Nullable_Reads_The_Type_Not_The_Question_Mark()
        {
            var holder = ParseCode("""
                using System.Collections.Generic;

                public class Holder
                {
                    public List<int?> Items { get; set; }
                    public string? Text { get; set; }
                    public string Plain { get; set; }
                }
                """).Classes.Single();

            Assert.That.IsFalse(holder.Properties.Single(p => p.Name == "Items").IsNullable,
                                because: "List<int?> is a non nullable list, the question mark belongs to the type argument",
                                fix: "Decide nullability from NullableTypeSyntax instead of searching the type text for a question mark");

            Assert.That.IsTrue(holder.Properties.Single(p => p.Name == "Text").IsNullable,
                               because: "string? is annotated as nullable",
                               fix: "Decide nullability from NullableTypeSyntax instead of searching the type text for a question mark");

            Assert.That.IsFalse(holder.Properties.Single(p => p.Name == "Plain").IsNullable,
                                because: "an unannotated type is not nullable",
                                fix: "Decide nullability from NullableTypeSyntax instead of searching the type text for a question mark");
        }

        [TestMethod]
        public void Method_Details_Are_Reported()
        {
            var holder = ParseCode("""
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class Holder
                {
                    public async Task<int> LoadAsync(int id, string name = "x") => await Task.FromResult(id);

                    public IEnumerable<int> Walk()
                    {
                        yield return 1;
                    }
                }
                """).Classes.Single();

            var load = holder.Methods.Single(m => m.Name == "LoadAsync");
            var walk = holder.Methods.Single(m => m.Name == "Walk");

            Assert.That.IsTrue(load.IsAsync,
                               because: "the method carries the async modifier, which is a fact rather than a guess from the return type",
                               fix: "Report IsAsync from the async modifier, check the shared modifier mapper covers it");

            Assert.That.IsTrue(load.IsExpressionBodied,
                               because: "the method body is an arrow clause",
                               fix: "Set IsExpressionBodied when the declaration carries an ArrowExpressionClauseSyntax");

            Assert.That.AreEqual("Task<int>",
                                 load.ReturnParameter,
                                 because: "the return type is what a rule about async signatures inspects",
                                 fix: "Read the return type from MethodDeclarationSyntax.ReturnType");

            Assert.That.HasCount(2,
                                 load.Parameters,
                                 because: "the method declares two parameters",
                                 fix: "Check ParameterListSyntaxExtensions.ToParameters");

            Assert.That.IsTrue(load.Parameters[1].IsOptional,
                               because: "the second parameter has a default value",
                               fix: "Set IsOptional when the parameter carries an equals clause");

            Assert.That.AreEqual("\"x\"",
                                 load.Parameters[1].DefaultValue,
                                 because: "a rule about default values needs the value as written",
                                 fix: "Read the default from ParameterSyntax.Default.Value");

            Assert.That.AreEqual(1,
                                 load.Parameters[1].Ordinal,
                                 because: "the position identifies a parameter when several share a name in an overload set",
                                 fix: "Pass the index into ToParameter when projecting the parameter list");

            Assert.That.IsTrue(walk.IsIterator,
                               because: "the body yields, which changes how the method executes and is worth a rule of its own",
                               fix: "Set IsIterator when the body contains a yield that does not belong to a nested function");

            Assert.That.IsFalse(walk.IsAsync,
                                because: "the method has no async modifier",
                                fix: "Report IsAsync from the async modifier rather than from the return type");
        }

        [TestMethod]
        public void Primary_Constructor_Is_Reported_As_A_Constructor()
        {
            var parsed = ParseCode("""
                public class Service(int retries, string name)
                {
                    public Service(int retries) : this(retries, "default") { }
                }
                """);

            var service = parsed.Classes.Single();

            Assert.That.HasCount(2,
                                 service.Constructors,
                                 because: "the primary constructor is a constructor, so a rule counting them must not miss it",
                                 fix: "Emit a Constructor with IsPrimary for a type that declares a parameter list");

            Assert.That.IsNotNull(service.PrimaryConstructor,
                                  because: "a rule about primary constructors needs to reach it directly",
                                  fix: "Expose PrimaryConstructor as the constructor flagged IsPrimary");

            Assert.That.HasCount(2,
                                 service.PrimaryConstructor.Parameters,
                                 because: "the primary constructor declares two parameters",
                                 fix: "Pass the type parameter list into ToPrimaryConstructor");

            Assert.That.AreEqual(ConstructorInitializerKind.This,
                                 service.Constructors.Single(c => !c.IsPrimary).InitializerKind,
                                 because: "constructor chaining is what a rule about duplicated initialisation looks for",
                                 fix: "Map the initializer keyword to This or Base");

            Assert.That.AreEqual<string>(["retries", "name"],
                                         service.Parameters.Select(p => p.Name),
                                         because: "the primary constructor wins over the longest declared constructor",
                                         fix: "Prefer the type parameter list over the constructor with the most parameters");
        }

        [TestMethod]
        public void Record_Positional_Parameters_Are_Its_Primary_Constructor()
        {
            var person = ParseCode("public record Person(string First, int Age);").Records.Single();

            Assert.That.AreEqual<string>(["First", "Age"],
                                         person.Parameters.Select(p => p.Name),
                                         because: "the positional parameters of a record are what most record rules inspect",
                                         fix: "Read the parameters from RecordDeclarationSyntax.ParameterList");

            Assert.That.IsTrue(person.PrimaryConstructor?.IsPrimary == true,
                               because: "a record always has a primary constructor when it declares positional parameters",
                               fix: "Emit a Constructor with IsPrimary for a type that declares a parameter list");
        }

        [TestMethod]
        public void Enum_Members_Carry_Their_Value()
        {
            var color = ParseCode("""
                using System;

                [Flags]
                public enum Color : byte
                {
                    Red = 1,
                    Green = 2,
                    Both = Red | Green,
                    Unset
                }
                """).Enums.Single();

            Assert.That.AreEqual("byte",
                                 color.UnderlyingType,
                                 because: "the underlying type decides the value range, which a rule about flag enums checks",
                                 fix: "Read the underlying type from the single entry of the enum base list");

            Assert.That.IsTrue(color.IsFlags,
                               because: "the attribute is written as Flags and must match with or without its Attribute suffix",
                               fix: "Compare attribute names through Attribute.IsNamed, which strips the suffix and the namespace");

            Assert.That.AreEqual("1",
                                 color.EnumFields.Single(f => f.Name == "Red").Value,
                                 because: "an explicit value is part of the contract of an enum member",
                                 fix: "Read the value from EnumMemberDeclarationSyntax.EqualsValue");

            Assert.That.AreEqual("Red | Green",
                                 color.EnumFields.Single(f => f.Name == "Both").Value,
                                 because: "a composed flag value must be reported as written",
                                 fix: "Report the expression as written rather than trying to evaluate it");

            Assert.That.IsFalse(color.EnumFields.Single(f => f.Name == "Unset").HasExplicitValue,
                                because: "a rule requiring explicit values needs to spot the member that has none",
                                fix: "Leave Value null when the member carries no equals clause");
        }
    }
}
