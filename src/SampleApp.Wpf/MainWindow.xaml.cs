using System.Windows;
using SampleApp.Wpf.ViewModels;

namespace SampleApp.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }
    }
}
