using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Solution.Parser.CSharp.Test.ParseHelper;

namespace Solution.Parser.CSharp.Test
{
    /// <summary>
    /// <c>#pragma warning</c> is trivia, so Roslyn keeps it out of every declaration. The tree reports it,
    /// so a rule can ask for a reason on every suppression or forbid suppressing a given analyzer.
    /// </summary>
    [TestClass]
    public class PragmaWarningTest
    {
        private static readonly CSharpSyntaxTree Tree = ParseCode("""
            #pragma warning disable CA2007, IDE0051 // DateTime.Now is the subject of this test
            namespace Shop;

            public class Cart
            {
            #pragma warning disable 618
                public void Save()
                {
            #pragma warning disable // legacy code
                    var x = 1;
            #pragma warning restore
                }
            #pragma warning restore 618
            }
            #pragma warning restore CA2007, IDE0051
            """);

        [TestMethod]
        public void Every_Pragma_Of_The_File_Is_Reported_In_Source_Order()
        {
            Assert.That.AreEqual<PragmaWarningAction>([PragmaWarningAction.Disable, PragmaWarningAction.Disable, PragmaWarningAction.Disable,
                                                       PragmaWarningAction.Restore, PragmaWarningAction.Restore, PragmaWarningAction.Restore],
                                                      Tree.PragmaWarnings.Select(p => p.Action),
                                                      because: "a pragma inside a type or a method body suppresses as much as one at file level",
                                                      fix: "Collect every PragmaWarningDirectiveTriviaSyntax with root.GetDirectives()");

            Assert.That.AreEqual<int>([1, 6, 9, 11, 13, 15],
                                      Tree.PragmaWarnings.Select(p => p.Location.StartLine),
                                      because: "a finding has to point at the pragma",
                                      fix: "Take the location from the directive node");
        }

        [TestMethod]
        public void Ids_Are_Read_As_The_Compiler_Reads_Them()
        {
            Assert.That.AreEqual<string>(["CA2007", "IDE0051"],
                                         Tree.PragmaWarnings[0].Ids,
                                         because: "a rule forbids suppressing an analyzer by its id",
                                         fix: "Project ErrorCodes onto Ids");

            Assert.That.AreEqual<string>(["CS0618"],
                                         Tree.PragmaWarnings[1].Ids,
                                         because: "618 is CS0618 to the compiler, and a rule about CS0618 must not miss it",
                                         fix: "Turn a numeric error code into CS plus four digits");

            Assert.That.IsTrue(Tree.PragmaWarnings[2].AppliesToAllWarnings && !Tree.PragmaWarnings[0].AppliesToAllWarnings,
                               because: "a pragma without ids switches off every warning",
                               fix: "AppliesToAllWarnings is Ids.IsEmpty");
        }

        [TestMethod]
        public void The_Comment_On_The_Line_Is_The_Reason()
        {
            Assert.That.AreEqual<string?>(["DateTime.Now is the subject of this test", null, "legacy code"],
                                          Tree.PragmaWarnings.Take(3).Select(p => p.Comment),
                                          because: "a rule asks every suppression for a reason, and the reason is the comment behind it",
                                          fix: "Read the comment trivia of the directive and drop the //");
        }

        [TestMethod]
        public void Many_Files_Read_Like_One()
        {
            var other = ParseCode("#pragma warning disable CA1822 // static on purpose");

            Assert.That.AreEqual<string>(["CA2007", "IDE0051", "CS0618", "CS0618", "CA2007", "IDE0051", "CA1822"],
                                         new[] { Tree, other }.PragmaWarnings.SelectMany(p => p.Ids),
                                         because: "a rule asks the whole production code, not one file",
                                         fix: "Add PragmaWarnings to SyntaxTreeListExtensions");
        }
    }
}
