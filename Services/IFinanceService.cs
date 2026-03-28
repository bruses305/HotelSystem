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
    public List<BookingTransactionDetail> BookingDetails { get; set; } = new();
    public List<ServiceTransactionDetail> ServiceDetails { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new(); // Для использования в отчётах
}

public class BookingTransactionDetail
{
    public DateTime Date { get; set; }
    public string RoomName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Type { get; set; } = "";
}

public class ServiceTransactionDetail
{
    public DateTime Date { get; set; }
    public string ServiceName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
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
