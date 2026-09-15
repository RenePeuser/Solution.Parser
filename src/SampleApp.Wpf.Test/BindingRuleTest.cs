using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleApp.Wpf.Test.Rules;
using Solution.Parser.CSharp;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test
{
    /// <summary>
    /// The rule this whole setup exists for: markup and code checked against each other.
    /// </summary>
    [TestClass]
    public class BindingRuleTest
    {
        [TestMethod]
        public void Every_View_Declares_Which_View_Model_It_Binds_To()
        {
            Assert.That.AreEqual("MainViewModel",
                                 SampleApp.View("MainWindow.xaml").DeclaredViewModel(),
                                 because: "d:DataContext is how a view names its view model, and telling it from a plain "
                                          + "DataContext needs the prefix",
                                 fix: "Check Property.Prefix and XamlPropertyParser");

            Assert.That.AreEqual("PersonViewModel",
                                 SampleApp.View("PersonView.xaml").DeclaredViewModel(),
                                 because: "the item view names its own view model",
                                 fix: "Check Property.Prefix and XamlPropertyParser");
        }

        [TestMethod]
        public void Every_Bound_Path_Exists_On_Its_View_Model()
        {
            var findings = SampleApp.Views
                                    .SelectMany(view => BindingRule.FindUnresolvedPaths(view, SampleApp.Types))
                                    .ToList();

            Assert.That.HasCount(0,
                                 findings,
                                 because: "a mistyped binding path compiles, renders nothing and is found by no compiler: "
                                          + string.Join("; ", findings),
                                 fix: "Either the markup names the wrong property or the view model lost it");
        }

        [TestMethod]
        public void The_Rule_Actually_Catches_A_Typo()
        {
            // A rule that never fails proves nothing, so the same rule runs against a view that is
            // deliberately wrong. Parsed from memory, because a broken view cannot live in a project
            // that has to compile.
            const string brokenView =
                """
                <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                             xmlns:vm="clr-namespace:SampleApp.Wpf.ViewModels"
                             d:DataContext="{d:DesignInstance Type=vm:PersonViewModel}">
                  <TextBlock Text="{Binding FulName}" />
                </UserControl>
                """;

            var findings = BindingRule.FindUnresolvedPaths(brokenView.ParseXaml("Broken.xaml"), SampleApp.Types);

            Assert.That.HasCount(1,
                                 findings,
                                 because: "FulName is a typo for FullName and the rule has to say so",
                                 fix: "Check BindingRule.FindUnresolvedPaths");

            Assert.That.Contains(findings.Single(),
                                 "FulName",
                                 because: "the finding has to name the offending path",
                                 fix: "Check the message BindingRule builds");
        }
    }
}
