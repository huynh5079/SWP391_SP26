using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;

namespace AEMS_WPF
{
    public partial class MainWindow : Window
    {
        private readonly LoggedInUserDto _user;

        public MainWindow(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            SetupUIForRole();
        }

        private void SetupUIForRole()
        {
            txtUserName.Text = _user.FullName;
            txtUserRole.Text = _user.Role;
            txtUserInitial.Text = string.IsNullOrEmpty(_user.FullName) ? "?" : _user.FullName[0].ToString().ToUpper();

            
            // Handle Menu Visibility
            if (_user.Role == "Admin")
            {
                BtnUsers.Visibility = Visibility.Visible;
                BtnErrorLogs.Visibility = Visibility.Visible;
                BtnApprovals.Visibility = Visibility.Visible;
            }
            else if (_user.Role == "Approver")
            {
                BtnApprovals.Visibility = Visibility.Visible;
                BtnEvents.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnErrorLogs.Visibility = Visibility.Collapsed;
            }

            MainFrame.Navigate(new Views.Dashboard.OverviewPage(_user));
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                switch (btn.Name)
                {
                    case "BtnDashboard":
                        MainFrame.Navigate(new Views.Dashboard.OverviewPage(_user));
                        break;
                    case "BtnEvents":
                        MainFrame.Navigate(new Views.Organizer.EventListPage(_user));
                        break;
                    case "BtnNotifications":
                        MainFrame.Navigate(new Views.Common.NotificationPage());
                        break;
                    case "BtnErrorLogs":
                        MainFrame.Navigate(new Views.Common.SystemErrorLogPage());
                        break;
                    case "BtnApprovals":
                        MainFrame.Navigate(new Views.Dashboard.ApproveDashBoard(_user));
                        break;
                        // Add more cases as more pages are implemented
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new Views.Auth.LoginWindow();
            login.Show();
            this.Close();
        }
    }
}