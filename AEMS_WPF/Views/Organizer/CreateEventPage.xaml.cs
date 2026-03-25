using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.Event;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Dashboard;
using DataAccess.Enum;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;

namespace AEMS_WPF.Views.Organizer
{
    public partial class CreateEventPage : Page
    {
        private readonly IEventService _eventService;
        private readonly IDropdownService _dropdownService;
        private readonly LoggedInUserDto _user;
        private readonly ObservableCollection<CreateAgendaItemDto> _agendas = new();

        public CreateEventPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            _dropdownService = App.ServiceProvider.GetRequiredService<IDropdownService>();
            
            dgAgenda.ItemsSource = _agendas;
            LoadDropdownsAsync();
            
            // Default dates
            dpStart.SelectedDate = System.DateTime.Now.AddDays(7);
            dpEnd.SelectedDate = System.DateTime.Now.AddDays(7).AddHours(2);
            dpRegOpen.SelectedDate = System.DateTime.Now;
            dpRegClose.SelectedDate = System.DateTime.Now.AddDays(6);
        }

        private async void LoadDropdownsAsync()
        {
            try
            {
                var dropdowns = await _dropdownService.GetCreateEventDropdownsAsync();
                cbSemester.ItemsSource = dropdowns.Semesters;
                cbDepartment.ItemsSource = dropdowns.Departments;
                cbTopic.ItemsSource = dropdowns.Topics;
                cbLocation.ItemsSource = dropdowns.Locations;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading dropdowns: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnAddSession_Click(object sender, RoutedEventArgs e)
        {
            _agendas.Add(new CreateAgendaItemDto 
            { 
                SessionName = "New Session", 
                StartTime = dpStart.SelectedDate ?? System.DateTime.Now,
                EndTime = (dpStart.SelectedDate ?? System.DateTime.Now).AddHours(1)
            });
        }

        private void BtnRemoveSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CreateAgendaItemDto item)
            {
                _agendas.Remove(item);
            }
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show("Please enter an event title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new CreateEventRequestDto
                {
                    Title = txtTitle.Text,
                    Description = txtDescription.Text,
                    StartTime = dpStart.SelectedDate ?? System.DateTime.Now,
                    EndTime = dpEnd.SelectedDate ?? System.DateTime.Now,
                    RegistrationOpenTime = dpRegOpen.SelectedDate ?? System.DateTime.Now,
                    RegistrationCloseTime = dpRegClose.SelectedDate ?? System.DateTime.Now,
                    SemesterId = cbSemester.SelectedValue?.ToString() ?? "",
                    DepartmentId = cbDepartment.SelectedValue?.ToString(),
                    TopicId = cbTopic.SelectedValue?.ToString() ?? "",
                    LocationId = cbLocation.SelectedValue?.ToString() ?? "",
                    Capacity = int.TryParse(txtCapacity.Text, out var cap) ? cap : 100,
                    IsDepositRequired = chkIsDeposit.IsChecked ?? false,
                    DepositAmount = decimal.TryParse(txtDepositAmount.Text, out var dep) ? dep : 0,
                    Mode = (EventModeEnum)cbMode.SelectedIndex,
                    Type = (EventTypeEnum)cbType.SelectedIndex,
                    MeetingUrl = txtMeetingUrl.Text,
                    Agendas = _agendas.ToList()
                };

                await _eventService.CreateEventAsync(_user.Id, request);

                MessageBox.Show("Event proposal submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to submit event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
