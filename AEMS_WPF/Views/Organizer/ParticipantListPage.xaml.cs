using System;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

using BusinessLogic.Service.Organizer.CheckIn;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ParticipantListPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly IEventService _eventService;
        private readonly ICheckInService _checkInService;

        public ParticipantListPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = eventTitle;
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            _checkInService = App.ServiceProvider.GetRequiredService<ICheckInService>();
            
            LoadParticipants();
        }

        private async void LoadParticipants()
        {
            try
            {
                var participants = await _checkInService.GetParticipantsAsync(_eventId.ToString());
                dgParticipants.ItemsSource = participants;
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
            NavigationService.Navigate(new AttendanceMonitorPage(_user, _eventId, txtEventTitle.Text));
        }

        private async void BtnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BusinessLogic.DTOs.Role.Organizer.EventParticipantDto participant)
            {
                try
                {
                    var response = await _checkInService.ProcessCheckInAsync(new BusinessLogic.DTOs.Ticket.CheckInRequestDto
                    {
                        EventId = _eventId.ToString(),
                        QrPayload = participant.TicketId // CheckInService uses TicketId as QrPayload usually
                    }, _user.Id);

                    MessageBox.Show(response.Message, "Check-in", MessageBoxButton.OK, response.IsSuccess ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    if (response.IsSuccess) LoadParticipants();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BusinessLogic.DTOs.Role.Organizer.EventParticipantDto participant)
            {
                var result = MessageBox.Show($"Are you sure you want to cancel the ticket for {participant.FullName}?", "Confirm Cancellation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _checkInService.CancelTicketAsync(participant.TicketId, _user.Id);
                        MessageBox.Show("Ticket cancelled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadParticipants();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
