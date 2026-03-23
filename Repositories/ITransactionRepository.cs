using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(TransactionCategory category);
    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Transaction>> GetAllWithDetailsAsync();
}
