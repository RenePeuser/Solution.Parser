using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// Each declaration kind used to translate modifiers with its own copy of the same switch, so a
    /// keyword missing from one copy was silently dropped for that kind only.
    /// </summary>
    [TestClass]
    public class ModifierAndAccessibilityTest
    {
        private const string SharedMapperFix =
            "Check SyntaxTokenListExtensions.ToModifiers, it is the single mapper every declaration kind shares";

        [TestMethod]
        public void Sealed_Is_Reported_On_A_Class()
        {
            var holder = ParseCode("public sealed class Holder { }").Classes.Single();

            Assert.That.Contains(holder.Modifiers,
                                 Modifier.Sealed,
                                 because: "sealed is central to rules about extensibility and used to be dropped entirely",
                                 fix: SharedMapperFix);

            Assert.That.IsTrue(holder.IsSealed(),
                               because: "the predicate must agree with the modifier list it reads",
                               fix: "Check QueryExtensions.IsSealed");
        }

        [TestMethod]
        public void Method_Modifiers_Are_Complete()
        {
            var holder = ParseCode("""
                using System.Threading.Tasks;

                public abstract class Holder
                {
                    public virtual void Virtual() { }

                    public override string ToString() => "x";

                    protected internal async Task Both() { await Task.Yield(); }

                    public new void Hidden() { }

                    public abstract void Abstract();
                }
                """).Classes.Single();

            Assert.That.Contains(holder.Methods.Single(m => m.Name == "Virtual").Modifiers,
                                 Modifier.Virtual,
                                 because: "virtual decides whether a method can be overridden, which several rules depend on",
                                 fix: SharedMapperFix);

            Assert.That.Contains(holder.Methods.Single(m => m.Name == "ToString").Modifiers,
                                 Modifier.Override,
                                 because: "a rule that skips overridden members has to recognise them",
                                 fix: SharedMapperFix);

            Assert.That.Contains(holder.Methods.Single(m => m.Name == "Both").Modifiers,
                                 Modifier.Async,
                                 because: "async naming rules read the modifier rather than guessing from the return type",
                                 fix: SharedMapperFix);

            Assert.That.Contains(holder.Methods.Single(m => m.Name == "Hidden").Modifiers,
                                 Modifier.New,
                                 because: "member hiding is worth a rule of its own and used to be invisible",
                                 fix: SharedMapperFix);

            Assert.That.Contains(holder.Methods.Single(m => m.Name == "Abstract").Modifiers,
                                 Modifier.Abstract,
                                 because: "an abstract member has no body, so rules about bodies have to skip it",
                                 fix: SharedMapperFix);
        }

        [TestMethod]
        public void Readonly_Is_Reported_On_A_Struct_As_Well_As_On_A_Field()
        {
            var point = ParseCode("public readonly struct Point { private readonly int _x; }").Structs.Single();

            Assert.That.Contains(point.Modifiers,
                                 Modifier.ReadOnly,
                                 because: "readonly used to be known to the field mapper only, so a readonly struct looked mutable",
                                 fix: SharedMapperFix);

            Assert.That.Contains(point.Fields.Single().Modifiers,
                                 Modifier.ReadOnly,
                                 because: "the field keeps its readonly modifier as well",
                                 fix: SharedMapperFix);
        }

        [TestMethod]
        public void Combined_Accessibility_Keywords_Are_Resolved()
        {
            var holder = ParseCode("""
                public class Holder
                {
                    protected internal int A { get; set; }
                    private protected int B { get; set; }
                    public int C { get; set; }
                    internal int D { get; set; }
                }
                """).Classes.Single();

            Assert.That.AreEqual(Accessibility.ProtectedOrInternal,
                                 holder.Properties.Single(p => p.Name == "A").Accessibility,
                                 because: "protected internal widens access and must not be confused with protected",
                                 fix: "Resolve the keyword pair before the single keywords in ToAccessibility");

            Assert.That.AreEqual(Accessibility.ProtectedAndInternal,
                                 holder.Properties.Single(p => p.Name == "B").Accessibility,
                                 because: "private protected narrows access and is the opposite of protected internal",
                                 fix: "Resolve the keyword pair before the single keywords in ToAccessibility");

            Assert.That.AreEqual(Accessibility.Public,
                                 holder.Properties.Single(p => p.Name == "C").Accessibility,
                                 because: "public is the accessibility most rules filter on",
                                 fix: "Check ToAccessibility handles the plain keywords");

            Assert.That.AreEqual(Accessibility.Internal,
                                 holder.Properties.Single(p => p.Name == "D").Accessibility,
                                 because: "internal members are not part of the public surface a rule guards",
                                 fix: "Check ToAccessibility handles the plain keywords");
        }

        /// <summary>
        /// Without a default, an absent modifier is indistinguishable from private, which is exactly what
        /// a rule such as "no public fields" has to tell apart.
        /// </summary>
        [TestMethod]
        public void Missing_Accessibility_Falls_Back_To_The_Language_Default()
        {
            var parsed = ParseCode("""
                class TopLevel
                {
                    int _field;

                    void Method() { }
                }

                interface IThing
                {
                    void Go();
                }
                """);

            var topLevel = parsed.Classes.Single();

            Assert.That.AreEqual(Accessibility.Internal,
                                 topLevel.Accessibility,
                                 because: "a type declared in a namespace without a modifier is internal",
                                 fix: "Resolve the default from the parent node in ToAccessibility");

            Assert.That.AreEqual(Accessibility.Private,
                                 topLevel.Fields.Single().Accessibility,
                                 because: "a class member without a modifier is private",
                                 fix: "Resolve the default from the parent node in ToAccessibility");

            Assert.That.AreEqual(Accessibility.Private,
                                 topLevel.Methods.Single().Accessibility,
                                 because: "a class member without a modifier is private",
                                 fix: "Resolve the default from the parent node in ToAccessibility");

            Assert.That.AreEqual(Accessibility.Public,
                                 parsed.Interfaces.Single().Methods.Single().Accessibility,
                                 because: "an interface member is public even though it carries no modifier, which a public surface rule must see",
                                 fix: "Resolve the default from the parent node in ToAccessibility");
        }
    }
}
