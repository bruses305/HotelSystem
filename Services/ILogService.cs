using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface ILogService
{
    Task LogAsync(LogLevel level, string message, string source);
    Task<IEnumerable<SystemLog>> GetAllLogsAsync();
    Task<IEnumerable<SystemLog>> GetLogsByLevelAsync(LogLevel level);
    Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> DeleteOldLogsAsync(int daysToKeep);
    Task<int> GetLogCountByLevelAsync(LogLevel level);
}
