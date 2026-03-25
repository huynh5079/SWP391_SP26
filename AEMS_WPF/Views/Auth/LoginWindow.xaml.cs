using System.Windows;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            // Resolve service from App's ServiceProvider
            _authService = App.ServiceProvider.GetRequiredService<IAuthService>();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            string email = txtEmail.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both email and password.");
                return;
            }

            try
            {
                // Create DTO as required by IAuthService
                var loginRequest = new LoginRequestDto
                {
                    Email = email,
                    Password = password
                };

                var result = await _authService.LoginAsync(loginRequest);
                
                if (result != null && result.User != null)
                {
                    // Check if role is allowed for Staff App
                    string role = result.User.Role ?? "";
                    if (role == "Admin" || role == "Organizer" || role == "Approver")
                    {
                        var dashboard = new MainWindow(result.User);
                        dashboard.Show();
                        this.Close();
                    }
                    else
                    {
                        ShowError("Access denied. This application is for Staff only.");
                    }
                }
                else
                {
                    ShowError("Invalid email or password.");
                }
            }
            catch (System.Exception ex)
            {
                // Capture the specific error message from the service
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
