using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class FinanceView : Page
{
    private readonly IFinanceService _financeService;

    public FinanceView()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        Loaded += FinanceView_Loaded;
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
        var dialog = new TransactionDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            _ = _financeService.AddTransactionAsync(dialog.Transaction);
            LoadDataAsync();
            MessageBox.Show("Транзакция успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

