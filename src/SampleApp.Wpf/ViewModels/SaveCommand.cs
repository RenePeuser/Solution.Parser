using System;
using System.Windows.Input;

namespace SampleApp.Wpf.ViewModels
{
    public sealed class SaveCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
