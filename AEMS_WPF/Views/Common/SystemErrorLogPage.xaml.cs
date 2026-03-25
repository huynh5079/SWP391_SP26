using System.Windows.Controls;
using BusinessLogic.Service.System;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Common
{
    public partial class SystemErrorLogPage : Page
    {
        private readonly ISystemErrorLogService _errorLogService;

        public SystemErrorLogPage()
        {
            InitializeComponent();
            _errorLogService = App.ServiceProvider.GetRequiredService<ISystemErrorLogService>();
            LoadLogs();
        }

        private async void LoadLogs()
        {
            try
            {
                // var logs = await _errorLogService.GetAllLogsAsync();
                // dgLogs.ItemsSource = logs;
            }
            catch { }
        }
    }
}
