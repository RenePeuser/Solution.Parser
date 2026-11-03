using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Core;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// The shortcuts a rule reads: one assertion per public member that has logic of its own. Each of
    /// them was never called by any test, so a wrong one would only show in a rule that misses something.
    /// </summary>
    [TestClass]
    public class PublicApiTest
    {
        private const string Sample = """
            namespace Shop;

            public interface IStore { }
            public struct Money { }
            public enum Color { Red }

            public delegate T Factory<T>();

            public partial class Cart : IStore
            {
                static Cart() { }

                public static int Count;
                public int Size;

                public required string Name { get; set; }
                public int Total { get; }
                public int this[int i] { get => i; }
                string IStore.Label { get; }

                /// <summary>Saves the cart.</summary>
                [Obsolete("use SaveAsync")]
                public void Save() { }

                /// <inheritdoc />
                public T Convert<T>() => default!;

                void IStore.Clear() { }

                public async Task<int> LoadAsync() => await Task.FromResult(1);

                public Task<int> Load() => Task.FromResult(1);

                public class Line { }
                public struct Position { }
                public record Entry;
                public interface IView { }
                public enum State { Open }
            }
            """;

        private static readonly CSharpSyntaxTree Tree = ParseCode(Sample);

        private static readonly Class Cart = Tree.Classes.Single(c => c.Name == "Cart");

        private static Method Method(string name)
        {
            return Cart.Methods.Single(m => m.Name == name);
        }

        [TestMethod]
        public void Every_Type_Kind_Is_Reported()
        {
            Assert.That.AreEqual<TypeKind>([TypeKind.Interface, TypeKind.Struct, TypeKind.Enum, TypeKind.Class],
                                           Tree.Types.Select(t => t.Kind),
                                           because: "Kind tells the declarations apart without a type check",
                                           fix: "Override Kind on every TypeDeclaration record");

            Assert.That.IsTrue(Cart.NestedClasses.Count == 1 && Cart.NestedStructs.Count == 1 && Cart.NestedRecords.Count == 1 &&
                               Cart.NestedInterfaces.Count == 1 && Cart.NestedEnums.Count == 1,
                               because: "each nested shortcut filters NestedTypes by its kind",
                               fix: "Check the Nested* shortcuts on TypeDeclaration");

            Assert.That.IsTrue(Tree.Delegates.Single().IsGeneric,
                               because: "Factory<T> declares a type parameter",
                               fix: "Check Delegate.IsGeneric");
        }

        [TestMethod]
        public void Member_Shortcuts_Read_The_Declaration()
        {
            Assert.That.IsTrue(Cart.Constructors.Single().IsStatic, because: "static Cart() is a static constructor", fix: "Check DeclarationWithModifiers.IsStatic");
            Assert.That.IsTrue(Cart.Fields.Single(f => f.Name == "Count").IsStatic && !Cart.Fields.Single(f => f.Name == "Size").IsStatic,
                               because: "only Count is static",
                               fix: "Check DeclarationWithModifiers.IsStatic");

            var name = Cart.Properties.Single(p => p.Name == "Name");
            var total = Cart.Properties.Single(p => p.Name == "Total");
            var label = Cart.Properties.Single(p => p.Name == "Label");

            Assert.That.IsTrue(name.IsRequired && name.HasGetter && name.HasSetter, because: "Name is required with get and set", fix: "Check the Property shortcuts");
            Assert.That.IsTrue(total.HasGetter && !total.HasSetter, because: "Total only has a getter", fix: "Check Property.HasSetter");
            Assert.That.IsTrue(label.IsExplicitInterfaceImplementation && !total.IsExplicitInterfaceImplementation,
                               because: "IStore.Label implements the interface explicitly",
                               fix: "Check Property.IsExplicitInterfaceImplementation");

            Assert.That.IsTrue(Cart.Indexers.Single().IsReadOnly, because: "the indexer has no setter", fix: "Check Indexer.IsReadOnly");

            Assert.That.IsTrue(Method("Convert").IsGeneric && !Method("Save").IsGeneric, because: "only Convert<T> is generic", fix: "Check Method.IsGeneric");
            Assert.That.IsTrue(Method("Clear").IsExplicitInterfaceImplementation, because: "IStore.Clear is explicit", fix: "Check Method.IsExplicitInterfaceImplementation");

            Assert.That.IsTrue(Method("Save").MethodValue == Method("Save").SourceCode && Method("Save").MethodBody == "{ }",
                               because: "the previous names still read the same values",
                               fix: "Keep MethodValue and MethodBody as aliases");
        }

        [TestMethod]
        public void Documentation_And_Attributes_Are_Read()
        {
            Assert.That.IsTrue(Method("Save").Documentation.HasSummary && !Method("Save").Documentation.IsInheritDoc,
                               because: "Save has a summary of its own",
                               fix: "Check DocumentationComment.HasSummary");

            Assert.That.IsTrue(Method("Convert").Documentation.IsInheritDoc && !Method("Convert").Documentation.HasSummary,
                               because: "<inheritdoc /> documents by reference, without a summary",
                               fix: "Check DocumentationComment.IsInheritDoc");

            Assert.That.AreEqual("use SaveAsync",
                                 Method("Save").Attribute("Obsolete")?.PositionalExpressions.Single().StringValue,
                                 because: "Attribute finds the one attribute by its short name",
                                 fix: "Check QueryExtensions.Attribute");

            Assert.That.IsNull(Method("Convert").Attribute("Obsolete"), because: "Convert has no attribute", fix: "Check QueryExtensions.Attribute");
        }

        [TestMethod]
        public void Async_Helpers_Agree_With_The_Declaration()
        {
            Assert.That.IsTrue(Method("LoadAsync").IsAsync && !Method("Load").IsAsync,
                               because: "only LoadAsync carries the async modifier",
                               fix: "Check MemberWithBody.IsAsync");

            Assert.That.IsTrue(Method("Load").ReturnType.IsTask && Method("LoadAsync").ReturnType.IsTask && !Method("Save").ReturnType.IsTask,
                               because: "whether a method returns something awaitable is a question about its return type",
                               fix: "Check TypeReference.IsTask");

            var local = ParseCode("""
                public class Holder
                {
                    public void Go()
                    {
                        async Task<int> FetchAsync() => await Task.FromResult(1);
                    }
                }
                """).Classes.Single().Methods.Single().LocalFunctions.Single();

            Assert.That.IsTrue(local.IsAsync && local.ReturnType.IsTask &&
                               local.LocalFunctionBody == local.Body && local.LocalFunctionValue == local.SourceCode,
                               because: "a local function answers the same questions as a method, and keeps its aliases",
                               fix: "Check LocalFunction");
        }

        [TestMethod]
        public void Body_Shortcuts_Read_The_Statements()
        {
            var method = ParseCode("""
                public class Holder
                {
                    public IEnumerable<int> Go(int x)
                    {
                        if (x > 0) { } else { }
                        if (x < 0) { }
                        try { } catch (IOException ex) when (ex.HResult == 1) { throw; }
                        Action log = () => Write(x);
                        app.MapGet("/").WithName("root");
                        yield break;
                    }
                }
                """).Classes.Single().Methods.Single();

            Assert.That.AreEqual<bool>([true, false],
                                       method.IfStatements.Select(i => i.HasElse),
                                       because: "only the first if has an else",
                                       fix: "Check MemberWithBody.IfStatements and IfStatement.HasElse");

            Assert.That.IsTrue(method.TryStatements.Single().Catches.Single().HasFilter, because: "the catch has a when", fix: "Check CatchClause.HasFilter");

            Assert.That.IsTrue(method.Throws.Single().IsRethrow, because: "throw; rethrows", fix: "Check ThrowStatement.IsRethrow");

            Assert.That.IsTrue(method.AllStatements.OfType<YieldStatement>().Single().IsBreak, because: "yield break ends the iterator", fix: "Check YieldStatement.IsBreak");

            Assert.That.AreEqual("Write(x)", method.Lambdas.Single().Body, because: "an expression lambda's body is its expression", fix: "Check Lambda.Body");

            var chain = method.InvocationChains.Single(c => c.Contains("MapGet"));

            Assert.That.IsTrue(chain.Last.Name == "WithName" && !chain.Contains("Map"),
                               because: "Contains matches whole names, and Last is the end of the chain",
                               fix: "Check InvocationChain.Contains and Last");
        }

        [TestMethod]
        public void Expression_Shortcuts_Read_The_Tree()
        {
            var value = ParseCode("""
                public class Holder { private object _x = Wrap(1, "a"); }
                """).Classes.Single().Fields.Single().InitializerExpression!;

            Assert.That.IsTrue(!value.IsLiteral && value.Arguments.All(a => a.IsLiteral),
                               because: "a call is no literal, its arguments are",
                               fix: "Check Expression.IsLiteral");

            Assert.That.AreEqual<string>(["1", "\"a\""],
                                         value.Descendants().Select(d => d.SourceCode),
                                         because: "Descendants leaves out the expression itself",
                                         fix: "Check Expression.Descendants");
        }

        /// <summary>Name.Replace(Extension, "") removed every occurrence of the extension, not just the last.</summary>
        [TestMethod]
        [DataRow("report.cs", "report")]
        [DataRow("report.cs.backup.cs", "report.cs.backup")]
        [DataRow("My.Project.csproj", "My.Project")]
        [DataRow("README", "README")]
        public void The_File_Name_Without_Extension_Drops_Only_The_Last_Extension(string fileName, string expected)
        {
            var file = new FileInfo(Path.Combine(Path.GetTempPath(), fileName));

            Assert.That.AreEqual(expected,
                                 file.FileNameWithoutExtension(),
                                 because: "only the last extension is the extension",
                                 fix: "Use Path.GetFileNameWithoutExtension in FileSystemInfoExtensions");

            Assert.That.IsTrue(file.NotExists(), because: "the file was never written", fix: "Check FileSystemInfoExtensions.NotExists");
        }
    }
}
