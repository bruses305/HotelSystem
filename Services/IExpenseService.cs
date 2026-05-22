using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IExpenseService
{
    Task<IEnumerable<Expense>> GetAllExpensesAsync();
    Task<Expense?> GetExpenseByIdAsync(int id);
    Task<Expense> CreateExpenseAsync(Expense expense);
    Task UpdateExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(int id);
    Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category);
    Task<IEnumerable<Expense>> GetOverdueExpensesAsync(int weeks);
    Task PayExpenseAsync(int id);
    Task<decimal> GetTotalExpensesAsync();
    Task<decimal> GetTotalExpensesByCategoryAsync(string category);
}