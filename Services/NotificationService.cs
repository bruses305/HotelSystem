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
            
            // Р В Р в‚¬Р В РўвЂР В Р’В°Р В Р’В»Р РЋР РЏР В Р’ВµР В РЎВ Р РЋР С“Р РЋРІР‚С™Р В Р’В°Р РЋР вЂљР РЋРІР‚в„–Р В Р’Вµ Р В Р’В°Р В Р вЂ Р РЋРІР‚С™Р В РЎвЂўР В РЎВР В Р’В°Р РЋРІР‚С™Р В РЎвЂР РЋРІР‚РЋР В Р’ВµР РЋР С“Р В РЎвЂќР В РЎвЂР В Р’Вµ Р РЋРЎвЂњР В Р вЂ Р В Р’ВµР В РўвЂР В РЎвЂўР В РЎВР В Р’В»Р В Р’ВµР В Р вЂ¦Р В РЎвЂР РЋР РЏ Р В РЎвЂў Р В Р’В±Р РЋР вЂљР В РЎвЂўР В Р вЂ¦Р В РЎвЂР РЋР вЂљР В РЎвЂўР В Р вЂ Р В Р’В°Р В Р вЂ¦Р В РЎвЂР РЋР РЏР РЋРІР‚В¦
            var toRemove = Notifications.Where(n => n.BookingId.HasValue).ToList();
            foreach (var n in toRemove)
                Notifications.Remove(n);
            
            int id = Notifications.Count > 0 ? Notifications.Max(n => n.Id) + 1 : 1;
            
            // Р В РІР‚вЂќР В Р’В°Р В Р’ВµР В Р’В·Р В РўвЂР РЋРІР‚в„– Р РЋР С“Р В Р’ВµР В РЎвЂ“Р В РЎвЂўР В РўвЂР В Р вЂ¦Р РЋР РЏ
            foreach (var b in bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date == today))
            {
                Notifications.Add(new NotificationItem
                {
                    Id = id++,
                    Title = "Р В РІР‚вЂќР В Р’В°Р В Р’ВµР В Р’В·Р В РўвЂ Р РЋР С“Р В Р’ВµР В РЎвЂ“Р В РЎвЂўР В РўвЂР В Р вЂ¦Р РЋР РЏ",
                    Message = $"{b.Room?.Name} - {b.Client?.FullName}",
                    Type = NotificationType.CheckIn,
                    BookingId = b.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            
            // Р В РІР‚в„ўР РЋРІР‚в„–Р В Р’ВµР В Р’В·Р В РўвЂР РЋРІР‚в„– Р РЋР С“Р В Р’ВµР В РЎвЂ“Р В РЎвЂўР В РўвЂР В Р вЂ¦Р РЋР РЏ
            foreach (var b in bookings.Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) && b.CheckOutDate.Date == today))
            {
                Notifications.Add(new NotificationItem
                {
                    Id = id++,
                    Title = "Р В РІР‚в„ўР РЋРІР‚в„–Р В Р’ВµР В Р’В·Р В РўвЂ Р РЋР С“Р В Р’ВµР В РЎвЂ“Р В РЎвЂўР В РўвЂР В Р вЂ¦Р РЋР РЏ",
                    Message = $"{b.Room?.Name} - {b.Client?.FullName}",
                    Type = NotificationType.CheckOut,
                    BookingId = b.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            
            // Р В РІР‚вЂќР В Р’В°Р В Р’ВµР В Р’В·Р В РўвЂР РЋРІР‚в„– Р В Р вЂ¦Р В Р’В° Р В Р вЂ¦Р В Р’ВµР В РўвЂР В Р’ВµР В Р’В»Р В Р’Вµ
            foreach (var b in bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date > today && b.CheckInDate.Date <= today.AddDays(7)))
            {
                Notifications.Add(new NotificationItem
                {
                    Id = id++,
                    Title = $"Р В РІР‚вЂќР В Р’В°Р В Р’ВµР В Р’В·Р В РўвЂ {b.CheckInDate:dd.MM}",
                    Message = $"{b.Room?.Name} - {b.Client?.FullName}",
                    Type = NotificationType.CheckIn,
                    BookingId = b.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            
            // Р В РІР‚в„ўР РЋРІР‚в„–Р В Р’ВµР В Р’В·Р В РўвЂР РЋРІР‚в„– Р В Р вЂ¦Р В Р’В° Р В Р вЂ¦Р В Р’ВµР В РўвЂР В Р’ВµР В Р’В»Р В Р’Вµ
            foreach (var b in bookings.Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) && b.CheckOutDate.Date > today && b.CheckOutDate.Date <= today.AddDays(7)))
            {
                Notifications.Add(new NotificationItem
                {
                    Id = id++,
                    Title = $"Р В РІР‚в„ўР РЋРІР‚в„–Р В Р’ВµР В Р’В·Р В РўвЂ {b.CheckOutDate:dd.MM}",
                    Message = $"{b.Room?.Name} - {b.Client?.FullName}",
                    Type = NotificationType.CheckOut,
                    BookingId = b.Id,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            
            SaveToFile();
            NotificationsChanged?.Invoke();
        }
        catch { }
    }
}


