using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    [TestClass]
    public class AccessorAndExtensionBlockTest
    {
        /// <summary>An accessor only had its body as text, so a call in a getter was invisible to every rule.</summary>
        [TestMethod]
        public void An_Accessor_Carries_Its_Body()
        {
            var holder = ParseCode("""
                namespace Shop;

                public class Cart
                {
                    private decimal? _total;

                    public decimal Total
                    {
                        get { return _total ??= Calculate(); }
                        private set => _total = Validate(value);
                    }

                    public string Label => Format(Total);

                    public int this[int i] { get => Lookup(i); }

                    public event EventHandler Changed
                    {
                        add { Subscribe(value); }
                        remove { Unsubscribe(value); }
                    }
                }
                """).Classes.Single();

            var total = holder.Properties.Single(p => p.Name == "Total");
            var getter = total.Accessors.Get!;
            var setter = total.Accessors.Set!;

            Assert.That.AreEqual("Calculate",
                                 getter.Invocations.Single().Name,
                                 because: "a call in a getter runs like a call in a method",
                                 fix: "Build the accessor body with ToMemberBody in AccessorExtensions");

            Assert.That.AreEqual(StatementKind.Return,
                                 getter.BodyStatements.Single().Kind,
                                 because: "a getter with a block has statements",
                                 fix: "Build the accessor body with ToMemberBody in AccessorExtensions");

            Assert.That.IsTrue(setter.IsExpressionBodied && setter.Invocations.Single().Name == "Validate" && setter.Accessibility == Accessibility.Private,
                               because: "an expression bodied setter keeps its call and its own accessibility",
                               fix: "Read Body and ExpressionBody of the AccessorDeclarationSyntax");

            Assert.That.AreEqual("Shop.Cart.Total.get",
                                 getter.FullyQualifiedName,
                                 because: "an accessor is named after its property",
                                 fix: "Name the accessor by its keyword below the property");

            var label = holder.Properties.Single(p => p.Name == "Label").Accessors.Get!;

            Assert.That.IsTrue(label.IsExpressionBodied && label.Invocations.Single().Name == "Format",
                               because: "an expression bodied property is a getter whose body is the expression",
                               fix: "Build the getter of an expression bodied property from its ArrowExpressionClause");

            Assert.That.AreEqual("Lookup",
                                 holder.Indexers.Single().Accessors.Get!.Invocations.Single().Name,
                                 because: "an indexer accessor carries its body as well",
                                 fix: "Indexers share AccessorExtensions.ToAccessors");

            var changed = holder.Events.Single();

            Assert.That.IsTrue(changed.Add!.Invocations.Single().Name == "Subscribe" && changed.Remove!.Kind == AccessorKind.Remove,
                               because: "the add and remove accessors of an event carry their bodies",
                               fix: "Set Event.Add and Event.Remove in EventDeclarationSyntaxExtensions");

            Assert.That.AreEqual<string>(["Calculate", "Validate", "Format", "Lookup", "Subscribe", "Unsubscribe"],
                                         ParseCode(holder.SourceCode).Invocations.Select(i => i.Name),
                                         because: "a rule over every call of a file must see the calls in accessors",
                                         fix: "Include the accessors in QueryExtensions.AllMembersWithBody(TypeDeclaration), which CSharpSyntaxTree.MembersWithBody builds on");
        }

        [TestMethod]
        public void An_Auto_Property_Has_Accessors_Without_Body()
        {
            var property = ParseCode("public class Holder { public int Id { get; init; } }").Classes.Single().Properties.Single();

            Assert.That.IsTrue(property.Accessors.Get!.IsAutoImplemented && !property.Accessors.Get.HasBody && property.Accessors.Init is not null,
                               because: "get; has no body for the compiler supplies it",
                               fix: "Set IsAutoImplemented when the accessor has neither block nor expression");
        }

        /// <summary>An extension block was taken for a nested class without a name, and its receiver was lost.</summary>
        [TestMethod]
        public void An_Extension_Block_Is_Not_A_Nested_Type()
        {
            var tree = ParseCode("""
                namespace Shop;

                public static class StringExtensions
                {
                    extension(string value)
                    {
                        public bool IsBlank => string.IsNullOrWhiteSpace(value);

                        public string Shout() => value.ToUpper();
                    }

                    extension<T>(IEnumerable<T> source) where T : class
                    {
                        public static IEnumerable<T> Empty() => [];
                    }
                }
                """);

            var type = tree.Classes.Single();

            Assert.That.IsTrue(type.NestedTypes.IsEmpty,
                               because: "an extension block declares no type",
                               fix: "Skip ExtensionBlockDeclarationSyntax in ToTypeDeclarationOrDefault");

            Assert.That.AreEqual(2, type.ExtensionBlocks.Count, because: "both blocks are reported", fix: "Build TypeDeclaration.ExtensionBlocks");

            var first = type.ExtensionBlocks[0];

            Assert.That.IsTrue(first.Receiver.Name == "value" && first.Receiver.TypeReference.Is("string") && first.HasReceiverName,
                               because: "the receiver is what the members extend",
                               fix: "Take the receiver from the ParameterList of the block");

            Assert.That.AreEqual("Shop.StringExtensions.Shout",
                                 first.Methods.Single().FullyQualifiedName,
                                 because: "the compiler emits an extension member on the static class",
                                 fix: "Leave the nameless block out of the qualified name");

            Assert.That.AreEqual("T",
                                 type.ExtensionBlocks[1].TypeParameters.Single().Name,
                                 because: "a generic block declares its type parameters",
                                 fix: "Read the TypeParameterList of the block");

            Assert.That.AreEqual<string>(["Shout", "Empty"],
                                         tree.Methods.Select(m => m.Name),
                                         because: "a rule over all methods must see the extension members",
                                         fix: "Include ExtensionBlocks in QueryExtensions.MethodsOf, which CSharpSyntaxTree.Methods builds on");

            // Member kind by member kind, as for any type: methods before property accessors.
            Assert.That.AreEqual<string>(["ToUpper", "IsNullOrWhiteSpace"],
                                         tree.Invocations.Select(i => i.Name),
                                         because: "the calls in extension members count",
                                         fix: "Include ExtensionBlocks in QueryExtensions.AllMembersWithBody(TypeDeclaration)");
        }

        [TestMethod]
        public void Extension_Members_Are_Flagged_Either_Way()
        {
            var tree = ParseCode("""
                public static class Classic
                {
                    public static string Shout(this string value) => value.ToUpper();
                    public static string Plain(string value) => value;
                }

                public static class Modern
                {
                    extension(string value)
                    {
                        public string Whisper() => value.ToLower();
                        public bool IsBlank => value.Length == 0;
                    }
                }

                public class Ordinary
                {
                    public void Go() { }
                }
                """);

            var shout = tree.Methods.Named("Shout").Single();
            var whisper = tree.Methods.Named("Whisper").Single();
            var isBlank = tree.Properties.Named("IsBlank").Single();

            Assert.That.IsTrue(shout is { IsExtension: true, IsInExtensionBlock: false, ExtensionReceiver: { Name: "value", IsThis: true } },
                               because: "this string value makes a classic extension method",
                               fix: "Map the this keyword to Modifier.This and set ExtensionReceiver in ToMethod");

            Assert.That.IsTrue(whisper is { IsExtension: true, IsInExtensionBlock: true, ExtensionReceiver.Name: "value" } && isBlank.IsExtension,
                               because: "a member of an extension block extends the receiver of the block",
                               fix: "Set ExtensionReceiver on the members in ToExtensionBlocks");

            Assert.That.IsFalse(tree.Methods.Named("Plain").Single().IsExtension || tree.Methods.Named("Go").Single().IsExtension,
                                because: "a static method without this extends nothing",
                                fix: "Only a first parameter with this makes an extension method");

            Assert.That.AreEqual<bool>([true, true, false],
                                       tree.Classes.Select(c => c.HasExtensionMembers),
                                       because: "a rule may ask which classes hold extensions",
                                       fix: "Check TypeDeclaration.HasExtensionMembers");
        }
    }
}
