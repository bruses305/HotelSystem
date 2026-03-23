using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface ILogRepository : IRepository<SystemLog>
{
    Task<IEnumerable<SystemLog>> GetLogsByLevelAsync(LogLevel level);
    Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<int> DeleteOldLogsAsync(int daysToKeep);
    Task<int> GetLogCountByLevelAsync(LogLevel level);
}
