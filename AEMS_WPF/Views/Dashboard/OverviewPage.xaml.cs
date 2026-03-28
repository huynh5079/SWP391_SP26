using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Dashboard;
using BusinessLogic.Service.System;
using BusinessLogic.Service.UserActivities;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Dashboard
{
    public partial class OverviewPage : Page
    {
        private readonly IDashboardService _dashboardService;
        private readonly INotificationService _notificationService;
        private readonly IUserActivityLogService _activityLogService;
        private readonly LoggedInUserDto _user;

        public OverviewPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            _dashboardService = App.ServiceProvider.GetRequiredService<IDashboardService>();
            _notificationService = App.ServiceProvider.GetRequiredService<INotificationService>();
            _activityLogService = App.ServiceProvider.GetRequiredService<IUserActivityLogService>();
            
            txtWelcome.Text = $"Hello, {_user.FullName.Split(' ')[0]}!";
            
            if (_user.Role == "Admin")
            {
                AdminSection.Visibility = Visibility.Visible;
            }

            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var stats = await _dashboardService.GetDashboardAsync(_user.Id);
                
                if (stats != null)
                {
                    txtStatTotal.Text = stats.TotalEvents.ToString();
                    txtStatUpcoming.Text = stats.UpcomingEvents.ToString();
                    txtStatPending.Text = stats.DraftEvents.ToString();
                    txtStatRevenue.Text = stats.DepositCollectedThisMonth.ToString("N0");
                    
                    UpcomingList.ItemsSource = stats.UpcomingList;
                    FeedbackList.ItemsSource = stats.RecentFeedbacks;
                    
                    if (stats.RecentFeedbacks == null || stats.RecentFeedbacks.Count == 0)
                    {
                        txtNoFeedback.Visibility = Visibility.Visible;
                    }
                }

                if (_user.Role == "Admin")
                {
                    var recentActivities = await _activityLogService.GetRecentActivitiesAsync(5);
                    ActivityList.ItemsSource = recentActivities;

                    var recentNotifications = await _notificationService.GetUserNotificationsAsync(_user.Id);
                    // Take only top 5 for dashboard
                    AlertList.ItemsSource = System.Linq.Enumerable.Take(recentNotifications, 5);
                }
            }
            catch (System.Exception ex)
            {
                // Silently fail dashboard elements or log if needed
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
        }
    }
}
