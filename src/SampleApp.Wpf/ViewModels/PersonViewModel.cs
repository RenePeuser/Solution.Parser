using SampleApp.Wpf.Models;

namespace SampleApp.Wpf.ViewModels
{
    public sealed class PersonViewModel : ViewModelBase
    {
        private readonly Person _person;

        public PersonViewModel(Person person)
        {
            _person = person;
        }

        public string FullName => _person.FirstName + " " + _person.LastName;

        public int Age => _person.Age;

        public string City => _person.City;
    }
}
