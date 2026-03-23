using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Repositories;
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
            MessageBox.Show($"Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В° Р В Р’В·Р В Р’В°Р В РЎвЂ“Р РЋР вЂљР РЋРЎвЂњР В Р’В·Р В РЎвЂќР В РЎвЂ: {ex.Message}", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddServicePayment_Click(object sender, RoutedEventArgs e)
    {
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
                MessageBox.Show($"Р В Р в‚¬Р РЋР С“Р В Р’В»Р РЋРЎвЂњР В РЎвЂ“Р В Р’В° Р В РЎвЂўР В РЎвЂ”Р В Р’В»Р В Р’В°Р РЋРІР‚РЋР В Р’ВµР В Р вЂ¦Р В Р’В°! Р В Р Р‹Р РЋРЎвЂњР В РЎВР В РЎВР В Р’В°: {dialog.Amount:N0} Р Р†РІР‚С™Р вЂ¦", "Р В Р в‚¬Р РЋР С“Р В РЎвЂ”Р В Р’ВµР РЋРІР‚В¦", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°: {ex.Message}\n\n{ex.StackTrace}", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}



