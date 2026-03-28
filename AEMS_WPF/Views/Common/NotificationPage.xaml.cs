using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.System;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Common
{
    public partial class NotificationPage : Page
    {
        private readonly INotificationService _notificationService;
        private readonly LoggedInUserDto _user;

        public NotificationPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            _notificationService = App.ServiceProvider.GetRequiredService<INotificationService>();
            LoadNotifications();
        }

        private async void LoadNotifications()
        {
            try
            {
                var notifications = await _notificationService.GetUserNotificationsAsync(_user.Id);
                NotificationList.ItemsSource = notifications;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to load notifications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string notificationId)
            {
                await _notificationService.DeleteNotificationAsync(notificationId);
                LoadNotifications();
            }
        }

        private async void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Are you sure you want to clear all your notifications?", 
                                         "Confirm Clear All", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (confirm == MessageBoxResult.Yes)
            {
                await _notificationService.ClearAllNotificationsAsync(_user.Id);
                LoadNotifications();
            }
        }
    }
}
