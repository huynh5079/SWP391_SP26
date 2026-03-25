using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class EventListPage : Page
    {
        private readonly IEventService _eventService;

        public EventListPage()
        {
            InitializeComponent();
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            LoadEvents();
        }

        private async void LoadEvents()
        {
            try
            {
                // In a real scenario, we'd filter by the current organizer's ID
                // var events = await _eventService.GetAllEventsAsync();
                // dgEvents.ItemsSource = events;
            }
            catch { }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateEventPage());
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            // Show details
        }
    }
}
