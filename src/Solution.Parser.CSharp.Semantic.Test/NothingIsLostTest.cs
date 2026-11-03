using System.Collections.Generic;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.CSharp;
using Solution.Parser.Sln;
using RoslynTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree;

namespace Solution.Parser.CSharp.Semantic.Test
{
    /// <summary>
    /// The lists a rule starts from, <c>trees.Classes</c>, <c>trees.Methods</c> and the like, are counted
    /// against Roslyn itself, file by file, over every file of this repository. The lists are built out
    /// of each other, so comparing one with another proves nothing; Roslyn is the count that does not
    /// depend on the parser, and a refactoring that drops a nested type or the members of an extension
    /// block shows up here as the file it happened in.
    /// </summary>
    [TestClass]
    public class NothingIsLostTest
    {
        private static readonly IReadOnlyList<CSharpSyntaxTree> Trees = SolutionFile.Find("Solution.Parser.sln").AllTrees;

        [TestMethod]
        public void The_Repository_Is_A_Real_Sample()
        {
            Assert.That.IsTrue(Trees.Count > 100 && Trees.Methods.Count > 500,
                               because: "the counts below only mean something over a code base of real size",
                               fix: "Check that SolutionFile.Find found Solution.Parser.sln with its projects");
        }

        [TestMethod]
        public void Every_Type_Is_Found()
        {
            AreAllFound("types", Trees, tree => tree.Classes.Count + tree.Records.Count + tree.Structs.Count + tree.Interfaces.Count + tree.Enums.Count,
                        root => root.DescendantNodes().Count(n => n is BaseTypeDeclarationSyntax and not ExtensionBlockDeclarationSyntax));

            AreAllFound("classes", Trees, tree => tree.Classes.Count, root => root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count());
            AreAllFound("records", Trees, tree => tree.Records.Count, root => root.DescendantNodes().OfType<RecordDeclarationSyntax>().Count());
            AreAllFound("structs", Trees, tree => tree.Structs.Count, root => root.DescendantNodes().OfType<StructDeclarationSyntax>().Count());
            AreAllFound("interfaces", Trees, tree => tree.Interfaces.Count, root => root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().Count());
            AreAllFound("enums", Trees, tree => tree.Enums.Count, root => root.DescendantNodes().OfType<EnumDeclarationSyntax>().Count());
        }

        [TestMethod]
        public void Every_Member_Is_Found()
        {
            AreAllFound("methods", Trees, tree => tree.Methods.Count, root => root.DescendantNodes().OfType<MethodDeclarationSyntax>().Count());

            AreAllFound("fields", Trees, tree => tree.Fields.Count,
                        root => root.DescendantNodes().OfType<FieldDeclarationSyntax>().Sum(f => f.Declaration.Variables.Count));

            // A primary constructor is a constructor too; the receiver of an extension block is not one.
            AreAllFound("constructors", Trees, tree => tree.Constructors.Count,
                        root => root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Count() +
                                root.DescendantNodes().OfType<TypeDeclarationSyntax>().Count(t => t.ParameterList is not null && t is not ExtensionBlockDeclarationSyntax));
        }

        [TestMethod]
        public void Many_Files_Hold_What_Each_File_Holds()
        {
            Assert.That.IsTrue(Trees.Classes.Count == Trees.Sum(t => t.Classes.Count) &&
                               Trees.Methods.Count == Trees.Sum(t => t.Methods.Count) &&
                               Trees.Invocations.Count == Trees.Sum(t => t.Invocations.Count) &&
                               Trees.MembersWithBody.Count == Trees.Sum(t => t.MembersWithBody.Count),
                               because: "the lists of many files are the lists of each file, joined, without loss or duplicates",
                               fix: "Check TreeListViews in SyntaxTreeListExtensions");
        }

        [TestMethod]
        public void The_Solution_Hands_Out_The_Same_Lists()
        {
            var solution = SolutionFile.Find("Solution.Parser.sln");
            var project = solution.Project("Solution.Parser.CSharp");

            Assert.That.IsTrue(ReferenceEquals(solution.ProductionTrees, solution.ProductionTrees) &&
                               ReferenceEquals(solution.AllTrees, solution.AllTrees) &&
                               ReferenceEquals(project.Trees, project.Trees) &&
                               ReferenceEquals(solution.ProductionTrees.Methods, solution.ProductionTrees.Methods),
                               because: "a rule writes solution.ProductionTrees.Methods in every query, and each must not build the list anew",
                               fix: "Keep the tree lists in CodeBase, so SyntaxTreeListExtensions can remember what it built for them");
        }

        private static void AreAllFound(string what, IReadOnlyList<CSharpSyntaxTree> trees, System.Func<CSharpSyntaxTree, int> parsed, System.Func<Microsoft.CodeAnalysis.SyntaxNode, int> roslyn)
        {
            var differences = (from tree in trees
                               let expected = roslyn(RoslynTree.ParseText(tree.SourceCode).GetRoot())
                               let actual = parsed(tree)
                               where expected != actual
                               select $"{tree.FileName}: Roslyn {expected}, parser {actual}").ToList();

            Assert.That.HasCount(0,
                                 differences,
                                 because: $"every file must report exactly the {what} Roslyn finds in it, nested ones included",
                                 fix: $"Check how CSharpSyntaxTree collects its {what}: {string.Join("; ", differences.Take(5))}");
        }
    }
}
