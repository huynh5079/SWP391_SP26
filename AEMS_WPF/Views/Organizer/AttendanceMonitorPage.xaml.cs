using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Organizer.CheckIn;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class AttendanceMonitorPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly ICheckInService _checkInService;
        private readonly ObservableCollection<AttendanceFeedItem> _feed = new();

        public AttendanceMonitorPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = $"Monitor: {eventTitle}";
            _checkInService = App.ServiceProvider.GetRequiredService<ICheckInService>();
            
            lvFeed.ItemsSource = _feed;
            LoadInitialStats();
        }

        private async void LoadInitialStats()
        {
            try
            {
                var participants = await _checkInService.GetParticipantsAsync(_eventId.ToString());
                int total = participants.Count;
                int checkedIn = participants.Count(p => p.Status == DataAccess.Enum.TicketStatusEnum.CheckedIn || p.Status == DataAccess.Enum.TicketStatusEnum.Used);
                
                txtTotalRegistered.Text = total.ToString();
                txtCheckedIn.Text = checkedIn.ToString();
                txtPercentage.Text = total > 0 ? $"{(checkedIn * 100 / total)}%" : "0%";

                // Populate feed with recent check-ins
                foreach(var p in participants.Where(p => p.CheckInTime.HasValue).OrderByDescending(p => p.CheckInTime))
                {
                    _feed.Add(new AttendanceFeedItem 
                    { 
                        FullName = p.FullName, 
                        StudentCode = p.StudentCode, 
                        Timestamp = p.CheckInTime.Value,
                        Initials = string.IsNullOrWhiteSpace(p.FullName) ? "?" : p.FullName[0].ToString().ToUpper()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading stats: {ex.Message}", "AEMS", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void BtnManualCheckIn_Click(object sender, RoutedEventArgs e)
        {
            string code = txtManualCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            try
            {
                // Note: The CheckInRequestDto needs either QrPayload or direct student info
                // For simplicity, we assume the code matches what's expected by the backend
                var response = await _checkInService.ProcessCheckInAsync(new BusinessLogic.DTOs.Ticket.CheckInRequestDto
                {
                    EventId = _eventId.ToString(),
                    QrPayload = code
                }, _user.Id);

                if (response.IsSuccess)
                {
                    MessageBox.Show(response.Message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtManualCode.Clear();
                    LoadInitialStats(); // Refresh
                }
                else
                {
                    MessageBox.Show(response.Message, "Check-in Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSimulateScan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("QR Simulation: In production, this would open a camera window and decode the QR payload into the check-in service.", "QR Simulator", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class AttendanceFeedItem
    {
        public string FullName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Initials { get; set; } = "";
    }
}
