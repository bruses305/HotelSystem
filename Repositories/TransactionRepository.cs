using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(HotelDbContext context) : base(context) { }

    public async Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet.Where(t => t.Type == TransactionType.Income);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        // SQLite Р В Р вЂ¦Р В Р’Вµ Р В РЎвЂ”Р В РЎвЂўР В РўвЂР В РўвЂР В Р’ВµР РЋР вЂљР В Р’В¶Р В РЎвЂР В Р вЂ Р В Р’В°Р В Р’ВµР РЋРІР‚С™ SumAsync Р В РўвЂР В Р’В»Р РЋР РЏ decimal, Р В Р’В·Р В Р’В°Р В РЎвЂ“Р РЋР вЂљР РЋРЎвЂњР В Р’В¶Р В Р’В°Р В Р’ВµР В РЎВ Р В Р вЂ¦Р В Р’В° Р В РЎвЂќР В Р’В»Р В РЎвЂР В Р’ВµР В Р вЂ¦Р РЋРІР‚С™
        var transactions = await query.ToListAsync();
        return transactions.Sum(t => t.Amount);
    }

    public async Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet.Where(t => t.Type == TransactionType.Expense);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        // SQLite Р В Р вЂ¦Р В Р’Вµ Р В РЎвЂ”Р В РЎвЂўР В РўвЂР В РўвЂР В Р’ВµР РЋР вЂљР В Р’В¶Р В РЎвЂР В Р вЂ Р В Р’В°Р В Р’ВµР РЋРІР‚С™ SumAsync Р В РўвЂР В Р’В»Р РЋР РЏ decimal, Р В Р’В·Р В Р’В°Р В РЎвЂ“Р РЋР вЂљР РЋРЎвЂњР В Р’В¶Р В Р’В°Р В Р’ВµР В РЎВ Р В Р вЂ¦Р В Р’В° Р В РЎвЂќР В Р’В»Р В РЎвЂР В Р’ВµР В Р вЂ¦Р РЋРІР‚С™
        var transactions = await query.ToListAsync();
        return transactions.Sum(t => t.Amount);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(TransactionCategory category)
    {
        return await _dbSet
            .Where(t => t.Category == category)
            .Include(t => t.Booking)
            .Include(t => t.Room)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Client)
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Room)
            .Include(t => t.Room)
            .Include(t => t.Employee)
            .Include(t => t.Service)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Client)
            .Include(t => t.Booking)
                .ThenInclude(b => b!.Room)
            .Include(t => t.Room)
            .Include(t => t.Employee)
            .Include(t => t.Service)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
}
