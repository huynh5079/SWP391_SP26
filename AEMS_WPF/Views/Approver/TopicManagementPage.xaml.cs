using AEMS_WPF.Views.Dashboard;
using BusinessLogic.DTOs.Event.Topic;
using BusinessLogic.Service.Event.Sub_Service.Topic;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Approver
{
    public partial class TopicManagementPage : Page
    {
        private readonly ITopicService _topicService;
        private List<TopicDTO> _allTopics = new();
        private readonly ApproveDashBoard? _dashboard;

        // Quản lý ID đang sửa. Nếu null = đang thêm mới.
        private string? _editingTopicId = null;

        public TopicManagementPage(ApproveDashBoard? dashboard = null)
        {
            InitializeComponent();
            _dashboard = dashboard;
            _topicService = App.ServiceProvider.GetRequiredService<ITopicService>();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var list = await _topicService.GetAllTopicsAsync();
                _allTopics = list ?? new List<TopicDTO>();
                UpdateUI(_allTopics);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI(List<TopicDTO> data)
        {
            icTopics.ItemsSource = null;
            icTopics.ItemsSource = data;

            var now = DateTime.Now;
            txtTotalCount.Text = data.Count.ToString();
            txtThisMonthCount.Text = data.Count(x => x.CreatedAt.Month == now.Month && x.CreatedAt.Year == now.Year).ToString();
            txtRecentlyUpdated.Text = data.Count(x => x.UpdatedAt >= now.AddDays(-7)).ToString();
        }

        // --- Logic Inline Editor ---

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingTopicId = null; // Chế độ thêm mới
            lblEditorTitle.Text = "Add New Topic";
            inputName.Text = string.Empty;
            inputDescription.Text = string.Empty;
            EditorPanel.Visibility = Visibility.Visible;
            inputName.Focus();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dto = (sender as Button)?.Tag as TopicDTO;
            if (dto == null) return;

            _editingTopicId = dto.TopicId; // Chế độ chỉnh sửa
            lblEditorTitle.Text = $"Edit Topic: {dto.TopicName}";
            inputName.Text = dto.TopicName;
            inputDescription.Text = dto.Description;

            EditorPanel.Visibility = Visibility.Visible;
            inputName.Focus();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            EditorPanel.Visibility = Visibility.Collapsed;
            _editingTopicId = null;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = inputName.Text.Trim();
            string desc = inputDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Vui lòng nhập tên Topic.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool isSuccess = false;

                if (_editingTopicId == null)
                {
                    // Thực hiện Thêm mới
                    var createDto = new CreateTopicDTO { TopicName = name, Description = desc };
                    var result = await _topicService.CreateTopicAsync(createDto);
                    isSuccess = result != null;
                }
                else
                {
                    // Thực hiện Cập nhật
                    var updateDto = new UpdateTopicDTO { TopicName = name, Description = desc };
                    isSuccess = await _topicService.UpdateTopicAsync(_editingTopicId, updateDto);
                }

                if (isSuccess)
                {
                    EditorPanel.Visibility = Visibility.Collapsed;
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show("Thao tác thất bại. Vui lòng kiểm tra lại (Tên có thể đã tồn tại).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Các hàm phụ trợ khác ---

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            var filtered = _allTopics.Where(x =>
                x.TopicName.ToLower().Contains(keyword) ||
                (x.Description?.ToLower().Contains(keyword) ?? false)
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
            if (_dashboard != null) _dashboard.NavigateBackToDashboard();
            else Window.GetWindow(this)?.Close();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var dto = (sender as Button)?.Tag as TopicDTO;
            if (dto == null) return;

            var confirm = MessageBox.Show($"Xóa topic '{dto.TopicName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                var result = await _topicService.DeleteTopicAsync(dto.TopicId);
                if (result) await LoadDataAsync();
                else MessageBox.Show("Không thể xóa (đang được sử dụng).");
            }
        }
    }
}