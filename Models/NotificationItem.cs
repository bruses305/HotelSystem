using System.Text.Json.Serialization;

namespace HotelSystem.Models;

public enum NotificationType
{
    Booking,
    CheckIn,
    CheckOut,
    Cancellation
}

public class NotificationItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
    
    [JsonPropertyName("type")]
    public NotificationType Type { get; set; } = NotificationType.Booking;
    
    [JsonPropertyName("bookingId")]
    public int? BookingId { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; } = false;
}

