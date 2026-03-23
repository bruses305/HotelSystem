using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public class FinanceReport
{
    public double TotalIncome { get; set; }
    public double TotalExpenses { get; set; }
    public double Profit { get; set; }
    public Dictionary<TransactionCategory, double> IncomeByCategory { get; set; } = new();
    public Dictionary<TransactionCategory, double> ExpensesByCategory { get; set; } = new();
    public Dictionary<int, double> IncomeByRoom { get; set; } = new();
    public Dictionary<string, double> IncomeByMonth { get; set; } = new();
}

public interface IFinanceService
{
    Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<decimal> GetProfitAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<FinanceReport> GetFinanceReportAsync(DateTime startDate, DateTime endDate);
    Task<Transaction> AddTransactionAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task RecordBookingPaymentAsync(int bookingId, decimal amount);
    Task RecordServicePaymentAsync(int bookingId, int serviceId, int quantity, decimal amount);
    Task RecordSalaryPaymentAsync(int employeeId, decimal amount);
    Task RecordRoomExpenseAsync(int roomId, string category, decimal amount);
}
