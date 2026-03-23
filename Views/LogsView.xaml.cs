using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Repositories;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class LogsView : Page
{
    private readonly ILogService _logService;

    public LogsView()
    {
        InitializeComponent();
        _logService = ServiceLocator.GetService<ILogService>();
        Loaded += LogsView_Loaded;
    }
        
    private void LogsView_Loaded(object sender, RoutedEventArgs e)
    {
        StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
        EndDatePicker.SelectedDate = DateTime.Today;
        LoadLogsAsync();
    }

    private async void LoadLogsAsync()
    {
        try
        {
            var logs = (await _logService.GetAllLogsAsync())
                .OrderByDescending(l => l.LogDate)
                .ToList();
            LogsGrid.ItemsSource = logs;
            
            if (TotalLogsText != null) TotalLogsText.Text = logs.Count.ToString();
            if (LowLogsText != null) LowLogsText.Text = logs.Count(l => l.Level == LogLevel.Low).ToString();
            if (MediumLogsText != null) MediumLogsText.Text = logs.Count(l => l.Level == LogLevel.Medium).ToString();
            if (CriticalLogsText != null) CriticalLogsText.Text = logs.Count(l => l.Level == LogLevel.Critical).ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки логов°: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            IEnumerable<SystemLog> logs;
            
            var levelIndex = LevelFilter?.SelectedIndex ?? 0;
            var startDate = StartDatePicker?.SelectedDate;
            var endDate = EndDatePicker?.SelectedDate;

            if (startDate.HasValue && endDate.HasValue)
            {
                logs = await _logService.GetLogsByDateRangeAsync(startDate.Value, endDate.Value.AddDays(1));
            }
            else
            {
                logs = await _logService.GetAllLogsAsync();
            }

            if (levelIndex > 0)
            {
                var level = (LogLevel)(levelIndex - 1);
                logs = logs.Where(l => l.Level == level);
            }

            LogsGrid.ItemsSource = logs.ToList();
        }
        catch
        {
            /*ignore*/
        }
    }

    private async void ClearOldLogs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ClearLogsDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                int oldLogCount = await _logService.DeleteOldLogsAsync(dialog.DaysToKeep);
                LoadLogsAsync();
                MessageBox.Show($"Очистка старых логов прошла успешно, логов удаленно: {oldLogCount}", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отчистке старых логов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


