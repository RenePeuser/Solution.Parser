using SampleApp.Wpf.Models;

namespace SampleApp.Wpf.ViewModels
{
    public sealed class PersonViewModel(Person person) : ViewModelBase
    {
        public string FullName => person.FirstName + " " + person.LastName;

        public int Age => person.Age;

        public string City => person.City;
    }
}
