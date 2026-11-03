using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// Every state a rule asks about is a property, on every declaration that can carry it. They used to
    /// be a mix of properties and extension methods, and <c>field.IsStatic()</c> did not compile because
    /// <c>Field</c> had a property of that name while <c>method.IsStatic()</c> needed the parentheses.
    /// </summary>
    [TestClass]
    public class StatePropertyTest
    {
        private const string BaseFix = "Check the state properties on DeclarationWithModifiers";

        private const string Code = """
            using System.Threading.Tasks;

            namespace Shop;

            public abstract class Base
            {
                public virtual void Open() { }

                protected abstract void Close();

                protected internal int Both;

                private protected int Narrow;

                int _implicit;

                public static extern void Native();

                public unsafe void Raw() { }
            }

            public sealed class Derived : Base
            {
                public sealed override void Open() { }

                protected override void Close() { }

                public new int Both;

                public async Task SaveAsync() { await Task.Yield(); }

                public void Fire() { }

                public Task<int> LoadAsync() => Task.FromResult(1);

                public void Locals()
                {
                    void Local() { }
                    static ValueTask LocalAsync() => default;
                }
            }

            public interface IStore
            {
                void Save();
            }

            public readonly struct Money
            {
                public readonly int Amount;

                public readonly override string ToString() => "";
            }

            public ref struct Span { }

            file class Hidden { }

            public static partial class Helpers
            {
                public static volatile int Counter;

                public static void Pass(ref int a, ref readonly int b, out int c, in int d, scoped ref int e) { c = 0; }
            }

            public delegate void Handler();

            public delegate Task AsyncHandler();
            """;

        private static CSharpSyntaxTree Tree { get; } = ParseCode(Code);

        private static TypeDeclaration Type(string name) => Tree.AllTypes().Single(t => t.Name == name);

        private static Method Method(string type, string name) => Type(type).Methods.Single(m => m.Name == name);

        private static Field Field(string type, string name) => Type(type).Fields.Single(f => f.Name == name);

        [TestMethod]
        public void Accessibility_Is_A_Property_On_Every_Declaration()
        {
            Assert.That.IsTrue(Type("Base").IsPublic && Method("Base", "Open").IsPublic,
                               because: "public is written on the type and on the method",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("Base", "Close").IsProtected && !Method("Base", "Close").IsProtectedInternal,
                               because: "IsProtected means exactly protected, not one of the combined forms",
                               fix: BaseFix);

            Assert.That.IsTrue(Field("Base", "Both").IsProtectedInternal && !Field("Base", "Both").IsProtected,
                               because: "protected internal has a check of its own",
                               fix: BaseFix);

            Assert.That.IsTrue(Field("Base", "Narrow").IsPrivateProtected,
                               because: "private protected has a check of its own",
                               fix: BaseFix);

            Assert.That.IsTrue(Field("Base", "_implicit").IsPrivate,
                               because: "a class member without a modifier is private by default",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("IStore", "Save").IsPublic,
                               because: "an interface member without a modifier is public by default",
                               fix: BaseFix);

            Assert.That.IsTrue(Type("Hidden").IsInternal,
                               because: "a type in a namespace without a modifier is internal by default",
                               fix: BaseFix);
        }

        [TestMethod]
        public void Modifiers_Are_Properties_On_Every_Declaration()
        {
            Assert.That.IsTrue(Type("Base").IsAbstract && Method("Base", "Close").IsAbstract,
                               because: "abstract is written on the type and on the method",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("Base", "Open").IsVirtual && !Method("Base", "Open").IsOverride,
                               because: "virtual and override are separate modifiers",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("Derived", "Open").IsOverride && Method("Derived", "Open").IsSealed,
                               because: "a sealed override carries both modifiers",
                               fix: BaseFix);

            Assert.That.IsTrue(Type("Derived").IsSealed && !Type("Base").IsSealed,
                               because: "sealed is read from the modifier list",
                               fix: BaseFix);

            Assert.That.IsTrue(Field("Derived", "Both").IsNew,
                               because: "new hides the inherited field",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("Base", "Native").IsExtern && Method("Base", "Native").IsStatic,
                               because: "static extern carries both modifiers",
                               fix: BaseFix);

            Assert.That.IsTrue(Method("Base", "Raw").IsUnsafe,
                               because: "unsafe is written on the method",
                               fix: BaseFix);

            Assert.That.IsTrue(Type("Helpers").IsStatic && Type("Helpers").IsPartial,
                               because: "static and partial are written on the type",
                               fix: BaseFix);

            Assert.That.IsFalse(Method("IStore", "Save").IsAbstract,
                                because: "the modifier checks read what is written, and an interface member writes no abstract",
                                fix: BaseFix);
        }

        [TestMethod]
        public void Field_States()
        {
            Assert.That.IsTrue(Field("Helpers", "Counter").IsVolatile && Field("Helpers", "Counter").IsStatic,
                               because: "static volatile carries both modifiers",
                               fix: "Check Field.IsVolatile");

            Assert.That.IsTrue(Field("Money", "Amount").IsReadOnly,
                               because: "a readonly field reads the modifier",
                               fix: "Check Field.IsReadOnly");
        }

        [TestMethod]
        public void Type_States()
        {
            Assert.That.IsTrue(Type("Money").IsReadOnly && !Type("Base").IsReadOnly,
                               because: "a readonly struct carries the modifier, a class does not",
                               fix: "Check TypeDeclaration.IsReadOnly");

            Assert.That.IsTrue(((Struct)Type("Span")).IsRefStruct && !((Struct)Type("Money")).IsRefStruct,
                               because: "a ref struct carries the ref modifier",
                               fix: "Check Struct.IsRefStruct");

            Assert.That.IsTrue(Type("Hidden").IsFile,
                               because: "a file-local type is written file class",
                               fix: "Check TypeDeclaration.IsFile");
        }

        [TestMethod]
        public void Method_States()
        {
            Assert.That.IsTrue(Method("Money", "ToString").IsReadOnly,
                               because: "a readonly struct member carries the modifier",
                               fix: "Check Method.IsReadOnly");

            Assert.That.IsTrue(Method("Derived", "Fire").ReturnsVoid && !Method("Derived", "Fire").ReturnsTask,
                               because: "void is not awaitable",
                               fix: "Check Method.ReturnsVoid");

            Assert.That.IsTrue(Method("Derived", "SaveAsync").ReturnsTask && Method("Derived", "SaveAsync").IsAsync,
                               because: "an async Task method is both",
                               fix: "Check Method.ReturnsTask");

            Assert.That.IsTrue(Method("Derived", "LoadAsync").ReturnsTask && !Method("Derived", "LoadAsync").IsAsync,
                               because: "ReturnsTask reads the return type, IsAsync the modifier; a Task<int> without async is only the first",
                               fix: "Check Method.ReturnsTask");
        }

        [TestMethod]
        public void Local_Function_And_Delegate_States()
        {
            var locals = Method("Derived", "Locals").LocalFunctions;

            Assert.That.IsTrue(locals.Single(l => l.Name == "Local").ReturnsVoid,
                               because: "the local function returns void",
                               fix: "Check LocalFunction.ReturnsVoid");

            Assert.That.IsTrue(locals.Single(l => l.Name == "LocalAsync").ReturnsTask && locals.Single(l => l.Name == "LocalAsync").IsStatic,
                               because: "a static local function returning ValueTask is both",
                               fix: "Check LocalFunction.ReturnsTask");

            Assert.That.IsTrue(Tree.Delegates.Single(d => d.Name == "Handler").ReturnsVoid,
                               because: "the delegate returns void",
                               fix: "Check Delegate.ReturnsVoid");

            Assert.That.IsTrue(Tree.Delegates.Single(d => d.Name == "AsyncHandler").ReturnsTask,
                               because: "the delegate returns Task",
                               fix: "Check Delegate.ReturnsTask");
        }

        [TestMethod]
        public void Parameter_States()
        {
            var parameters = Method("Helpers", "Pass").Parameters;

            Assert.That.IsTrue(parameters[0].IsRef && !parameters[0].IsRefReadOnly,
                               because: "ref is not ref readonly",
                               fix: "Check Parameter.IsRef");

            Assert.That.IsTrue(parameters[1].IsRef && parameters[1].IsRefReadOnly,
                               because: "ref readonly is a ref as well",
                               fix: "Check Parameter.IsRefReadOnly");

            Assert.That.IsTrue(parameters[2].IsOut,
                               because: "out is written on the parameter",
                               fix: "Check Parameter.IsOut");

            Assert.That.IsTrue(parameters[3].IsIn,
                               because: "in is written on the parameter",
                               fix: "Check Parameter.IsIn");

            Assert.That.IsTrue(parameters[4].IsScoped && parameters[4].IsRef,
                               because: "scoped ref carries both",
                               fix: "Check Parameter.IsScoped");
        }
    }
}
