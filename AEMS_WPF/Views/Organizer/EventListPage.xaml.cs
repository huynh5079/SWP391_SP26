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
                var events = await _eventService.GetMyEventsAsync(_user.Id);
                dgEvents.ItemsSource = events;

                // Update counters
                txtTotalEvents.Text = events.Count.ToString();
                txtPendingEvents.Text = events.Count(e => e.Status == DataAccess.Enum.EventStatusEnum.Pending).ToString();
                txtActiveEvents.Text = events.Count(e => e.Status == DataAccess.Enum.EventStatusEnum.Published).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading events: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void BtnTeams_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BusinessLogic.DTOs.Role.Organizer.EventListDto eventItem)
            {
                NavigationService.Navigate(new TeamManagementPage(_user, Guid.Parse(eventItem.EventId), eventItem.Title));
            }
        }

        private void BtnExpenses_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BusinessLogic.DTOs.Role.Organizer.EventListDto eventItem)
            {
                NavigationService.Navigate(new ExpenseSubmissionPage(_user, Guid.Parse(eventItem.EventId), eventItem.Title));
            }
        }

        private void BtnQuiz_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Quiz and Feedback creation is coming in the next phase!", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
