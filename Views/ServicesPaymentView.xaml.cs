using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public class ServicePaymentDisplay
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string ClientName { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

public partial class ServicesPaymentView : Page
{
    private readonly IFinanceService _financeService;

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

            var displayItems = serviceTransactions.Select(t => new ServicePaymentDisplay
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Quantity = t.Quantity,
                Amount = t.Amount,
                ClientName = t.Booking?.Client?.FullName ?? "",
                RoomName = t.Booking?.Room?.Name ?? "",
                ServiceName = t.Service?.Name ?? ""
            }).ToList();
            
            PaymentsGrid.ItemsSource = displayItems;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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



