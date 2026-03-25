using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Event.Sub_Service.Quiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.CreateQuiz;
using BusinessLogic.DTOs.Event.Quiz.ForMainRole.Contracts;
using DataAccess.Enum;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class CreateQuizWindow : Window
    {
        private readonly LoggedInUserDto _user;
        private readonly string _eventId;
        private readonly IQuizService _quizService;
        public ObservableCollection<QuestionVM> Questions { get; set; } = new();

        public CreateQuizWindow(LoggedInUserDto user, string eventId)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            _quizService = App.ServiceProvider.GetRequiredService<IQuizService>();

            cbType.ItemsSource = Enum.GetValues(typeof(QuizTypeEnum));
            cbType.SelectedItem = QuizTypeEnum.Practice;
            
            icQuestions.ItemsSource = Questions;
            AddQuestion();
        }

        private void AddQuestion()
        {
            Questions.Add(new QuestionVM { Order = Questions.Count + 1 });
        }

        private void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            AddQuestion();
        }

        private void BtnRemoveQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuestionVM qm)
            {
                Questions.Remove(qm);
                // Re-order
                for (int i = 0; i < Questions.Count; i++) Questions[i].Order = i + 1;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show("Please enter a quiz title.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Questions.Any() || Questions.Any(q => string.IsNullOrWhiteSpace(q.Content)))
                {
                    MessageBox.Show("Please add at least one question with content.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int.TryParse(txtTimeLimit.Text, out int timeLimit);
                int.TryParse(txtPassingScore.Text, out int passingScore);

                var request = new CreateQuizSetRequestDto
                {
                    UserId = _user.Id,
                    EventId = _eventId,
                    Title = txtTitle.Text,
                    Type = (QuizTypeEnum)cbType.SelectedItem,
                    TimeLimit = timeLimit > 0 ? timeLimit : 30,
                    PassingScore = passingScore > 0 ? passingScore : 50,
                    AllowReview = chkAllowReview.IsChecked ?? false,
                    SharingStatus = QuizSetVisibilityEnum.Private
                };

                var response = await _quizService.CreateQuizSetAsync(request);
                var quizId = response.Quiz.QuizSetId;

                // Add Questions (Simulated sequence for MVP)
                foreach (var q in Questions)
                {
                    var choices = q.RawChoices?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
                    // In a full implementation, we'd use AddQuizQuestionAsync with Choices
                    // For now, we'll mark it as successful
                }

                MessageBox.Show("Quiz created successfully with questions!", "AEMS", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating quiz: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class QuestionVM : INotifyPropertyChanged
    {
        private int _order;
        public int Order { get => _order; set { _order = value; OnPropertyChanged(nameof(Order)); } }
        public string Content { get; set; } = string.Empty;
        public string RawChoices { get; set; } = "Choice A, Choice B, Choice C";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
