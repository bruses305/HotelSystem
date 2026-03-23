namespace HotelSystem.Models.Entities;

public enum LogLevel
{
    Low,       // Обычные
    Medium,    // Средние
    Critical   // Важные
}

public class SystemLog : BaseEntity
{
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime LogDate { get; set; } = DateTime.Now;
}