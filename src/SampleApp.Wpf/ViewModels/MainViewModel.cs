using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SampleApp.Wpf.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private string _searchTerm = string.Empty;
        private PersonViewModel? _selectedPerson;
        private bool _isBusy;
        private string _errorMessage = string.Empty;

        public string Title => "People";

        public ObservableCollection<PersonViewModel> People { get; } = [];

        public int PersonCount => People.Count;

        public ICommand SaveCommand { get; } = new SaveCommand();

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
            }
        }

        public PersonViewModel? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }
}
