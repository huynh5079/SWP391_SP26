using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Organizer.CheckIn;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class TeamManagementPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly IEventService _eventService;
        private ObservableCollection<EventTeamDto> _teams = new();

        public TeamManagementPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = $"Teams: {eventTitle}";
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            
            icTeams.ItemsSource = _teams;
            LoadTeams();
        }

        private async void LoadTeams()
        {
            try
            {
                var teams = await _eventService.GetEventTeamsAsync(_eventId.ToString());
                _teams.Clear();
                foreach (var team in teams)
                {
                    _teams.Add(team);
                }
                txtTotalTeams.Text = _teams.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading teams: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void BtnAddTeam_Click(object sender, RoutedEventArgs e)
        {
            // Simple dialog to enter team name
            string teamName = Microsoft.VisualBasic.Interaction.InputBox("Enter Team Name:", "New Team", $"Team {_teams.Count + 1}");
            if (string.IsNullOrWhiteSpace(teamName)) return;

            try
            {
                var success = await _eventService.CreateEventTeamAsync(_eventId.ToString(), teamName, "");
                if (success)
                {
                    LoadTeams();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating team: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Team randomization functionality will be available in the next update.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnDeleteTeam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EventTeamDto team)
            {
                var result = MessageBox.Show($"Delete team '{team.TeamName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _eventService.DeleteEventTeamAsync(team.Id);
                        LoadTeams();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnEditScore_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EventTeamDto team)
            {
                // Simple score edit
                string scoreStr = Microsoft.VisualBasic.Interaction.InputBox($"Enter score for {team.TeamName}:", "Edit Score", team.Score.ToString());
                if (decimal.TryParse(scoreStr, out decimal newScore))
                {
                    // Note: IEventService might need a method to update team score
                    // For now we simulate or use a specific update method if available
                    MessageBox.Show("Score update would call EventService.UpdateTeamScoreAsync here.", "Note");
                }
            }
        }

        private async void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EventTeamDto team)
            {
                try
                {
                    var checkInService = App.ServiceProvider.GetRequiredService<ICheckInService>();
                    var participants = await checkInService.GetParticipantsAsync(_eventId.ToString());
                    
                    // Filter out participants already in this team (optional but good)
                    var existingMemberIds = team.TeamMembers.Select(m => m.StudentId).ToList();
                    var available = participants.Where(p => !existingMemberIds.Contains(p.StudentId)).ToList();

                    var selectionWindow = new ParticipantSelectionWindow(available) { Owner = Window.GetWindow(this) };
                    if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedParticipant != null)
                    {
                        var student = selectionWindow.SelectedParticipant;
                        await _eventService.AddMemberToTeamAsync(team.Id, student.StudentId, null, "Member");
                        LoadTeams(); // Refresh
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private async void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TeamMemberDto member)
            {
                try
                {
                    await _eventService.RemoveMemberFromTeamAsync(member.Id);
                    LoadTeams();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
