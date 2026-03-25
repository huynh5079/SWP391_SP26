using System;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.GetQuiz;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class QuizManagementPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly string _eventId;
        private readonly string _eventTitle;
        private readonly IQuizService _quizService;

        public QuizManagementPage(LoggedInUserDto user, string eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            _eventTitle = eventTitle;
            _quizService = App.ServiceProvider.GetRequiredService<IQuizService>();

            txtEventTitle.Text = $"Quiz Management: {_eventTitle}";
            LoadQuizzes();
        }

        private async void LoadQuizzes()
        {
            try
            {
                var response = await _quizService.GetOrganizerQuizzesAsync(new GetOrganizerQuizzesRequestDto 
                { 
                    UserId = _user.Id,
                    EventId = _eventId 
                });
                
                icQuizzes.ItemsSource = response.Quizzes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading quizzes: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnCreateQuiz_Click(object sender, RoutedEventArgs e)
        {
            var createWin = new CreateQuizWindow(_user, _eventId) { Owner = Window.GetWindow(this) };
            if (createWin.ShowDialog() == true)
            {
                LoadQuizzes();
            }
        }

        private void BtnViewScores_Click(object sender, RoutedEventArgs e)
        {
             MessageBox.Show("Scores View coming next!", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnPublish_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for publish quiz
        }
    }
}
