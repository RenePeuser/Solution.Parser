using System.IO;
using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test
{
    /// <summary>
    /// The sample application seen through the parser. Because SampleApp.Wpf is a real WPF project,
    /// anything asserted here is asserted about markup the WPF compiler accepted.
    /// </summary>
    [TestClass]
    public class SampleAppParseTest
    {
        [TestMethod]
        public void Every_View_Of_The_Application_Is_Found_Through_The_Solution()
        {
            Assert.That.AreEqual<string>(["App.xaml", "Controls.xaml", "MainWindow.xaml", "PersonView.xaml"],
                                         SampleApp.Views.Select(v => Path.GetFileName(v.FilePath)).Order(),
                                         because: "walking solution to project to files is the entry point a consumer uses",
                                         fix: "Check ProjectFile.SourceFiles and the XamlFiles() view");
        }

        [TestMethod]
        public void Nothing_In_The_Application_Confuses_The_Parser()
        {
            Assert.That.All(SampleApp.Views,
                            v => v.Diagnostics.IsEmpty,
                            "parses without a diagnostic",
                            because: "the WPF compiler accepted all of this, so the parser has no excuse",
                            fix: "Read the diagnostic, it names the value the parser could not read");
        }

        [TestMethod]
        public void Each_Document_Element_Is_Recognised_For_What_It_Is()
        {
            Assert.That.IsOfType<Application>(SampleApp.View("App.xaml").Root,
                                              because: "App.xaml starts with an Application element",
                                              fix: "Check ElementBuilder.CreateEmptyRoot");

            Assert.That.IsOfType<Window>(SampleApp.View("MainWindow.xaml").Root,
                                         because: "MainWindow.xaml starts with a Window element",
                                         fix: "Check ElementBuilder.CreateEmptyRoot");

            Assert.That.IsOfType<UserControl>(SampleApp.View("PersonView.xaml").Root,
                                              because: "PersonView.xaml starts with a UserControl element",
                                              fix: "Check ElementBuilder.CreateEmptyRoot");

            Assert.That.IsOfType<ResourceDictionaryRoot>(SampleApp.View("Controls.xaml").Root,
                                                         because: "a theme file is a resource dictionary",
                                                         fix: "Check ElementBuilder.CreateEmptyRoot");
        }

        [TestMethod]
        public void The_Code_Behind_Type_Is_Reachable_From_The_Markup()
        {
            Assert.That.AreEqual("SampleApp.Wpf.MainWindow",
                                 SampleApp.View("MainWindow.xaml").FullQualifiedName,
                                 because: "x:Class is what pairs a view with its code behind",
                                 fix: "Check ElementBuilder.BuildRoot");
        }

        [TestMethod]
        public void A_Merged_Dictionary_Points_At_A_File_That_Exists()
        {
            var merged = SampleApp.View("App.xaml")
                                  .AllElements()
                                  .OfType<ResourceDictionaryElement>()
                                  .Select(d => d.Source)
                                  .Single(source => source.Length > 0);

            Assert.That.AreEqual("Themes/Controls.xaml",
                                 merged,
                                 because: "a rule checking that merged dictionaries resolve reads the Source",
                                 fix: "Check ResourceDictionaryElement.Source");
        }

        [TestMethod]
        public void The_Themes_File_Declares_The_Styles_The_Application_Uses()
        {
            Assert.That.AreEqual<string>(["CaptionText", "PrimaryButton"],
                                         SampleApp.View("Controls.xaml").AllStyles().Select(s => s.XKey).Order(),
                                         because: "styles live in the theme file and a rule has to find them there",
                                         fix: "Check QueryExtensions.AllStyles");
        }
    }
}
