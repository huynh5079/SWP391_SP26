using System;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class EventDetailsPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly string _eventId;
        private readonly IEventService _eventService;
        private EventDetailsDto? _details;

        public EventDetailsPage(LoggedInUserDto user, string eventId)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            
            LoadDetails();
        }

        private async void LoadDetails()
        {
            try
            {
                _details = await _eventService.GetEventDetailsAsync(_eventId, _user.Id);
                
                if (_details != null)
                {
                    txtTitle.Text = _details.Title;
                    txtStatus.Text = _details.Status.ToString().ToUpper();
                    txtDateTime.Text = $"{_details.StartTime:dd/MM/yyyy HH:mm} - {_details.EndTime:HH:mm}";
                    txtDescription.Text = _details.Description;
                    txtCapacity.Text = _details.MaxCapacity.ToString();
                    txtRegistered.Text = _details.RegisteredCount.ToString();
                    txtWaitlist.Text = _details.WaitlistCount.ToString();
                    txtLocation.Text = _details.Location ?? "Not Specified";
                    txtDepartment.Text = _details.DepartmentName ?? "N/A";
                    txtSemester.Text = _details.SemesterName ?? "N/A";
                    txtRating.Text = _details.AvgRating.ToString("F1");

                    icAgendas.ItemsSource = _details.Agendas;
                    icDocuments.ItemsSource = _details.Documents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading event details: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            // Navigation to Edit Page (could reuse CreateEventPage with edit mode)
            MessageBox.Show("Edit functionality is coming soon!", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnParticipants_Click(object sender, RoutedEventArgs e)
        {
            if (_details != null)
            {
                NavigationService.Navigate(new ParticipantListPage(_user, Guid.Parse(_details.EventId), _details.Title));
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
             if (sender is Button btn && btn.DataContext is EventDocumentDto doc)
             {
                 MessageBox.Show($"Downloading {doc.FileName} from {doc.Url}...", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
             }
        }
    }
}
