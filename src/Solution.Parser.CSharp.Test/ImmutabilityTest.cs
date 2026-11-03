using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// A rule about immutable models asks whether a property type can change. A collection is exactly
    /// one of immutable, read only and mutable, and only a type immutable all the way down is immutable.
    /// </summary>
    [TestClass]
    public class ImmutabilityTest
    {
        private static TypeReference TypeOf(string type)
        {
            return ParseCode($"public record Holder {{ public {type} Value {{ get; init; }} }}").Records.Single().Properties.Single().TypeReference;
        }

        [TestMethod]
        [DataRow("ImmutableList<int>", "immutable")]
        [DataRow("ImmutableArray<string>", "immutable")]
        [DataRow("IImmutableDictionary<string, int>", "immutable")]
        [DataRow("FrozenSet<Guid>", "immutable")]
        [DataRow("IReadOnlyList<int>", "read only")]
        [DataRow("IEnumerable<int>", "read only")]
        [DataRow("IReadOnlyDictionary<string, int>", "read only")]
        [DataRow("ReadOnlyCollection<int>", "read only")]
        [DataRow("List<int>", "mutable")]
        [DataRow("int[]", "mutable")]
        [DataRow("Dictionary<string, int>", "mutable")]
        [DataRow("IList<int>", "mutable")]
        [DataRow("HashSet<int>", "mutable")]
        [DataRow("ConcurrentDictionary<string, int>", "mutable")]
        [DataRow("ObservableCollection<int>", "mutable")]
        public void A_Collection_Is_Exactly_One_Of_Immutable_Read_Only_And_Mutable(string type, string expected)
        {
            var reference = TypeOf(type);

            string[] actual =
            [
                ..reference.IsImmutableCollection ? new[] { "immutable" } : [],
                ..reference.IsReadOnlyCollection ? new[] { "read only" } : [],
                ..reference.IsMutableCollection ? new[] { "mutable" } : []
            ];

            Assert.That.AreEqual<string>([expected],
                                         actual,
                                         because: $"{type} is a {expected} collection, and only that",
                                         fix: "Check the collection name sets in TypeReference");
        }

        [TestMethod]
        [DataRow("string", true)]
        [DataRow("int?", true)]
        [DataRow("decimal", true)]
        [DataRow("Guid", true)]
        [DataRow("System.DateTimeOffset", true)]
        [DataRow("TimeSpan?", true)]
        [DataRow("ImmutableList<string>", true)]
        [DataRow("ImmutableDictionary<string, ImmutableList<int>>", true)]
        [DataRow("(int Id, string Name)", true)]
        [DataRow("KeyValuePair<string, int>", true)]
        [DataRow("ImmutableList<List<int>>", false)]
        [DataRow("IReadOnlyList<string>", false)]
        [DataRow("List<string>", false)]
        [DataRow("string[]", false)]
        [DataRow("object", false)]
        [DataRow("StringBuilder", false)]
        [DataRow("(int Id, List<int> Lines)", false)]
        [DataRow("Order", false)]
        [DataRow("ImmutableList<Order>", false)]
        public void Immutable_Means_All_The_Way_Down(string type, bool expected)
        {
            Assert.That.AreEqual(expected,
                                 TypeOf(type).IsImmutable,
                                 because: expected ? $"no value of {type} can change" : $"{type} can change, or is not known to be immutable",
                                 fix: "Check TypeReference.IsImmutableWith");
        }

        [TestMethod]
        public void Own_Types_Count_When_The_Rule_Says_So()
        {
            var tree = ParseCode("""
                public record Order(string Id, ImmutableList<Line> Lines);
                public record Line(string Sku, int Count);
                public class Cart { public List<Line> Lines { get; } = []; }
                public record Invoice(ImmutableList<Order> Orders, Cart Cart);
                """);

            // A rule's own notion: a record whose properties are all init only.
            var immutable = tree.Records.Where(r => r.Properties.All(p => !p.HasSetter)).Select(r => r.Name).ToHashSet();

            var orders = tree.Records.Single(r => r.Name == "Invoice").Properties;

            Assert.That.IsTrue(orders.Single(p => p.Name == "Orders").TypeReference.IsImmutableWith(t => immutable.Contains(t.Name)),
                               because: "an immutable list of immutable records is immutable, down to the Line records",
                               fix: "Ask isImmutableType for every type argument the parser does not know");

            Assert.That.IsFalse(orders.Single(p => p.Name == "Cart").TypeReference.IsImmutableWith(t => immutable.Contains(t.Name)),
                                because: "Cart is a class the rule does not vouch for",
                                fix: "Leave unknown types to isImmutableType");
        }

        /// <summary>A positional record declared its properties as parameters only, so a rule over Properties saw none of them.</summary>
        [TestMethod]
        public void A_Positional_Record_Reports_Its_Properties()
        {
            var tree = ParseCode("""
                namespace Shop;

                public record Order([property: JsonPropertyName("id")] string Id, List<Line> Lines, [Required] string Note)
                {
                    public string Note { get; set; } = Note;
                    public int Count => Lines.Count;
                }

                public record struct Point(int X, int Y);
                public readonly record struct Size(int Width, int Height);
                public class Service(ILogger logger);
                """);

            var order = tree.Records.Single(r => r.Name == "Order");

            Assert.That.AreEqual<string>(["Id", "Lines", "Note", "Count"],
                                         order.Properties.Select(p => p.Name),
                                         because: "the compiler writes a property for every parameter the record does not declare itself",
                                         fix: "Synthesize the positional properties in TypeDeclarationSyntaxExtensions.ToProperties");

            var id = order.Properties[0];

            Assert.That.IsTrue(id is { IsPositional: true, IsReadOnly: true, IsInitOnly: true, IsAutoProperty: true, Accessibility: Accessibility.Public, FullyQualifiedName: "Shop.Order.Id" },
                               because: "a positional property of a record is public with get and init",
                               fix: "Give the positional property get and init accessors");

            Assert.That.AreEqual("JsonPropertyName",
                                 id.Attributes.Single().Name,
                                 because: "an attribute with the property target belongs to the property",
                                 fix: "Keep the attributes targeting property");

            Assert.That.IsTrue(order.Properties[1].TypeReference.IsMutableCollection,
                               because: "a rule against mutable collections in records must see List<Line> Lines",
                               fix: "Take the type of the positional property from the parameter");

            Assert.That.IsFalse(order.Properties.Single(p => p.Name == "Note").IsPositional,
                                because: "Note is declared by the record itself, which wins over the parameter",
                                fix: "Skip parameters the record declares a property for");

            Assert.That.IsTrue(tree.Records.Single(r => r.Name == "Point").Properties.All(p => p.HasSetter && !p.IsReadOnly),
                               because: "a record struct that is not readonly gets get and set",
                               fix: "Give a mutable record struct set accessors");

            Assert.That.IsTrue(tree.Records.Single(r => r.Name == "Size").Properties.All(p => p.IsReadOnly && p.IsInitOnly),
                               because: "a readonly record struct gets get and init",
                               fix: "Check the readonly modifier of the record struct");

            Assert.That.IsTrue(tree.Classes.Single().Properties.IsEmpty,
                               because: "the primary constructor of a class writes no property",
                               fix: "Only synthesize properties for records");
        }
    }
}
