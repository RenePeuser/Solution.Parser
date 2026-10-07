using System;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// Two code bases over the same files never answer for each other: a call is resolved by the code
    /// base it came from, not by whichever one opened its file last.
    /// </summary>
    [TestClass]
    public class CodeBaseIsolationTest
    {
        private const string IsolationFix = "Check SemanticRegistry, a call must find the workspace that parsed it, not the last one that saw its file";

        [TestMethod]
        public void A_Call_Of_The_Fast_Mode_Throws_Even_When_Another_Code_Base_Has_Symbols_For_The_File()
        {
            var fileName = $"Shared_{Guid.NewGuid():N}.cs";
            const string Code = "public class A { void Go() => Run(); void Run() { } }";

            var fast = CodeBase.FromSources((fileName, Code));
            _ = CodeBase.FromSources((fileName, Code)).WithSymbols().AllTrees.AllInvocations().Single().Method;

            var call = fast.AllTrees.AllInvocations().Single();

            Assert.ThrowsExactly<InvalidOperationException>(() => _ = call.Method);
            Assert.That.AreEqual(0, fast.Workspace.CompilationCount, because: "the fast mode must not borrow symbols from another code base", fix: IsolationFix);
        }

        [TestMethod]
        public void Two_Code_Bases_With_The_Same_File_Resolve_Against_Their_Own_Code()
        {
            var fileName = $"Shared_{Guid.NewGuid():N}.cs";

            var first = CodeBase.FromSources((fileName, "public class A { void Go() => First(1); void First(int first) { } }")).WithSymbols();
            var second = CodeBase.FromSources((fileName, "public class A { void Go() => Second(true); void Second(bool second) { } }")).WithSymbols();

            var firstCall = first.AllTrees.AllInvocations().Single();
            var secondCall = second.AllTrees.AllInvocations().Single();

            Assert.That.AreEqual("First", firstCall.Method?.Name, because: "the first code base was opened first but still knows its own code", fix: IsolationFix);
            Assert.That.AreEqual("Second", secondCall.Method?.Name, because: "the second code base knows its own code", fix: IsolationFix);
        }
    }
}
