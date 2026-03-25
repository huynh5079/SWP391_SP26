using System.Windows.Controls;
using BusinessLogic.Service.System;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Common
{
    public partial class NotificationPage : Page
    {
        private readonly INotificationService _notificationService;

        public NotificationPage()
        {
            InitializeComponent();
            _notificationService = App.ServiceProvider.GetRequiredService<INotificationService>();
            LoadNotifications();
        }

        private async void LoadNotifications()
        {
            try
            {
                // Note: GetUserNotifications might need a specific userId
                // For now, let's show dummy or fetch all if available
                // var notifications = await _notificationService.GetNotificationsAsync(userId);
                // NotificationList.ItemsSource = notifications;
            }
            catch { }
        }
    }
}
