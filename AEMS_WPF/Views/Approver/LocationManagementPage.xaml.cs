using BusinessLogic.DTOs.Event.Location;
using BusinessLogic.Service.Event.Sub_Service.Location;
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
    public partial class LocationManagementPage : Page
    {
        private readonly ILocationService _locationService;
        private readonly Dashboard.ApproveDashBoard? _dashboard;
        private List<LocationDTO> _allLocations = new();
        private LocationDTO? _editingDto = null;

        public LocationManagementPage(Dashboard.ApproveDashBoard? dashboard = null)
        {
            InitializeComponent();
            _dashboard = dashboard;
            _locationService = App.ServiceProvider.GetRequiredService<ILocationService>();

            // Init ComboBoxes
            frmStatus.ItemsSource = Enum.GetValues(typeof(LocationStatusEnum));
            frmStatus.SelectedIndex = 0;

            var typeItems = new List<object> { "(None)" };
            typeItems.AddRange(Enum.GetValues(typeof(LocationTypeEnum)).Cast<object>());
            frmType.ItemsSource = typeItems;
            frmType.SelectedIndex = 0;

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var list = await _locationService.GetAllLocationsAsync();
                _allLocations = list ?? new List<LocationDTO>();
                UpdateUI(_allLocations);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI(List<LocationDTO> data)
        {
            icLocations.ItemsSource = null;
            icLocations.ItemsSource = data;
            txtTotalCount.Text = data.Count.ToString();
            txtAvailableCount.Text = data.Count(x => x.Status == LocationStatusEnum.Available).ToString();
            txtMaintenanceCount.Text = data.Count(x => x.Status == LocationStatusEnum.Maintenance).ToString();
            txtTotalCapacity.Text = data.Sum(x => x.Capacity).ToString();
        }

        // ===== FORM HELPERS =====
        private void ShowForm(string title, LocationDTO? dto = null)
        {
            _editingDto = dto;
            txtFormTitle.Text = title;

            if (dto != null)
            {
                frmName.Text = dto.Name;
                frmAddress.Text = dto.Address;
                frmCapacity.Text = dto.Capacity.ToString();
                frmDescription.Text = dto.Description;
                frmStatus.SelectedItem = dto.Status;
                frmType.SelectedItem = dto.Type.HasValue ? (object)dto.Type.Value : "(None)";
            }
            else
            {
                frmName.Text = "";
                frmAddress.Text = "";
                frmCapacity.Text = "";
                frmDescription.Text = "";
                frmStatus.SelectedIndex = 0;
                frmType.SelectedIndex = 0;
            }

            FormPanel.Visibility = Visibility.Visible;
            frmName.Focus();
        }

        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
            _editingDto = null;
        }

        // ===== BUTTON HANDLERS =====
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ShowForm("Add New Location");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dto = (sender as Button)?.Tag as LocationDTO;
            if (dto == null) return;
            ShowForm("Edit Location", dto);
        }

        private void BtnFormCancel_Click(object sender, RoutedEventArgs e)
        {
            HideForm();
        }

        private async void BtnFormSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(frmName.Text))
            {
                MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(frmAddress.Text))
            {
                MessageBox.Show("Address is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(frmCapacity.Text, out int capacity) || capacity <= 0)
            {
                MessageBox.Show("Capacity must be a positive number.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LocationTypeEnum? selectedType = frmType.SelectedItem is LocationTypeEnum t ? t : null;

            try
            {
                btnFormSave.IsEnabled = false;

                if (_editingDto != null)
                {
                    var updateDto = new UpdateLocationDTO
                    {
                        Name = frmName.Text.Trim(),
                        Address = frmAddress.Text.Trim(),
                        Capacity = capacity,
                        Status = (LocationStatusEnum)frmStatus.SelectedItem,
                        Type = selectedType,
                        Description = frmDescription.Text.Trim()
                    };
                    var result = await _locationService.UpdateLocationAsync(_editingDto.LocationId, updateDto);
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
                    var createDto = new CreateLocationDTO
                    {
                        Name = frmName.Text.Trim(),
                        Address = frmAddress.Text.Trim(),
                        Capacity = capacity,
                        Status = (LocationStatusEnum)frmStatus.SelectedItem,
                        Type = selectedType,
                        Description = frmDescription.Text.Trim()
                    };
                    await _locationService.CreateLocationAsync(createDto);
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
            var dto = (sender as Button)?.Tag as LocationDTO;
            if (dto == null) return;

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa '{dto.Name}'?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _locationService.DeleteLocationAsync(dto.LocationId);
                if (result) await LoadDataAsync();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            var filtered = _allLocations.Where(x =>
                x.Name.ToLower().Contains(keyword) ||
                x.Address.ToLower().Contains(keyword)
            ).ToList();
            UpdateUI(filtered);
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            await LoadDataAsync();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_dashboard != null)
                _dashboard.NavigateBackToDashboard();
            else
                Window.GetWindow(this)?.Close();
        }
    }
}