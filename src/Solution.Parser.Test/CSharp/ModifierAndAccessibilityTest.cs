using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using static Solution.Parser.Test.CSharp.ParseHelper;

namespace Solution.Parser.Test.CSharp
{
    /// <summary>
    /// Each declaration kind used to translate modifiers with its own copy of the same switch, so a
    /// keyword missing from one copy was silently dropped for that kind only.
    /// </summary>
    [TestClass]
    public class ModifierAndAccessibilityTest
    {
        [TestMethod]
        public void Sealed_Is_Reported_On_A_Class()
        {
            var holder = ParseCode("public sealed class Holder { }").Classes.Single();

            CollectionAssert.Contains(holder.Modifiers.ToArray(), Modifier.Sealed);
            Assert.IsTrue(holder.IsSealed());
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

            CollectionAssert.Contains(holder.Methods.Single(m => m.Name == "Virtual").Modifiers.ToArray(), Modifier.Virtual);
            CollectionAssert.Contains(holder.Methods.Single(m => m.Name == "ToString").Modifiers.ToArray(), Modifier.Override);
            CollectionAssert.Contains(holder.Methods.Single(m => m.Name == "Both").Modifiers.ToArray(), Modifier.Async);
            CollectionAssert.Contains(holder.Methods.Single(m => m.Name == "Hidden").Modifiers.ToArray(), Modifier.New);
            CollectionAssert.Contains(holder.Methods.Single(m => m.Name == "Abstract").Modifiers.ToArray(), Modifier.Abstract);
        }

        [TestMethod]
        public void Readonly_Is_Reported_On_A_Struct_As_Well_As_On_A_Field()
        {
            var parsed = ParseCode("public readonly struct Point { private readonly int _x; }");
            var point = parsed.Structs.Single();

            CollectionAssert.Contains(point.Modifiers.ToArray(), Modifier.ReadOnly);
            CollectionAssert.Contains(point.Fields.Single().Modifiers.ToArray(), Modifier.ReadOnly);
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

            Assert.AreEqual(Accessibility.ProtectedOrInternal, holder.Properties.Single(p => p.Name == "A").Accessibility);
            Assert.AreEqual(Accessibility.ProtectedAndInternal, holder.Properties.Single(p => p.Name == "B").Accessibility);
            Assert.AreEqual(Accessibility.Public, holder.Properties.Single(p => p.Name == "C").Accessibility);
            Assert.AreEqual(Accessibility.Internal, holder.Properties.Single(p => p.Name == "D").Accessibility);
        }

        /// <summary>
        /// Without a default an absent modifier is indistinguishable from private, which is exactly what
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

            Assert.AreEqual(Accessibility.Internal, topLevel.Accessibility, "A type declared in a namespace is internal.");
            Assert.AreEqual(Accessibility.Private, topLevel.Fields.Single().Accessibility);
            Assert.AreEqual(Accessibility.Private, topLevel.Methods.Single().Accessibility);
            Assert.AreEqual(Accessibility.Public, parsed.Interfaces.Single().Methods.Single().Accessibility, "An interface member is public.");
        }
    }
}
