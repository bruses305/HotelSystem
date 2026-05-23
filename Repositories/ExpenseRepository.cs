using System.Linq;
using System.Threading.Tasks;
using HotelSystem.Data;
using HotelSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem.Repositories;

public class ExpenseRepository : Repository<Expense>, IExpenseRepository
{
    private readonly HotelDbContext _context;

    public ExpenseRepository(HotelDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category)
    {
        return await _context.Expenses
            .Where(e => e.Category == category)
            .OrderByDescending(e => e.LastPaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetOverdueExpensesAsync(int weeks)
    {
        var thresholdDate = DateTime.Now.AddDays(-weeks * 7);
        return await _context.Expenses
            .Where(e => e.LastPaymentDate < thresholdDate)
            .OrderByDescending(e => e.LastPaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Expenses
            .Where(e => e.LastPaymentDate >= start && e.LastPaymentDate <= end)
            .OrderByDescending(e => e.LastPaymentDate)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalExpensesByCategoryAsync(string category)
    {
        return await _context.Expenses
            .Where(e => e.Category == category)
            .SumAsync(e => e.Amount);
    }

    public async Task<IEnumerable<Expense>> GetExpensesWithRoomAsync()
    {
        return await _context.Expenses
            .OrderByDescending(e => e.LastPaymentDate)
            .ToListAsync();
    }
}