using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class TypeReferenceTest
    {
        /// <summary>Rules used to cut types like this apart with IndexOf('&lt;') and regular expressions.</summary>
        [TestMethod]
        public void Generic_Type_Is_Taken_Apart()
        {
            var type = TypeReference.Parse("Dictionary<string, List<int?>>");

            Assert.That.AreEqual("Dictionary",
                                 type.Name,
                                 because: "the name is what a rule compares, without the type arguments",
                                 fix: "Take the identifier of the GenericNameSyntax, not its source");

            Assert.That.AreEqual<string>(["string", "List<int?>"],
                                         type.TypeArguments.Select(t => t.SourceCode),
                                         because: "every type argument is a type of its own",
                                         fix: "Map each entry of the TypeArgumentList through ToTypeReference");

            var list = type.TypeArguments[1];

            Assert.That.IsFalse(list.IsNullable,
                                because: "the ? belongs to the type argument of the list, not to the list",
                                fix: "Only set IsNullable for a NullableTypeSyntax around this very type");

            Assert.That.IsTrue(list.TypeArguments.Single().IsNullable,
                               because: "int? is a nullable int",
                               fix: "Unwrap the NullableTypeSyntax and keep IsNullable on its element");

            Assert.That.AreEqual("int",
                                 list.TypeArguments.Single().Name,
                                 because: "a nullable int is still named int, the ? is a flag",
                                 fix: "Take the name from the element type of the NullableTypeSyntax");
        }

        [TestMethod]
        public void Qualified_Type_Keeps_The_Qualifier_Apart()
        {
            var type = TypeReference.Parse("global::System.Collections.Generic.List<int>");

            Assert.That.AreEqual("List",
                                 type.Name,
                                 because: "the name must not change with the way the namespace is written",
                                 fix: "Take the right part of the QualifiedNameSyntax as the type");

            Assert.That.AreEqual("System.Collections.Generic",
                                 type.Qualifier,
                                 because: "the qualifier says where the type lives; global:: adds nothing a rule needs",
                                 fix: "Strip the global:: prefix from the left part of the QualifiedNameSyntax");

            Assert.That.AreEqual("System.Collections.Generic.List",
                                 type.FullName,
                                 because: "FullName joins qualifier and name",
                                 fix: "Join Qualifier and Name with a dot");
        }

        [TestMethod]
        public void Task_Is_Unwrapped_To_What_Await_Gives()
        {
            Assert.That.AreEqual("List<Foo>",
                                 TypeReference.Parse("Task<List<Foo>>").UnwrapTask().SourceCode,
                                 because: "the result of an async method is what awaiting it gives",
                                 fix: "Return the single type argument of a Task or ValueTask");

            Assert.That.IsTrue(TypeReference.Parse("ValueTask").UnwrapTask().IsVoid,
                               because: "awaiting a plain task gives nothing",
                               fix: "Return void for a Task or ValueTask without a type argument");

            Assert.That.AreEqual("Foo",
                                 TypeReference.Parse("Foo").UnwrapTask().SourceCode,
                                 because: "a type that is no task is already the result, so a rule can unwrap any return type",
                                 fix: "Return the type itself when it is not a task");

            Assert.That.IsFalse(TypeReference.Parse("TaskCompletionSource<int>").IsTask,
                                because: "only Task and ValueTask are tasks, not every name starting with Task",
                                fix: "Compare the whole name, not a prefix");
        }

        [TestMethod]
        public void Collections_Are_Recognised_With_Their_Element()
        {
            Assert.That.AreEqual("Foo",
                                 TypeReference.Parse("IReadOnlyList<Foo>").ElementType?.SourceCode,
                                 because: "what a list holds is its one type argument",
                                 fix: "Return the single type argument of a known sequence type");

            Assert.That.AreEqual("int[]",
                                 TypeReference.Parse("int[][]").ElementType?.SourceCode,
                                 because: "a jagged array is an array of arrays",
                                 fix: "Build the element of an array from the rank specifiers that follow the first one");

            Assert.That.AreEqual(2,
                                 TypeReference.Parse("int[,]").ArrayRank,
                                 because: "the rank tells a two dimensional array from a jagged one",
                                 fix: "Take the rank of the first rank specifier");

            var dictionary = TypeReference.Parse("Dictionary<string, int>");

            Assert.That.IsTrue(dictionary.IsCollection && dictionary.IsDictionary,
                               because: "a dictionary is a collection of a special kind",
                               fix: "Keep the dictionary names in IsCollection as well as in IsDictionary");

            Assert.That.IsNull(dictionary.ElementType,
                               because: "a dictionary has a key and a value, not one element",
                               fix: "Return null as ElementType for a dictionary");

            Assert.That.IsFalse(TypeReference.Parse("string").IsCollection,
                                because: "nobody means a string when asking for a collection",
                                fix: "Leave string out of the known collection names");
        }

        [TestMethod]
        public void Is_Matches_The_Way_A_Rule_Means_It()
        {
            var logger = TypeReference.Parse("Microsoft.Extensions.Logging.ILogger<MyService>");

            Assert.That.IsTrue(logger.Is("ILogger"),
                               because: "asking for ILogger means any ILogger<T>",
                               fix: "Accept any type arguments when the pattern has none");

            Assert.That.IsTrue(logger.Is("ILogger<MyService>"),
                               because: "the exact type must match",
                               fix: "Compare the type arguments pairwise when the pattern has some");

            Assert.That.IsFalse(logger.Is("ILogger<OtherService>"),
                                because: "a different type argument is a different type",
                                fix: "Compare the type arguments pairwise when the pattern has some");

            Assert.That.IsFalse(logger.Is("Other.Namespace.ILogger"),
                                because: "two written namespaces that differ are two different types",
                                fix: "Compare the qualifiers when both sides write one");

            Assert.That.IsTrue(TypeReference.Parse("Guid").Is("System.Guid"),
                               because: "a using directive is the usual reason why no namespace is written",
                               fix: "Only compare the qualifiers when both sides write one");

            Assert.That.IsTrue(TypeReference.Parse("string").Is("String"),
                               because: "string and String are the same type",
                               fix: "Map keywords to their framework names before comparing");

            Assert.That.IsTrue(TypeReference.Parse("int?").Is("int") && !TypeReference.Parse("int").Is("int?"),
                               because: "the ? only counts when the pattern asks for it",
                               fix: "Require IsNullable only when the pattern is nullable");

            Assert.That.IsTrue(TypeReference.Parse("Dictionary<string, int>").Is("Dictionary<,>"),
                               because: "an omitted type argument stands for any type",
                               fix: "Treat a pattern argument without a name as a wildcard");
        }

        [TestMethod]
        public void Is_Takes_The_Type_As_Type_Argument()
        {
            Assert.That.IsTrue(TypeReference.Parse("Guid").Is<System.Guid>() && TypeReference.Parse("System.Guid").Is<System.Guid>(),
                               because: "the full name of the runtime type must match with and without a written namespace",
                               fix: "Write the namespace into the pattern in ClrTypeName and compare qualifiers only when both are written");

            Assert.That.IsFalse(TypeReference.Parse("Other.Guid").Is<System.Guid>(),
                                because: "the type argument knows its namespace, a different written one is a different type",
                                fix: "Write the namespace into the pattern in ClrTypeName");

            Assert.That.IsTrue(TypeReference.Parse("int").Is<int>() && TypeReference.Parse("string").Is<string>(),
                               because: "typeof(int) is System.Int32, which is the keyword int",
                               fix: "Map keywords to their framework names before comparing");

            Assert.That.IsTrue(TypeReference.Parse("List<int>").Is<System.Collections.Generic.List<int>>() &&
                               !TypeReference.Parse("List<string>").Is<System.Collections.Generic.List<int>>(),
                               because: "a closed generic type argument names exactly one type",
                               fix: "Write the type arguments as C# in ClrTypeName instead of the runtime name List`1");

            Assert.That.IsTrue(TypeReference.Parse("int?").Is<int?>() && !TypeReference.Parse("int").Is<int?>(),
                               because: "Nullable<int> is written int? in source",
                               fix: "Write a Nullable<T> as T? in ClrTypeName");

            Assert.That.IsTrue(TypeReference.Parse("(int, string)").Is<(int, string)>(),
                               because: "a ValueTuple is written as tuple in source",
                               fix: "Write a ValueTuple as (A, B) in ClrTypeName");

            Assert.That.IsTrue(TypeReference.Parse("int[]").Is<int[]>() && !TypeReference.Parse("int").Is<int[]>(),
                               because: "an array is written with brackets",
                               fix: "Write an array as element[] in ClrTypeName");

            Assert.That.IsTrue(TypeReference.Parse("Environment.SpecialFolder").Is<System.Environment.SpecialFolder>() &&
                               TypeReference.Parse("SpecialFolder").Is<System.Environment.SpecialFolder>(),
                               because: "a nested type is written through its containing type, which a using covers only partly",
                               fix: "Write the containing type into the pattern and let qualifiers agree when one ends with the other");

            Assert.That.IsTrue(TypeReference.Parse("Task<List<DateTime?>>").Mentions<System.DateTime>(),
                               because: "Mentions must find the type argument however deeply it is nested",
                               fix: "Pass the name from ClrTypeName to Mentions(string)");
        }

        [TestMethod]
        public void Base_Types_Match_By_Type_Argument()
        {
            var holder = ParseCode("""
                public class Holder : System.Collections.ObjectModel.Collection<int>, IDisposable, IEquatable<Holder>
                {
                }
                """).Classes.Single();

            Assert.That.IsTrue(holder.Implements<System.IDisposable>() && holder.InheritsFrom<System.Collections.ObjectModel.Collection<int>>(),
                               because: "a rule should not have to write nameof for a framework type it can reference",
                               fix: "Compare the TypeReference of each base type through Is<T>");

            Assert.That.IsFalse(holder.InheritsFrom<System.Collections.ObjectModel.Collection<string>>(),
                                because: "a closed generic type argument names exactly one type",
                                fix: "Compare the TypeReference of each base type through Is<T>, not through the simplified name");

            Assert.That.IsFalse(holder.Implements<System.IAsyncDisposable>(),
                                because: "an interface that is not in the base list is not implemented",
                                fix: "Compare the TypeReference of each base type through Is<T>");

            // Implements with a class is a mistake in the rule that should show at once.
            Assert.ThrowsExactly<System.ArgumentException>(() => holder.Implements<System.Exception>());
        }

        [TestMethod]
        public void Mentions_Looks_Inside_The_Type()
        {
            var type = TypeReference.Parse("Task<(int Id, List<DateTime?>[] Dates)>");

            Assert.That.IsTrue(type.Mentions("DateTime"),
                               because: "a rule banning DateTime must find it however deeply it is nested",
                               fix: "Walk type arguments, tuple elements and array elements in SelfAndDescendants");

            Assert.That.IsFalse(type.Mentions("DateTimeOffset"),
                                because: "only whole names match",
                                fix: "Compare through Is, not through Contains on the source");

            Assert.That.AreEqual<string>(["Id", "Dates"],
                                         type.TypeArguments.Single().TupleElements.Select(e => e.Name!),
                                         because: "the names of the tuple elements are part of the contract",
                                         fix: "Read the identifier of each TupleElementSyntax");
        }

        /// <summary>Every declaration with a type now carries it taken apart, with a location.</summary>
        [TestMethod]
        public void Declarations_Carry_Their_Type_Taken_Apart()
        {
            var holder = ParseCode("""
                public class Holder : Base<int>, IDisposable
                {
                    private readonly ILogger<Holder> _logger;

                    public IReadOnlyList<string> Names { get; } = [];

                    public async Task<List<Order>> LoadAsync(CancellationToken token) => [];
                }
                """).Classes.Single();

            Assert.That.AreEqual("ILogger",
                                 holder.Fields.Single().TypeReference.Name,
                                 because: "a field type is what a rule about injected dependencies inspects",
                                 fix: "Set Field.TypeReference from the type of the variable declaration");

            Assert.That.AreEqual("string",
                                 holder.Properties.Single().TypeReference.ElementType?.Name,
                                 because: "a property type must be taken apart like any other",
                                 fix: "Set Property.TypeReference from the property type");

            var load = holder.Methods.Single();

            Assert.That.AreEqual("Order",
                                 load.ReturnType.UnwrapTask().ElementType?.Name,
                                 because: "the return type taken apart is what the async and API rules inspect",
                                 fix: "Set Method.ReturnType from the return type syntax");

            Assert.That.AreEqual("CancellationToken",
                                 load.Parameters.Single().TypeReference.Name,
                                 because: "a parameter type must be taken apart like any other",
                                 fix: "Set Parameter.TypeReference from the parameter type");

            Assert.That.AreEqual(7,
                                 load.ReturnType.Location.StartLine,
                                 because: "a finding about a type must point at the type",
                                 fix: "Give the TypeReference the location of its TypeSyntax");

            Assert.That.IsTrue(holder.BaseTypes[0].TypeReference.Is("Base<int>"),
                               because: "a base type must be taken apart like any other",
                               fix: "Set BaseType.TypeReference in ToBaseTypes");
        }
    }
}
