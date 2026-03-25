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

                    // Update Visibility based on Lifecycle
                    BtnSubmit.Visibility = _details.CanSendForApproval ? Visibility.Visible : Visibility.Collapsed;
                    BtnEdit.Visibility = _details.CanEdit ? Visibility.Visible : Visibility.Collapsed;
                    BtnDelete.Visibility = (_details.Status == DataAccess.Enum.EventStatusEnum.Pending || _details.Status == DataAccess.Enum.EventStatusEnum.Published) ? Visibility.Collapsed : Visibility.Visible;

                    // Update Status Color
                    UpdateStatusUI(_details.Status);

                    icAgendas.ItemsSource = _details.Agendas;
                    icDocuments.ItemsSource = _details.Documents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading event details: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusUI(DataAccess.Enum.EventStatusEnum status)
        {
            txtStatus.Text = status.ToString().ToUpper();
            switch (status)
            {
                case DataAccess.Enum.EventStatusEnum.Published:
                    borderStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 52, 211, 153)); // Green
                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153));
                    break;
                case DataAccess.Enum.EventStatusEnum.Pending:
                    borderStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 251, 191, 36)); // Yellow
                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36));
                    break;
                case DataAccess.Enum.EventStatusEnum.Draft:
                    borderStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 156, 163, 175)); // Gray
                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                    break;
                default:
                    borderStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 239, 68, 68)); // Red
                    txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    break;
            }
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _eventService.SendForApprovalAsync(_user.Id, _eventId);
                MessageBox.Show("Event submitted for approval successfully!", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Submit failed: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this event? This cannot be undone.", "AEMS Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await _eventService.DeleteEventAsync(_user.Id, _eventId);
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete failed: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
