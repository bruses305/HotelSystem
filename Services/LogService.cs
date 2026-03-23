using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class LogService : ILogService
{
    private static LogService _instance;
    public static LogService Instance => _instance;
    private readonly ILogRepository _logRepository;

    public LogService(ILogRepository logRepository)
    {
        _instance = this;
        _logRepository = logRepository;
    }

    public async Task LogAsync(LogLevel level, string message, string source)
    {
        var log = new SystemLog
        {
            Level = level,
            Message = message,
            Source = source,
            LogDate = DateTime.Now
        };

        await _logRepository.AddAsync(log);
    }

    public async Task<IEnumerable<SystemLog>> GetAllLogsAsync()
    {
        return await _logRepository.GetAllAsync();
    }

    public async Task<IEnumerable<SystemLog>> GetLogsByLevelAsync(LogLevel level)
    {
        return await _logRepository.GetLogsByLevelAsync(level);
    }

    public async Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _logRepository.GetLogsByDateRangeAsync(startDate, endDate);
    }

    public async Task<int> DeleteOldLogsAsync(int daysToKeep)
    {
        return await _logRepository.DeleteOldLogsAsync(daysToKeep);
    }

    public async Task<int> GetLogCountByLevelAsync(LogLevel level)
    {
        return await _logRepository.GetLogCountByLevelAsync(level);
    }
}
