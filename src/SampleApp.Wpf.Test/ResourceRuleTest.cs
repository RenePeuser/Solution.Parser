using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleApp.Wpf.Test.Rules;
using Solution.Parser.Xaml;

namespace SampleApp.Wpf.Test
{
    [TestClass]
    public class ResourceRuleTest
    {
        [TestMethod]
        public void Every_Referenced_Resource_Is_Declared_Somewhere_In_The_Application()
        {
            var findings = ResourceRule.FindUndeclaredKeys(SampleApp.Views).ToList();

            Assert.That.HasCount(0,
                                 findings,
                                 because: "a missing resource key throws when the view is first shown: "
                                          + string.Join("; ", findings),
                                 fix: "Either the key is misspelled or the theme file lost the resource");
        }

        [TestMethod]
        public void The_Rule_Actually_Catches_A_Missing_Key()
        {
            const string brokenView =
                """
                <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                  <TextBlock Style="{StaticResource NoSuchStyle}" />
                </UserControl>
                """;

            var findings = ResourceRule.FindUndeclaredKeys([.. SampleApp.Views, brokenView.ParseXaml("Broken.xaml")]);

            Assert.That.HasCount(1,
                                 findings,
                                 because: "the key is declared nowhere in the application",
                                 fix: "Check ResourceRule.FindUndeclaredKeys");
        }
    }
}
