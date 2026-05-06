using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ForecastWindow : Window
{
    private readonly IFinanceService _financeService;
    private readonly IRoomService _roomService;
    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    
    private List<ForecastBookingItem> _forecastBookings = new();
    private List<ForecastServiceItem> _forecastServices = new();

    public ForecastWindow()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        
        // Автоматический расчёт при загрузке
        CalculateForecast();
    }

    private void CalculateForecast()
    {
        var result = CalculateDetailedForecastAsync().GetAwaiter().GetResult();
        DisplayDetailedForecast(result);
    }

    private async Task<ForecastResult> CalculateDetailedForecastAsync()
    {
        var totalIncome = 0m;
        var totalExpenses = 0m;
        
        // Расчёт доходов от бронирований
        foreach (var booking in _forecastBookings)
        {
            var room = booking.Room ?? await _roomService.GetRoomByIdAsync(booking.RoomId);
            if (room != null)
            {
                var income = room.Price * booking.StayDays * booking.Count;
                totalIncome += income;
                
                // Реальные расходы на номер (вода + свет + интернет + уборка) * дни * количество
                var roomExpenses = room.TotalExpenses * booking.StayDays * booking.Count;
                totalExpenses += roomExpenses;
                
                System.Diagnostics.Debug.WriteLine($"Номер {room.Name}: Доход={income:N0}, Расходы={roomExpenses:N0} (ставка={room.TotalExpenses:F0}/ночь)");
            }
        }
        
        // Расчёт доходов от услуг
        foreach (var service in _forecastServices)
        {
            var serviceData = service.Service;
            if (serviceData != null)
            {
                var income = serviceData.Price * service.Count;
                totalIncome += income;
                
                // Расходы на услуги = 0
                var serviceExpenses = 0m;
                totalExpenses += serviceExpenses;
                
                System.Diagnostics.Debug.WriteLine($"Услуга {serviceData.Name} (ID:{serviceData.Id}): Цена={serviceData.Price}, Количество={service.Count}, Доход={income:N0}, Расходы=0");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Услуга не найдена! ServiceId={service.ServiceId}");
            }
        }
        
        var profit = totalIncome - totalExpenses;
        
        System.Diagnostics.Debug.WriteLine($"ИТОГО: Доход={totalIncome:N0}, Расход={totalExpenses:N0}, Прибыль={profit:N0}");
        
        return new ForecastResult
        {
            Income = totalIncome,
            Expenses = totalExpenses,
            Profit = profit,
            Details = BuildForecastDetails()
        };
    }

    private string BuildForecastDetails()
    {
        var details = new StringBuilder();
        
        if (_forecastBookings.Any())
        {
            details.AppendLine("📅 Бронирования номеров:");
            details.AppendLine(new string('-', 40));
            
            foreach (var booking in _forecastBookings)
            {
                var room = booking.Room;
                var roomName = room?.Name ?? $"Номер {booking.RoomId}";
                var income = (room?.Price ?? 0) * booking.StayDays * booking.Count;
                var expenses = (room?.TotalExpenses ?? 0) * booking.StayDays * booking.Count;
                var profit = income - expenses;
                details.AppendLine($"• {roomName} - {booking.Count}x{booking.StayDays}дней");
                details.AppendLine($"  Доход: {income:N0} Br | Расходы: {expenses:N0} Br | Прибыль: {profit:N0} Br");
            }
            
            details.AppendLine();
        }
        
        if (_forecastServices.Any())
        {
            details.AppendLine("🧳 Услуги:");
            details.AppendLine(new string('-', 40));
            
            foreach (var service in _forecastServices)
            {
                var serviceData = service.Service;
                var serviceName = serviceData?.Name ?? $"Услуга {service.ServiceId}";
                var income = (serviceData?.Price ?? 0) * service.Count;
                var expenses = 0m;
                var profit = income - expenses;
                details.AppendLine($"• {serviceName} - {service.Count} раз(а)");
                details.AppendLine($"  Доход: {income:N0} Br | Расходы: 0 Br | Прибыль: {profit:N0} Br");
            }
        }
        
        return details.ToString();
    }

    private void DisplayDetailedForecast(ForecastResult result)
    {
        ForecastIncomeText.Text = $"{result.Income:N0} Br";
        ForecastExpenseText.Text = $"{result.Expenses:N0} Br";
        ForecastProfitText.Text = $"{result.Profit:N0} Br";
        
        ForecastDetailsPanel.Children.Clear();
        
        var detailsText = new TextBlock
        {
            Text = result.Details,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 15)
        };
        ForecastDetailsPanel.Children.Add(detailsText);
        
        var summaryText = new TextBlock
        {
            Text = $"Всего бронирований: {_forecastBookings.Sum(b => b.Count)}\n" +
                   $"Всего услуг: {_forecastServices.Sum(s => s.Count)}\n" +
                   $"Маржинальность: {(result.Income > 0 ? (result.Profit / result.Income * 100): 0):F1}%",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 0)
        };
        ForecastDetailsPanel.Children.Add(summaryText);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddBookingButton_Click(object sender, RoutedEventArgs e)
    {
        var rooms = _roomService.GetAllRoomsAsync().GetAwaiter().GetResult();
        var roomList = rooms.Select(r => $"{r.Name} ({r.Price} Br/ночь)").ToList();
        
        var result = Microsoft.VisualBasic.Interaction.InputBox(
            $"Выберите номер (например, 1, 2, 3):\n{string.Join("\n", roomList.Select((r, i) => $"{i + 1}. {r}"))}\n\n" +
            $"Введите формат: \"номер_количество_дней\"\nПример: 1_3_5 (номер 1, 3 раза, по 5 дней)",
            "Добавить бронирование", "");
        
        if (string.IsNullOrWhiteSpace(result)) return;
        
        var parts = result.Split('_');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var roomNum) || 
            !int.TryParse(parts[1], out var count) || !int.TryParse(parts[2], out var days))
        {
            MessageBox.Show("Неверный формат! Используйте: номер_количество_дней\nПример: 1_3_5", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var room = rooms.ElementAtOrDefault(roomNum - 1);
        if (room == null)
        {
            MessageBox.Show("Номер не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _forecastBookings.Add(new ForecastBookingItem
        {
            RoomId = room.Id,
            Room = room,
            Count = count,
            StayDays = days
        });
        
        RefreshBookingList();
        CalculateForecast(); // Автоматический расчёт
    }

    private async void AddServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var services = await _serviceService.GetAllServicesAsync();
        var serviceList = services.Where(s => s.IsActive).Select(s => $"{s.Name} ({s.Price} Br)").ToList();
        
        if (!serviceList.Any())
        {
            MessageBox.Show("Нет доступных услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = Microsoft.VisualBasic.Interaction.InputBox(
            $"Выберите услугу (например, 1, 2, 3):\n{string.Join("\n", serviceList.Select((r, i) => $"{i + 1}. {r}"))}\n\n" +
            $"Введите формат: \"номер_количество\"\nПример: 2_5 (услуга 2, 5 раз)",
            "Добавить услугу", "");
        
        if (string.IsNullOrWhiteSpace(result)) return;
        
        var parts = result.Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var serviceNum) || 
            !int.TryParse(parts[1], out var count))
        {
            MessageBox.Show("Неверный формат! Используйте: номер_количество\nПример: 2_5", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var service = services.Where(s => s.IsActive).ElementAtOrDefault(serviceNum - 1);
        if (service == null)
        {
            MessageBox.Show("Услуга не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _forecastServices.Add(new ForecastServiceItem
        {
            ServiceId = service.Id,
            Service = service,
            Count = count
        });
        
        RefreshServiceList();
        CalculateForecast(); // Автоматический расчёт
    }

    private void RefreshBookingList()
    {
        BookingItemsPanel.Children.Clear();
        
        var bookingsCopy = _forecastBookings.ToList();
        
        for (int i = 0; i < bookingsCopy.Count; i++)
        {
            var booking = bookingsCopy[i];
            var originalIndex = _forecastBookings.IndexOf(booking);
            var room = booking.Room;
            var income = (room?.Price ?? 0) * booking.StayDays * booking.Count;
            var expenses = (room?.TotalExpenses ?? 0) * booking.StayDays * booking.Count;
            var profit = income - expenses;
            
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            var textBlock = new TextBlock
            {
                Text = $"{room?.Name} - {booking.Count}x{booking.StayDays}д = {income:N0} Br (расход: {expenses:N0}, прибыль: {profit:N0})",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            var removeButton = new Button
            {
                Content = "❌",
                Width = 30,
                Height = 30,
                Background = Brushes.LightCoral,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            var indexToRemove = originalIndex;
            removeButton.Click += (s, e) =>
            {
                _forecastBookings.RemoveAt(indexToRemove);
                RefreshBookingList();
                CalculateForecast();
            };
            
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(removeButton);
            BookingItemsPanel.Children.Add(stackPanel);
        }
    }

    private void RefreshServiceList()
    {
        ServiceItemsPanel.Children.Clear();
        
        var servicesCopy = _forecastServices.ToList();
        
        for (int i = 0; i < servicesCopy.Count; i++)
        {
            var service = servicesCopy[i];
            var originalIndex = _forecastServices.IndexOf(service);
            var serviceData = service.Service;
            var income = (serviceData?.Price ?? 0) * service.Count;
            var expenses = 0m;
            var profit = income - expenses;
            
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            var textBlock = new TextBlock
            {
                Text = $"{serviceData?.Name} - {service.Count} раз = {income:N0} Br (расход: 0, прибыль: {profit:N0})",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            var removeButton = new Button
            {
                Content = "❌",
                Width = 30,
                Height = 30,
                Background = Brushes.LightCoral,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            var indexToRemove = originalIndex;
            removeButton.Click += (s, e) =>
            {
                _forecastServices.RemoveAt(indexToRemove);
                RefreshServiceList();
                CalculateForecast();
            };
            
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(removeButton);
            ServiceItemsPanel.Children.Add(stackPanel);
        }
    }
}

public class ForecastBookingItem
{
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public int Count { get; set; }
    public int StayDays { get; set; }
}

public class ForecastServiceItem
{
    public int ServiceId { get; set; }
    public Models.Entities.Service? Service { get; set; }
    public int Count { get; set; }
}

public class ForecastResult
{
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public string Details { get; set; } = string.Empty;
}
