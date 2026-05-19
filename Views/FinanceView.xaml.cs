using System.Windows;
using System.Windows.Input;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class FinanceView : Page
{
    private readonly IFinanceService _financeService;

    public FinanceView()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        Loaded += FinanceView_Loaded;
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Finance) && FindName("AddTransactionButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
        
        if (!PermissionChecker.HasPermission(PermissionCategory.Reports, PermissionType.Create) && FindName("ForecastButton") is Button forecastButton)
        {
            forecastButton.Visibility = Visibility.Collapsed;
        }
    }

    public bool CanView()
    {
        return PermissionChecker.CanView(PermissionCategory.Finance);
    }

    private void FinanceView_Loaded(object sender, RoutedEventArgs e)
    {
        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
        EndDatePicker.SelectedDate = DateTime.Today;
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        try
        {
            var startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);
            
            var income = await _financeService.GetTotalIncomeAsync(startDate, endDate);
            var expenses = await _financeService.GetTotalExpensesAsync(startDate, endDate);
            var profit = await _financeService.GetProfitAsync(startDate, endDate);
            IncomeText.Text = $"{income:N0}";
            ExpensesText.Text = $"{expenses:N0}";
            ProfitText.Text = $"{profit:N0}";
            TransactionsGrid.ItemsSource = await _financeService.GetTransactionsAsync(startDate, endDate);
        }
        catch (Exception ex) 
        {
            MessageBox.Show($"Ошибка:: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); 
        }
    }

    private void ApplyFilter_Click(object sender, RoutedEventArgs e) { LoadDataAsync(); }

    private async void AddTransaction_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Finance))
        {
            MessageBox.Show("Недостаточно прав для создания транзакций!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new TransactionDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            _ = _financeService.AddTransactionAsync(dialog.Transaction);
            LoadDataAsync();
            MessageBox.Show("Транзакция успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void TransactionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TransactionsGrid.SelectedItem is Transaction transaction)
        {
            MessageBox.Show(
                $"Дата: {transaction.TransactionDate:dd.MM.yyyy HH:mm}\n" +
                $"Тип: {transaction.Type}\n" +
                $"Категория: {transaction.Category}\n" +
                $"Сумма: {transaction.Amount}\n" +
                $"Описание: {transaction.Description ?? "-"}",
                "Информация о транзакции",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ForecastButton_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.HasPermission(PermissionCategory.Reports, PermissionType.Create))
        {
            MessageBox.Show("Недостаточно прав для использования прогнозов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var forecastWindow = new ForecastWindow();
        forecastWindow.Owner = Window.GetWindow(this);
        forecastWindow.ShowDialog();
    }
}

