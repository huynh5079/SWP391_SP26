using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Dashboard
{
    public partial class OverviewPage : Page
    {
        private readonly IDashboardService _dashboardService;
        private readonly LoggedInUserDto _user;

        public OverviewPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            _dashboardService = App.ServiceProvider.GetRequiredService<IDashboardService>();
            
            txtWelcome.Text = $"Hello, {_user.FullName.Split(' ')[0]}!";
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // In a real app, we'd use the current user's ID
                // Note: The service methods might need the Guid userId string
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
                        txtNoFeedback.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
            catch { }
        }
    }
}
