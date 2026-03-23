using System.Windows;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public class BookingDisplay
{
    public int Id { get; set; }
    public string DisplayInfo { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string ClientName { get; set; } = "";
}

public partial class ServicePaymentDialog : Window
{
    public int BookingId { get; private set; }
    public int ServiceId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Amount { get; private set; }

    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    private List<Service> _services = new();
    private List<BookingDisplay> _bookings = new();

    public ServicePaymentDialog()
    {
        InitializeComponent();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        Loaded += ServicePaymentDialog_Loaded;
    }

    private async void ServicePaymentDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var bookings = await _bookingService.GetAllBookingsWithDetailsAsync();
            _bookings = bookings
                .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid)
                .Select(b => new BookingDisplay
                {
                    Id = b.Id,
                    DisplayInfo = $"{b.Room?.Name ?? " Номер"} - {b.Client?.FullName ?? "Имя Клиента™"} ({b.CheckInDate:dd.MM}-{b.CheckOutDate:dd.MM})",
                    RoomName = b.Room?.Name ?? "",
                    ClientName = b.Client?.FullName ?? ""
                })
                .ToList();
            BookingComboBox.ItemsSource = _bookings;

            var services = await _serviceService.GetAllServicesAsync();
            _services = services?.ToList() ?? new List<Service>();
            ServiceComboBox.ItemsSource = _services;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки данных сервисов¦: {ex.Message}", "Ошибка°", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BookingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTotal();
    }

    private void ServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTotal();
    }

    private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        if (TotalText == null || ServiceComboBox == null || QuantityTextBox == null)
            return;
        
        try
        {
            if (ServiceComboBox.SelectedItem is Service service && int.TryParse(QuantityTextBox.Text, out var quantity))
            {
                var total = service.Price * quantity;
                TotalText.Text = $"{total:N0} Br";
            }
            else
            {
                TotalText.Text = "0 Br";
            }
        }
        catch
        {
            TotalText.Text = "0 Br";
        }
    }

    private void Pay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (BookingComboBox.SelectedItem is not BookingDisplay booking)
            {
                MessageBox.Show("Оплата прошла успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ServiceComboBox.SelectedItem is not Service service)
            {
                MessageBox.Show("Оплата прошла успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
            {
                MessageBox.Show("Оплата прошла успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BookingId = booking.Id;
            ServiceId = service.Id;
            Quantity = quantity;
            Amount = service.Price * quantity;
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Оплата не прошла" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

