using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.System;
using BusinessLogic.Service.UserActivities;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Common
{
    public partial class ActivityLogPage : Page
    {
        private readonly IUserActivityLogService _activityLogService;
        private int _currentPage = 1;
        private const int _pageSize = 20;

        public ActivityLogPage()
        {
            InitializeComponent();
            _activityLogService = App.ServiceProvider.GetRequiredService<IUserActivityLogService>();
            LoadData();
        }

        private async void LoadData()
        {
            var result = await _activityLogService.GetLogsAsync(_currentPage, _pageSize, null);
            dgLogs.ItemsSource = result.Data;
            txtPage.Text = $"Page {_currentPage}";
            btnPrev.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = result.Data.Count() == _pageSize; // Simple check for next page presence
        }

        private void CleanUp_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to clean up activity logs older than 30 days?", 
                                         "Confirm Clean Up", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (confirm == MessageBoxResult.Yes)
            {
                _activityLogService.DeleteOldLogsAsync(30);
                LoadData();
                MessageBox.Show("Old logs have been cleaned up.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadData();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            LoadData();
        }
    }
}
