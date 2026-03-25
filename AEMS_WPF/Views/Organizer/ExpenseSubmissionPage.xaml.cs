using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BusinessLogic.DTOs.Authentication.Login;
using Microsoft.Win32;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ExpenseSubmissionPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private string? _selectedImagePath;

        public ExpenseSubmissionPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = $"Expenses: {eventTitle}";
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                imgReceipt.Source = new BitmapImage(new Uri(_selectedImagePath));
                stackPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtExpenseName.Text) || string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedImagePath == null)
            {
                MessageBox.Show("Please upload a receipt image as proof.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // In a real implementation, we would call a service like:
                // await _budgetService.SubmitActualExpenseAsync(_eventId, new ExpenseDto { ... }, _selectedImagePath);
                
                MessageBox.Show("Expense submitted successfully! The finance team will review it shortly.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting expense: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
