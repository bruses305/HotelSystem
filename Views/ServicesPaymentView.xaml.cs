using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class ServicesPaymentView : Page
{
    private readonly IFinanceService _financeService;
    private List<ServicePaymentDisplay> _allPayments = new();

    public ServicesPaymentView()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        Loaded += ServicesPaymentView_Loaded;
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.ServicesPayment) && FindName("AddServicePaymentButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ServicesPaymentView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPaymentsAsync();
    }

    private async void LoadPaymentsAsync()
    {
        try
        {
            var transactions = await _financeService.GetTransactionsAsync();
            var serviceTransactions = transactions
                .Where(t => t.Category == TransactionCategory.AdditionalService)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            _allPayments = serviceTransactions.Select(t => new ServicePaymentDisplay
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Quantity = t.Quantity,
                Amount = t.Amount,
                ClientName = t.Booking?.Client?.FullName ?? "",
                RoomName = t.Booking?.Room?.Name ?? "",
                ServiceName = t.Service?.Name ?? ""
            }).ToList();
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilters()
    {
        var startDate = StartDatePicker.SelectedDate;
        var endDate = EndDatePicker.SelectedDate;
        var searchQuery = SearchTextBox.Text;
        
        var filtered = _allPayments.AsQueryable();
        
        if (startDate.HasValue)
            filtered = filtered.Where(p => p.TransactionDate >= startDate.Value);
        
        if (endDate.HasValue)
            filtered = filtered.Where(p => p.TransactionDate <= endDate.Value.AddDays(1));
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var query = searchQuery.Trim().ToLower();
            filtered = filtered.Where(p =>
                p.Id.ToString().Contains(query) ||
                p.ClientName.ToLower().Contains(query) ||
                p.ServiceName.ToLower().Contains(query) ||
                p.TransactionDate.ToString("dd.MM.yyyy").Contains(query)
            );
        }
        
        PaymentsGrid.ItemsSource = filtered.ToList();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFilters();
        }
    }

    private void DateFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        StartDatePicker.SelectedDate = null;
        EndDatePicker.SelectedDate = null;
        ApplyFilters();
    }

    private async void AddServicePayment_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.ServicesPayment))
        {
            MessageBox.Show("Недостаточно прав для создания оплаты услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var dialog = new ServicePaymentDialog();
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow != null)
            {
                dialog.Owner = mainWindow;
            }
            
            if (dialog.ShowDialog() == true)
            {
                await _financeService.RecordServicePaymentAsync(dialog.BookingId, dialog.ServiceId, dialog.Quantity, dialog.Amount);
                LoadPaymentsAsync();
                MessageBox.Show($"Услуга была успешно добавленна в количестве: {dialog.Amount:N0} штук", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PaymentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Для услуг редактирование через диалог не реализовано, показываем информацию
        if (PaymentsGrid.SelectedItem is ServicePaymentDisplay payment)
        {
            MessageBox.Show(
                $"Услуга: {payment.ServiceName}\n" +
                $"Клиент: {payment.ClientName}\n" +
                $"Номер: {payment.RoomName}\n" +
                $"Количество: {payment.Quantity}\n" +
                $"Сумма: {payment.Amount:N0}\n" +
                $"Дата: {payment.TransactionDate:dd.MM.yyyy HH:mm}",
                "Информация об услуге",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}



