using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class CreateEventPage : Page
    {
        private readonly IEventService _eventService;

        public CreateEventPage()
        {
            InitializeComponent();
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Business Logic to gather fields and call CreateEventAsync
            MessageBox.Show("Event Submission Processed (Simulated UI Workflow)", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigationService.GoBack();
        }
    }
}
