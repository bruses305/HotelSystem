using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class LogRepository : Repository<SystemLog>, ILogRepository
{
    public LogRepository(HotelDbContext context) : base(context) { }

    public async Task<IEnumerable<SystemLog>> GetLogsByLevelAsync(LogLevel level)
    {
        return await _dbSet
            .Where(l => l.Level == level)
            .OrderByDescending(l => l.LogDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(l => l.LogDate >= startDate && l.LogDate <= endDate)
            .OrderByDescending(l => l.LogDate)
            .ToListAsync();
    }

    public async Task<int> DeleteOldLogsAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        var oldLogs = await _dbSet.Where(l => l.LogDate < cutoffDate).ToListAsync();
        int oldLogCount = oldLogs.Count;
        _dbSet.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
        return oldLogCount;
        
    }

    public async Task<int> GetLogCountByLevelAsync(LogLevel level)
    {
        return await _dbSet.CountAsync(l => l.Level == level);
    }
}
