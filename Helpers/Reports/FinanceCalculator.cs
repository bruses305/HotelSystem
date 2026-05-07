using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Единый класс для расчёта финансовых показателей
/// Используется как единственный источник правды для всех финансовых вычислений
/// </summary>
public class FinanceCalculator
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ITransactionRepository _transactionRepository;

    public FinanceCalculator(
        IBookingRepository bookingRepository,
        ITransactionRepository transactionRepository)
    {
        _bookingRepository = bookingRepository;
        _transactionRepository = transactionRepository;
    }

    /// <summary>
    /// Получает все доходы за период без дублирования
    /// </summary>
    public async Task<List<IncomeEntry>> GetAllIncomesAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = new List<IncomeEntry>();
        
        // 1. Доходы от транзакций (услуги и другие доходы)
        var transactions = await _transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate);
        var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Income && t.ServiceId.HasValue);
        
        foreach (var tx in incomeTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                Source = "Service",
                SourceId = tx.ServiceId,
                Description = tx.Description
            });
        }
        
        // 2. Доходы от транзакций "Прочий доход"
        var otherIncomeTransactions = transactions.Where(t => t.Type == TransactionType.Income && !t.ServiceId.HasValue && !t.BookingId.HasValue);
        foreach (var tx in otherIncomeTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                Source = "OtherIncome",
                SourceId = null,
                Description = tx.Description
            });
        }
        
        // 3. ОПЛАТА БРОНИРОВАНИЙ - только через транзакции, не через Booking.PaidAmount
        // Ищем транзакции связанные с бронированиями (это создаются в RecordBookingPaymentAsync)
        var bookingPaymentTransactions = transactions.Where(t => t.Type == TransactionType.Income && t.BookingId.HasValue && t.RoomId.HasValue);
        foreach (var tx in bookingPaymentTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                Source = "Booking",
                SourceId = tx.BookingId,
                Description = tx.Description
            });
        }
        
        return incomes;
    }

    /// <summary>
    /// Получает все расходы за период
    /// </summary>
    public async Task<List<ExpenseEntry>> GetAllExpensesAsync(DateTime startDate, DateTime endDate)
    {
        var expenses = new List<ExpenseEntry>();
        
        var transactions = await _transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate);
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Expense);
        
        foreach (var tx in expenseTransactions)
        {
            expenses.Add(new ExpenseEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                Category = tx.Category,
                Source = tx.BookingId.HasValue ? "Booking" : 
                        tx.RoomId.HasValue ? "Room" :
                        tx.EmployeeId.HasValue ? "Salary" : "Other",
                SourceId = tx.BookingId ?? tx.RoomId ?? tx.EmployeeId,
                Description = tx.Description
            });
        }
        
        return expenses;
    }

    /// <summary>
    /// Считает доходы по дням БЕЗ дублирования
    /// </summary>
    public async Task<Dictionary<DateTime, decimal>> GetIncomeByDayAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        
        return incomes
            .GroupBy(i => i.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Amount)
            );
    }

    /// <summary>
    /// Считает расходы по дням
    /// </summary>
    public async Task<Dictionary<DateTime, decimal>> GetExpenseByDayAsync(DateTime startDate, DateTime endDate)
    {
        var expenses = await GetAllExpensesAsync(startDate, endDate);
        
        return expenses
            .GroupBy(e => e.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.Amount)
            );
    }

    /// <summary>
    /// Считает доходы по месяцам
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetIncomeByMonthAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        
        return incomes
            .GroupBy(i => i.Date.ToString("yyyy-MM"))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Amount)
            );
    }

    /// <summary>
    /// Считает расходы по месяцам
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetExpenseByMonthAsync(DateTime startDate, DateTime endDate)
    {
        var expenses = await GetAllExpensesAsync(startDate, endDate);
        
        return expenses
            .GroupBy(e => e.Date.ToString("yyyy-MM"))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(e => e.Amount)
            );
    }

    /// <summary>
    /// Считает общую сумму доходов
    /// </summary>
    public async Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        return incomes.Sum(i => i.Amount);
    }

    /// <summary>
    /// Считает общую сумму расходов
    /// </summary>
    public async Task<decimal> GetTotalExpenseAsync(DateTime startDate, DateTime endDate)
    {
        var expenses = await GetAllExpensesAsync(startDate, endDate);
        return expenses.Sum(e => e.Amount);
    }

    /// <summary>
    /// Считает прибыль
    /// </summary>
    public async Task<decimal> GetProfitAsync(DateTime startDate, DateTime endDate)
    {
        var income = await GetTotalIncomeAsync(startDate, endDate);
        var expense = await GetTotalExpenseAsync(startDate, endDate);
        return income - expense;
    }
}

/// <summary>
/// Запись дохода
/// </summary>
public class IncomeEntry
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Source { get; set; } // "Booking", "Service", "OtherIncome"
    public int? SourceId { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Запись расхода
/// </summary>
public class ExpenseEntry
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public TransactionCategory Category { get; set; }
    public string Source { get; set; } // "Booking", "Room", "Salary", "Other"
    public int? SourceId { get; set; }
    public string? Description { get; set; }
}
