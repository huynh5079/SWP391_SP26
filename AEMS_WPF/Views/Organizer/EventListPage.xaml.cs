using BusinessLogic.DTOs.Authentication.Login;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class EventListPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly IEventService _eventService;

        public EventListPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
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
            NavigationService.Navigate(new CreateEventPage(_user));
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            // Show details - Logic can be added later or navigate to DetailsPage
            MessageBox.Show("Event details functionality is coming soon!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnParticipants_Click(object sender, RoutedEventArgs e)
        {
            using var scope = App.ServiceProvider.CreateScope();
            if (sender is Button btn && btn.DataContext is BusinessLogic.DTOs.Role.Organizer.EventListDto eventItem)
            {
                NavigationService.Navigate(new ParticipantListPage(_user, Guid.Parse(eventItem.EventId), eventItem.Title));
            }
        }
    }
}
