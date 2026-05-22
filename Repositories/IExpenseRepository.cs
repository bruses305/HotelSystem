using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IExpenseRepository : IRepository<Expense>
{
    Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category);
    Task<IEnumerable<Expense>> GetOverdueExpensesAsync(int weeks);
    Task<IEnumerable<Expense>> GetExpensesByDateRangeAsync(DateTime start, DateTime end);
    Task<decimal> GetTotalExpensesByCategoryAsync(string category);
    Task<IEnumerable<Expense>> GetExpensesWithRoomAsync();
}