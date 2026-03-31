using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Approval;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Dashboard
{
    public partial class ApproveDashBoard : Window
    {
        private readonly LoggedInUserDto _user;
        private readonly ObservableCollection<EventItemDto> _pending = new();
        private readonly IApproverQueryService? _queryService;
        private readonly IApproverCommandService? _commandService;
        private bool _manageExpanded = false;

        public ApproveDashBoard(LoggedInUserDto user)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user));
            dgPendingApprovals.ItemsSource = _pending;

            try
            {
                _queryService = App.ServiceProvider.GetService<IApproverQueryService>();
                _commandService = App.ServiceProvider.GetService<IApproverCommandService>();
            }
            catch { }

            // Set user info in sidebar
            txtUserName.Text = _user.FullName ?? "Approver";
            txtUserRole.Text = _user.Role ?? "Approver";
            txtUserInitial.Text = string.IsNullOrEmpty(_user.FullName) ? "A" : _user.FullName[0].ToString().ToUpper();

            Loaded += ApproveDashBoard_Loaded;
        }

        private async void ApproveDashBoard_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadPendingAsync();
        }

        private async Task LoadPendingAsync()
        {
            tbTotalPending.Text = "0";
            tbApproved.Text = "0";
            tbRejected.Text = "0";
            tbActionRequired.Text = "0";

            if (_queryService == null) return;

            try
            {
                var items = await _queryService.GetPendingEventsAsync(
                    approverUserId: _user.Id,
                    search: null,
                    status: null,
                    page: 1,
                    pageSize: 50
                ) ?? new();

                _pending.Clear();
                foreach (var it in items) _pending.Add(it);

                tbTotalPending.Text = items.Count.ToString();
                tbActionRequired.Text = items.Count(i => i.StartTime <= DateTime.Now.AddDays(7)).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot load pending approvals: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private EventItemDto? GetRowContext(object sender)
        {
            if (sender is Button btn && btn.DataContext is EventItemDto dto) return dto;
            if (sender is FrameworkElement fe && fe.DataContext is EventItemDto dto2) return dto2;
            return null;
        }

       
        

        private void NavManage_Click(object sender, RoutedEventArgs e)
        {
            _manageExpanded = !_manageExpanded;
            ManageDropdown.Visibility = _manageExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavTopics_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Topics page (not implemented).", "Topics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavLocations_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Locations page (not implemented).", "Locations", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavAgenda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Agenda page (not implemented).", "Agenda", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavNotifications_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Notifications page (not implemented).", "Notifications", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== TABLE ACTIONS =====
        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            if (_commandService == null)
            {
                MessageBox.Show("Approve service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show($"Approve \"{dto.Title}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _commandService.ApproveAsync(dto.Id, _user.Id, comment: "Approved from approver dashboard");
                await LoadPendingAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Approve failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            OpenApproveEventPage(dto.Id, "Reject Event");
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            OpenApproveEventPage(dto.Id, "View Event");
        }

        private void BtnRequestChange_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            OpenApproveEventPage(dto.Id, "Request Change");
        }

        private void OpenApproveEventPage(string eventId, string title)
        {
            var page = new Views.Approver.ApproveEventPage(_user, eventId);
            var frame = new Frame();
            frame.Navigate(page);
            var win = new Window
            {
                Content = frame,
                Owner = this,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = title
            };
            win.ShowDialog();
            _ = LoadPendingAsync();
        }

        private async void BtnViewAll_Click(object sender, RoutedEventArgs e)
        {
            await LoadPendingAsync();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new Views.Auth.LoginWindow();
            login.Show();
            this.Close();
        }
    }
}