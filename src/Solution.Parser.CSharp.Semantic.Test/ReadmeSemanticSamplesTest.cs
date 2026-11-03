using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Semantic.Test.SemanticHelper;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>The cookbook rules of the README that need symbols, word for word; see ReadmeSamplesTest for the others.</summary>
    [TestClass]
    public class ReadmeSemanticSamplesTest
    {
        /// <summary>The syntax sees Sleep(100) and cannot tell whose Sleep it is; the compiler can.</summary>
        [TestMethod]
        public void No_Thread_Sleep_However_It_Is_Written()
        {
            var code = WithSymbols("""
                using System.Threading;
                using static System.Threading.Thread;

                public class Poller
                {
                    public void Wait() => Thread.Sleep(100);
                    public void Nap() => Sleep(100);
                }

                public class Bed
                {
                    public void Sleep(string reason) { }
                    public void Rest() => Sleep("tired");
                }
                """);

            var findings = from call in code.AllTrees.Invocations
                           where call.Method?.FullyQualifiedName == "System.Threading.Thread.Sleep"
                           select $"{call.Location}: Thread.Sleep blocks a thread, await Task.Delay instead";

            Assert.That.AreEqual(2,
                                 findings.Count(),
                                 because: "both calls bind to Thread.Sleep, the one written through using static included, and the own Sleep(string) does not",
                                 fix: "Keep the README cookbook and this test in step");
        }
    }
}
