using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Project;
using Solution.Parser.Sln;

namespace Solution.Parser.Architecture.Test
{
    /// <summary>
    /// The package layering, checked with this library against this library's own solution.
    /// </summary>
    /// <remarks>
    /// The split only pays off while nobody adds a reference that quietly puts Roslyn or MSBuild back
    /// under a package that does not need them. A comment in a csproj cannot enforce that; this can.
    /// </remarks>
    [TestClass]
    public class LayeringTest
    {
        private const string LayeringFix =
            "Adding this reference undoes the package split. Put the code that needs it in a package "
            + "that already depends on it, or extend the file list instead of ProjectFile";

        private static SolutionFile _solution = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            var solutionFile = new SolutionFileName("Solution.Parser.sln").FindSolutionFileReverseFrom(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            _solution = solutionFile.Parse();
        }

        private static ProjectFile Project(string name)
        {
            return _solution.Projects.Single(p => p.ProjectFileInfo.FileNameWithoutExtenion == name);
        }

        private static ImmutableList<string> ProjectReferencesOf(string name)
        {
            return Project(name).ProjectReferences.Select(r => r.Name).Order(StringComparer.Ordinal).ToImmutableList();
        }

        private static ImmutableList<string> PackageReferencesOf(string name)
        {
            return Project(name).PackageReferences.Select(r => r.Include).Order(StringComparer.Ordinal).ToImmutableList();
        }

        [TestMethod]
        public void Core_Depends_On_Nothing_Of_Its_Own()
        {
            Assert.That.HasCount(0,
                                 ProjectReferencesOf("Solution.Parser.Core"),
                                 because: "Core is the bottom of the stack, everything else builds on it",
                                 fix: LayeringFix);
        }

        [TestMethod]
        public void Xaml_Needs_Neither_Roslyn_Nor_MsBuild()
        {
            Assert.That.AreEqual<string>(["Solution.Parser.Core"],
                                         ProjectReferencesOf("Solution.Parser.Xaml"),
                                         because: "parsing markup needs nothing but the shared contracts",
                                         fix: LayeringFix);

            Assert.That.All(PackageReferencesOf("Solution.Parser.Xaml"),
                            p => !p.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
                                 && !p.StartsWith("Microsoft.Build", StringComparison.Ordinal)
                                 && !p.StartsWith("NuGet.", StringComparison.Ordinal),
                            "is not a compiler, build engine or package manager dependency",
                            because: "a consumer that only parses XAML must not pull Roslyn, MSBuild or NuGet",
                            fix: LayeringFix);
        }

        [TestMethod]
        public void CSharp_Needs_Roslyn_But_Not_MsBuild()
        {
            Assert.That.AreEqual<string>(["Solution.Parser.Core"],
                                         ProjectReferencesOf("Solution.Parser.CSharp"),
                                         because: "the C# parser needs the shared contracts and Roslyn, nothing else of ours",
                                         fix: LayeringFix);

            Assert.That.All(PackageReferencesOf("Solution.Parser.CSharp"),
                            p => !p.StartsWith("Microsoft.Build", StringComparison.Ordinal),
                            "is not an MSBuild dependency",
                            because: "turning a file into a syntax tree does not need a build engine",
                            fix: LayeringFix);
        }

        [TestMethod]
        public void Project_Knows_No_Language()
        {
            Assert.That.AreEqual<string>(["Solution.Parser.Core"],
                                         ProjectReferencesOf("Solution.Parser.Project"),
                                         because: "this is the whole point of the split: a csproj reader that knows about "
                                                  + "C# and XAML forces both on everyone who reads a solution",
                                         fix: LayeringFix);
        }

        [TestMethod]
        public void Reading_A_Solution_Does_Not_Pull_Xaml()
        {
            Assert.That.DoesNotContain(TransitiveProjectReferencesOf("Solution.Parser.Sln"),
                                       "Solution.Parser.Xaml",
                                       because: "walking a solution is language agnostic",
                                       fix: LayeringFix);
        }

        [TestMethod]
        public void Writing_AspNet_Rules_Does_Not_Pull_Xaml()
        {
            Assert.That.DoesNotContain(TransitiveProjectReferencesOf("Solution.Parser.AspNet"),
                                       "Solution.Parser.Xaml",
                                       because: "this is the acceptance criterion of the split: ASP.NET code rules must not "
                                                + "drag the markup parser along",
                                       fix: LayeringFix);
        }

        [TestMethod]
        public void The_Meta_Package_Still_Offers_Everything()
        {
            Assert.That.AreEqual<string>([
                                             "Solution.Parser.AspNet",
                                             "Solution.Parser.CSharp",
                                             "Solution.Parser.Core",
                                             "Solution.Parser.Nuspec",
                                             "Solution.Parser.Project",
                                             "Solution.Parser.Sln",
                                             "Solution.Parser.Xaml"
                                         ],
                                         ProjectReferencesOf("Solution.Parser"),
                                         because: "an existing PackageReference to Solution.Parser must keep the surface it had",
                                         fix: "Add the new package to the meta project as well, or existing users lose it");
        }

        private static ImmutableList<string> TransitiveProjectReferencesOf(string name)
        {
            var seen = ImmutableList.CreateBuilder<string>();
            Walk(name, seen);

            return seen.ToImmutable();
        }

        private static void Walk(string name,
                                 ImmutableList<string>.Builder seen)
        {
            foreach (var reference in ProjectReferencesOf(name).Where(r => !seen.Contains(r)))
            {
                seen.Add(reference);
                Walk(reference, seen);
            }
        }
    }
}
