using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Approval;
using Microsoft.Extensions.DependencyInjection;
using AEMS_WPF.Views.Approver;

namespace AEMS_WPF.Views.Dashboard
{
    /// <summary>
    /// Interaction logic for ApproveDashBoard.xaml
    /// </summary>
    public partial class ApproveDashBoard : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly ObservableCollection<EventItemDto> _pending = new();

        private readonly IApproverQueryService? _queryService;
        private readonly IApproverCommandService? _commandService;

        public ApproveDashBoard(LoggedInUserDto user)
        {
            InitializeComponent();

            _user = user ?? throw new ArgumentNullException(nameof(user));

            dgPendingApprovals.ItemsSource = _pending;

            // resolve optional services
            try
            {
                _queryService = App.ServiceProvider.GetService<IApproverQueryService>();
                _commandService = App.ServiceProvider.GetService<IApproverCommandService>();
            }
            catch
            {
                // swallow - will show empty state
            }

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

            if (_queryService == null)
            {
                // service missing -> keep empty state
                return;
            }

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
                // placeholders: if query service provides approved/rejected counts adapt here
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
            // Open the approve window as a modal so approver can enter reason
            var win = new Views.Approver.ApproveEventPage(_user, dto.Id);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            // refresh list after possible action
            _ = LoadPendingAsync();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            var win = new Views.Approver.ApproveEventPage(_user, dto.Id);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
            _ = LoadPendingAsync();
        }

        private void BtnBudget_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            // placeholder: open budget/details page (implement if exists)
            MessageBox.Show($"Open budget for event '{dto.Title}' (not implemented).", "Budget", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnChange_Click(object sender, RoutedEventArgs e)
        {
            var dto = GetRowContext(sender);
            if (dto == null) return;
            // placeholder: open change request page (not implemented)
            MessageBox.Show($"Open change request for '{dto.Title}' (not implemented).", "Change", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BtnRequestChange_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement the logic for handling the "Change" button click
            MessageBox.Show("Request Change clicked!");
        }
        private async void BtnViewAll_Click(object sender, RoutedEventArgs e)
        {
            await LoadPendingAsync();
            // optionally navigate to a full list page if exists
        }

        // Sidebar logout (if hosted in a Window this will close the window and show login)
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new Views.Auth.LoginWindow();
            login.Show();
            Window.GetWindow(this)?.Close();
        }
    }
}
