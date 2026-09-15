using System.Linq;
using AspNetCore.Simple.MsTest.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Solution.Parser.Xaml;
using static Solution.Parser.Xaml.Test.ParseHelper;

namespace Solution.Parser.Xaml.Test
{
    /// <summary>
    /// One window using the constructs a real WPF file uses at once. Each of them is covered on its own
    /// elsewhere; this checks that they still behave when they meet.
    /// </summary>
    [TestClass]
    public class RealWorldMarkupTest
    {
        private const string Window =
            """
            <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:local="clr-namespace:My.Sample;assembly=My.Sample"
                    xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
                    x:Class="My.Sample.MainWindow"
                    Title="Orders: overview"
                    DataContext="{x:Type local:MainViewModel}">

              <!-- resources, merged and local -->
              <Window.Resources>
                <ResourceDictionary>
                  <ResourceDictionary.MergedDictionaries>
                    <ResourceDictionary Source="pack://application:,,,/My.Sample;component/Themes/Dark.xaml" />
                  </ResourceDictionary.MergedDictionaries>

                  <Style x:Key="MoneyText" TargetType="{x:Type TextBlock}">
                    <Setter Property="Foreground" Value="{DynamicResource MoneyBrush}" />
                    <Style.Triggers>
                      <DataTrigger Binding="{Binding IsNegative}" Value="True">
                        <Setter Property="Foreground" Value="Red" />
                      </DataTrigger>
                    </Style.Triggers>
                  </Style>

                  <ControlTemplate x:Key="RoundButton" TargetType="{x:Type Button}">
                    <Border Background="{TemplateBinding Background}" CornerRadius="4">
                      <ContentPresenter />
                    </Border>
                    <ControlTemplate.Triggers>
                      <Trigger Property="IsMouseOver" Value="True">
                        <Setter Property="Opacity" Value="0.8" />
                      </Trigger>
                    </ControlTemplate.Triggers>
                  </ControlTemplate>
                </ResourceDictionary>
              </Window.Resources>

              <Grid>
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width="*" />
                  <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <TextBlock x:Name="Total"
                           Grid.Column="0"
                           Margin="0,0,5,0"
                           Style="{StaticResource MoneyText}"
                           ToolTip="Sum of all rows, tax included: see help"
                           Text="{Binding Total, StringFormat={0:#,##0.00} EUR, TargetNullValue={x:Null}}" />

                <TextBlock Grid.Column="0" ContentStringFormat="{}{0:N2}">
                  <TextBlock.Text>
                    <MultiBinding StringFormat="{}{0} of {1}">
                      <Binding Path="Done" />
                      <Binding Path="(Grid.Column)" RelativeSource="{RelativeSource AncestorType=Grid, Mode=FindAncestor}" />
                    </MultiBinding>
                  </TextBlock.Text>
                </TextBlock>

                <Button x:Name="Save"
                        Grid.Column="1"
                        Template="{StaticResource RoundButton}"
                        Content="{x:Static local:Texts.Save}"
                        Command="{Binding SaveCommand}">Save</Button>
              </Grid>
            </Window>
            """;

        [TestMethod]
        public void The_Whole_File_Parses_Without_A_Single_Diagnostic()
        {
            var tree = ParseXaml(Window);

            Assert.That.HasCount(0,
                                 tree.Diagnostics,
                                 because: "none of this is exotic, and two of these lines used to cost the entire file",
                                 fix: "Read the reported diagnostic, it names the value the tokenizer choked on");
        }

        [TestMethod]
        public void The_Root_Is_A_Window_With_Its_Code_Behind_And_Data_Context()
        {
            var tree = ParseXaml(Window);

            Assert.That.AreEqual("My.Sample.MainWindow",
                                 tree.FullQualifiedName,
                                 because: "x:Class pairs the markup with its code behind",
                                 fix: "Check ElementBuilder.BuildRoot");

            Assert.That.AreEqual("MainViewModel",
                                 tree.Root.DataContext?.FullQualifiedName,
                                 because: "a rule pairing a view with its view model reads the data context type",
                                 fix: "Check ElementBuilder.ToDataContext");

            Assert.That.AreEqual("Orders: overview",
                                 tree.Root["Title"]?.PropertyValue?.ValueText,
                                 because: "a colon in a window title is text",
                                 fix: "Check PropertyValueParser");
        }

