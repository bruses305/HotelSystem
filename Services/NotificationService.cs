using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using HotelSystem.Models;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Services;

public class NotificationService
{
    private static readonly NotificationService _instance = new();
    private static ILogService ILogService => LogService.Instance;
    public static NotificationService Instance => _instance;
    
    public ObservableCollection<NotificationItem> Notifications { get; } = new();
    
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HotelSystem",
        "notifications.json"
    );
    
    public event Action? NotificationsChanged;
    
    private NotificationService()
    {
        LoadFromFile();
    }
    
    private void LoadFromFile()
    {
        try
        {
            Notifications.Clear();
            
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<NotificationItem>>(json);
                if (items != null)
                {
                    foreach (var item in items.OrderByDescending(n => n.CreatedAt))
                        Notifications.Add(item);
                }
            }
        }
        catch { /*ignore*/}
    }
    
    private void SaveToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            var json = JsonSerializer.Serialize(Notifications.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { /*ignore*/}
    }
    
    public void AddNotification(string title, string message, NotificationType type = NotificationType.Booking, int? bookingId = null)
    {
        var notification = new NotificationItem
        {
            Id = Notifications.Count > 0 ? Notifications.Max(n => n.Id) + 1 : 1,
            Title = title,
            Message = message,
            Type = type,
            BookingId = bookingId,
            CreatedAt = DateTime.Now,
            IsRead = false
        };
        
        Notifications.Insert(0, notification);
        SaveToFile();
        NotificationsChanged?.Invoke();
    }
    
    public int UnreadCount => Notifications.Count(n => !n.IsRead);
    
    public async Task MarkAsRead(int id)
    {
        var notif = Notifications.FirstOrDefault(n => n.Id == id);
        
        if (notif != null && !notif.IsRead)
        {
            notif.IsRead = true;
            await ILogService.LogAsync(LogLevel.Low, $"Пользователь: {AuthService.CurrentEmployee.FullName} прочитал уведомление: {notif.Title}", typeof(NotificationService).Name);
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
    }
    
    public async Task MarkAllAsRead()
    {
        bool changed = false;
        await ILogService.LogAsync(LogLevel.Low, $"Пользователь: {AuthService.CurrentEmployee.FullName} прочитал все уведомление", typeof(NotificationService).Name);
        foreach (var n in Notifications.Where(n => !n.IsRead))
        {
            n.IsRead = true;
            changed = true;
        }
        
        if (changed)
        {
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
    }
    
    public async Task RemoveNotification(int id)
    {
        var notif = Notifications.FirstOrDefault(n => n.Id == id);
        
        await ILogService.LogAsync(LogLevel.Low, $"Пользователь: {AuthService.CurrentEmployee.FullName} Удалил уведомление: {notif.Title}", typeof(NotificationService).Name);
        
        if (notif != null)
        {
            Notifications.Remove(notif);
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
    }
    
    public async Task ClearAll()
    {
        await ILogService.LogAsync(LogLevel.Medium, $"Пользователь: {AuthService.CurrentEmployee.FullName} Удалил все уведомления", typeof(NotificationService).Name);

        if (Notifications.Count > 0)
        {
            Notifications.Clear();
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
    }
    
    public async Task GenerateBookingNotificationsAsync()
    {
        try
        {
            var bookingService = ServiceLocator.GetService<IBookingService>();
            var today = DateTime.Today;
            var bookings = (await bookingService.GetAllBookingsWithDetailsAsync()).ToList();
            
            // Удаляем старые уведомления о бронированиях
            var toRemove = Notifications.Where(n => n.BookingId.HasValue).ToList();
            foreach (var n in toRemove)
                Notifications.Remove(n);
            
            int id = Notifications.Count > 0 ? Notifications.Max(n => n.Id) + 1 : 1;
            
            // Заезд сегодня
            AddNotificationsForBookings(
                bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date == today),
                id, NotificationType.CheckIn, "Заезд сегодня", b => b.CheckInDate);
            
            // Выезд сегодня
            AddNotificationsForBookings(
                bookings.Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) && b.CheckOutDate.Date == today),
                id, NotificationType.CheckOut, "Выезд сегодня", b => b.CheckOutDate);
            
            // Заезд на этой неделе
            AddNotificationsForBookings(
                bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date > today && b.CheckInDate.Date <= today.AddDays(7)),
                id, NotificationType.CheckIn, b => $"Заезд {b.CheckInDate:dd.MM}", b => b.CheckInDate);
            
            // Выезд на этой неделе
            AddNotificationsForBookings(
                bookings.Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) && b.CheckOutDate.Date > today && b.CheckOutDate.Date <= today.AddDays(7)),
                id, NotificationType.CheckOut, b => $"Выезд {b.CheckOutDate:dd.MM}", b => b.CheckOutDate);
            
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
        catch { }
    }

    private void AddNotificationsForBookings(
        IEnumerable<Booking> bookings,
        int startId,
        NotificationType type,
        Func<Booking, string> titleFunc,
        Func<Booking, DateTime> dateFunc)
    {
        int id = startId;
        foreach (var b in bookings)
        {
            Notifications.Add(new NotificationItem
            {
                Id = id++,
                Title = titleFunc(b),
                Message = $"{b.Room?.Name} - {b.Client?.FullName}",
                Type = type,
                BookingId = b.Id,
                CreatedAt = DateTime.Now,
                IsRead = false
            });
        }
    }

    private void AddNotificationsForBookings(
        IEnumerable<Booking> bookings,
        int startId,
        NotificationType type,
        string fixedTitle,
        Func<Booking, DateTime> dateFunc)
    {
        AddNotificationsForBookings(bookings, startId, type, _ => fixedTitle, dateFunc);
    }
}


