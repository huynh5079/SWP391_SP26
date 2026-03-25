using System;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ParticipantListPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly IEventService _eventService;

        public ParticipantListPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = eventTitle;
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            
            LoadParticipants();
        }

        private async void LoadParticipants()
        {
            try
            {
                // Note: IEventService might need a method to get participants
                // For now we simulate or use existing if available
                // var participants = await _eventService.GetParticipantsAsync(_eventId);
                // dgParticipants.ItemsSource = participants;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading participants: {ex.Message}", "AEMS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnScanQR_Click(object sender, RoutedEventArgs e)
        {
            // Navigation to QR Scanner
        }

        private void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            // Logic to manually check-in a participant
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            // Logic to remove registration
        }
    }
}
