using AEMS_WPF.Views.Dashboard;
using BusinessLogic.DTOs.Event.Semester;
using BusinessLogic.Service.Event.Sub_Service.Semester;
using DataAccess.Enum;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Approver
{
    public partial class SemesterManagementPage : Page
    {
        private readonly ISemesterService _semesterService;
        private readonly ApproveDashBoard? _dashboard;
        private List<SemesterDTO> _allSemesters = new();
        private SemesterDTO? _editingDto = null;

        public SemesterManagementPage(ApproveDashBoard? dashboard = null)
        {
            InitializeComponent();
            _dashboard = dashboard;
            _semesterService = App.ServiceProvider.GetRequiredService<ISemesterService>();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var list = await _semesterService.GetAllSemestersAsync();
                _allSemesters = list ?? new List<SemesterDTO>();
                UpdateUI(_allSemesters);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI(List<SemesterDTO> data)
        {
            icSemesters.ItemsSource = null;
            icSemesters.ItemsSource = data;
            txtTotalCount.Text = data.Count.ToString();
            txtActiveCount.Text = data.Count(x => x.Status == SemesterStatusEnum.Active).ToString();
            txtUpcomingCount.Text = data.Count(x => x.Status == SemesterStatusEnum.Upcoming).ToString();
            txtFinishedCount.Text = data.Count(x => x.Status == SemesterStatusEnum.Finished).ToString();
        }

        // ===== FORM =====
        private void ShowForm(string title, SemesterDTO? dto = null)
        {
            _editingDto = dto;
            txtFormTitle.Text = title;

            if (dto != null)
            {
                frmName.Text = dto.Name ?? "";
                frmCode.Text = dto.Code ?? "";
                frmYear.Text = dto.year.ToString();
                frmStartDate.Text = dto.StartDate?.ToString("yyyy-MM-dd") ?? "";
                frmEndDate.Text = dto.EndDate?.ToString("yyyy-MM-dd") ?? "";
            }
            else
            {
                frmName.Text = "";
                frmCode.Text = "";
                frmYear.Text = DateTime.Now.Year.ToString();
                frmStartDate.Text = "";
                frmEndDate.Text = "";
            }

            FormPanel.Visibility = Visibility.Visible;
            frmName.Focus();
        }

        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            _editingDto = null;
        }

        // ===== HANDLERS =====
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ShowForm("Add New Semester");
        }

        private async void BtnAutoCreate_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Auto-create next semester?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var result = await _semesterService.AutoCreateSemesterAsync();
                MessageBox.Show($"Created: {result.Name}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dto = (sender as Button)?.Tag as SemesterDTO;
            if (dto == null) return;
            ShowForm("Edit Semester", dto);
        }

        private void BtnFormCancel_Click(object sender, RoutedEventArgs e)
        {
            HideForm();
        }

        private async void BtnFormSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(frmName.Text))
            {
                MessageBox.Show("Name is required (Spring/Summer/Fall).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(frmCode.Text))
            {
                MessageBox.Show("Code is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(frmYear.Text, out int year) || year < 2000)
            {
                MessageBox.Show("Year must be a valid number (e.g. 2025).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!DateTime.TryParse(frmStartDate.Text, out DateTime startDate))
            {
                MessageBox.Show("Start Date is invalid. Use yyyy-MM-dd.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!DateTime.TryParse(frmEndDate.Text, out DateTime endDate))
            {
                MessageBox.Show("End Date is invalid. Use yyyy-MM-dd.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (endDate < startDate)
            {
                MessageBox.Show("End Date must be after Start Date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dto = new SemesterDTO
            {
                Name = frmName.Text.Trim(),
                Code = frmCode.Text.Trim(),
                year = year,
                StartDate = startDate,
                EndDate = endDate,
                Status = SemesterStatusEnum.Upcoming
            };

            try
            {
                btnFormSave.IsEnabled = false;

                if (_editingDto != null)
                {
                    var result = await _semesterService.UpdateSemesterAsync(_editingDto.SemesterId!, dto);
                    if (result)
                    {
                        HideForm();
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Update failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    await _semesterService.CreateSemesterAsync(dto);
                    HideForm();
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnFormSave.IsEnabled = true;
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var dto = (sender as Button)?.Tag as SemesterDTO;
            if (dto == null) return;

            var confirm = MessageBox.Show($"Xóa semester '{dto.Name}'?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    await _semesterService.DeleteSemesterAsync(dto.SemesterId!);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            var filtered = _allSemesters.Where(x =>
                (x.Name?.ToLower().Contains(keyword) ?? false) ||
                (x.Code?.ToLower().Contains(keyword) ?? false)
            ).ToList();
            UpdateUI(filtered);
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            await LoadDataAsync();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
            => Window.GetWindow(this)?.Close();
    }
}