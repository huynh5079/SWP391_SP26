using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Approval;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Approver
{
    public partial class ApproveEventPage : Page
    {
        private LoggedInUserDto? _user;
        private string? _eventId;

        private readonly IApproverQueryService? _queryService;
        private readonly IApproverCommandService? _commandService;

        public ApproveEventPage()
        {
            InitializeComponent();

            try
            {
                _queryService = App.ServiceProvider.GetService<IApproverQueryService>();
                _commandService = App.ServiceProvider.GetService<IApproverCommandService>();
            }
            catch
            {
                // swallow - show error later if missing
            }

            Loaded += ApproveEventPage_Loaded;
        }

        public ApproveEventPage(LoggedInUserDto user, string eventId) : this()
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _eventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
        }

        private async void ApproveEventPage_Loaded(object? sender, RoutedEventArgs e)
        {
            if ((_user == null || string.IsNullOrWhiteSpace(_eventId)) && DataContext != null)
            {
                try
                {
                    var dc = DataContext;
                    var userProp = dc.GetType().GetProperty("User");
                    var eventIdProp = dc.GetType().GetProperty("EventId");
                    if (userProp != null) _user = userProp.GetValue(dc) as LoggedInUserDto;
                    if (eventIdProp != null) _eventId = eventIdProp.GetValue(dc)?.ToString();
                }
                catch { }
            }

            if (_user == null || string.IsNullOrWhiteSpace(_eventId))
            {
                MessageBox.Show("Cannot determine approver user or event id.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService?.GoBack();
                return;
            }

            await LoadDetailAsync();
        }

        private void ClearUiBeforeLoad()
        {
            txtTitle.Text = "-";
            txtOrganizer.Text = "-";
            txtStartEnd.Text = "-";
            txtLocation.Text = "-";
            txtStatus.Text = "-";
            txtDescription.Text = "-";
            lstAgendas.ItemsSource = null;
            lstDocuments.ItemsSource = null;
            lstLogs.ItemsSource = null;
            txtComment.Text = string.Empty;
        }

        private async Task LoadDetailAsync()
        {
            ClearUiBeforeLoad();

            if (_queryService == null)
            {
                MessageBox.Show("Approver query service is not registered. Check DI.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var detail = await _queryService.GetEventDetailAsync(_eventId!);
                if (detail == null)
                {
                    MessageBox.Show("Event not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                txtTitle.Text = detail.Title ?? "-";
                txtOrganizer.Text = $"{detail.OrganizerName} ({detail.OrganizerEmail})";
                txtStartEnd.Text = $"{detail.StartTime:G} — {detail.EndTime:G}";
                txtLocation.Text = detail.Location ?? "-";
                txtStatus.Text = detail.Status.ToString();
                txtDescription.Text = detail.Description ?? "-";

                lstAgendas.ItemsSource = (IEnumerable<string>?)detail.Agendas?.Select(a =>
                {
                    string timePrefix = a.StartTime.HasValue
                        ? a.StartTime.Value.ToString("g") + " - " + (a.EndTime?.ToString("g") ?? "") + ": "
                        : "";
                    string speaker = string.IsNullOrWhiteSpace(a.Speaker) ? "" : " — " + a.Speaker;
                    return timePrefix + a.Title + speaker;
                }).ToList() ?? Array.Empty<string>();

                lstDocuments.ItemsSource = (IEnumerable<string>?)detail.Documents?.Select(d => d.FileName).ToList() ?? Array.Empty<string>();
                lstLogs.ItemsSource = (IEnumerable<string>?)detail.ApprovalLogs?.Select(l => $"{l.CreatedAt:g} — {l.Action}: {l.Comment ?? string.Empty}").ToList() ?? Array.Empty<string>();

                var status = detail.Status;
                btnApprove.IsEnabled = (_commandService != null && status == DataAccess.Enum.EventStatusEnum.Pending);
                btnReject.IsEnabled = btnApprove.IsEnabled;
                btnRequestChange.IsEnabled = btnApprove.IsEnabled;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot load event detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_commandService == null)
            {
                MessageBox.Show("Approve command service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_user == null || string.IsNullOrWhiteSpace(_eventId))
            {
                MessageBox.Show("Missing user or event id.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show($"Approve \"{txtTitle.Text}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _commandService.ApproveAsync(_eventId!, _user.Id, txtComment.Text);
                MessageBox.Show("Event approved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Approve failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_commandService == null)
            {
                MessageBox.Show("Reject command service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_user == null || string.IsNullOrWhiteSpace(_eventId))
            {
                MessageBox.Show("Missing user or event id.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtComment.Text))
            {
                MessageBox.Show("Please enter a reason for rejection.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Reject \"{txtTitle.Text}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _commandService.RejectAsync(_eventId!, _user.Id, txtComment.Text);
                MessageBox.Show("Event rejected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Reject failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRequestChange_Click(object sender, RoutedEventArgs e)
        {
            if (_commandService == null)
            {
                MessageBox.Show("Request-change command service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_user == null || string.IsNullOrWhiteSpace(_eventId))
            {
                MessageBox.Show("Missing user or event id.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtComment.Text))
            {
                MessageBox.Show("Please enter requested change details.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Send change request for \"{txtTitle.Text}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _commandService.RequestChangeAsync(_eventId!, _user.Id, txtComment.Text);
                MessageBox.Show("Change request sent.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Request change failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}