        [TestMethod]
        public void Resources_Survive_The_Merged_Dictionary_Wrapper()
        {
            var tree = ParseXaml(Window);

            Assert.That.HasCount(1,
                                 tree.AllStyles(),
                                 because: "the window declares one style, nested two levels down in resource dictionaries",
                                 fix: "Check ElementBase.Resources and the descendant walk");

            Assert.That.HasCount(1,
                                 tree.AllTemplates(),
                                 because: "the control template is declared alongside the style",
                                 fix: "Check ElementBuilder.CreateEmpty");

            Assert.That.AreEqual("TextBlock",
                                 tree.AllStyles().Single().TargetTypeName,
                                 because: "a target type written as {x:Type TextBlock} names the same type as a plain one",
                                 fix: "Check TypeNameExtensions.ToTypeName");
        }

        [TestMethod]
        public void A_Template_Trigger_Keeps_Its_Setter()
        {
            var trigger = ParseXaml(Window).Root.AllTriggers().Single(t => t.TypeName == "Trigger");

            Assert.That.AreEqual("IsMouseOver",
                                 trigger["Property"]?.PropertyValue?.ValueText,
                                 because: "which property a trigger watches is the point of it",
                                 fix: "Check ElementBuilder");

            Assert.That.AreEqual("Opacity",
                                 trigger.Setters.Single().PropertyName,
                                 because: "a trigger's setters belong to the trigger",
                                 fix: "Check Trigger.Setters");
        }

        [TestMethod]
        public void A_Format_String_With_A_Comma_And_A_Suffix_Stays_Intact()
        {
            var total = ParseXaml(Window).FindByName("Total");
            var binding = total?["Text"]?.PropertyValue as Binding;

            Assert.That.AreEqual("{0:#,##0.00} EUR",
                                 binding?.StringFormat?.ValueText,
                                 because: "the format string keeps both its grouping separator and its trailing text",
                                 fix: "Check MarkupExtensionTokenizer.SplitArguments");

            Assert.That.IsOfType<NullExtension>(binding?.TargetNullValue,
                                                because: "the binding falls back to an explicit null",
                                                fix: "Check BindingParser");
        }

        [TestMethod]
        public void A_Multi_Binding_Written_As_Child_Elements_Is_Reachable()
        {
            var tree = ParseXaml(Window);

            Assert.That.AreEqual<string>(["Done", "(Grid.Column)"],
                                         tree.OfTypeName("Binding").Select(b => b["Path"]?.PropertyValue?.ValueText ?? string.Empty),
                                         because: "bindings written as elements inside a MultiBinding are ordinary elements",
                                         fix: "Check ElementBuilder, a <Binding> element is a normal child");
        }

        [TestMethod]
        public void A_Pack_Uri_Is_Plain_Text()
        {
            var merged = ParseXaml(Window)
                         .AllElements()
                         .OfType<ResourceDictionaryElement>()
                         .Single(d => !d.Source.Length.Equals(0));

            Assert.That.AreEqual("pack://application:,,,/My.Sample;component/Themes/Dark.xaml",
                                 merged.Source,
                                 because: "a pack uri is full of colons and commas and is still just a string",
                                 fix: "Check PropertyValueParser, only an unescaped leading brace makes a value markup");
        }

        [TestMethod]
        public void Content_And_Attributes_Coexist_On_The_Same_Element()
        {
            var save = ParseXaml(Window).FindByName("Save");

            Assert.That.AreEqual("Save",
                                 save?.Content,
                                 because: "the button carries text content next to its attributes",
                                 fix: "Check ElementBuilder.ContentOf");

            Assert.That.AreEqual("Texts",
                                 (save?["Content"]?.PropertyValue as XStaticMarkupExtension)?.Type,
                                 because: "the Content attribute is a separate thing from the text content",
                                 fix: "Check XStaticParser");
        }
    }
}
