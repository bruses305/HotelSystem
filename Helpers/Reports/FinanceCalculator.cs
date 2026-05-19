using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Единый класс для расчёта финансовых показателей
/// Используется как единственный источник правды для всех финансовых вычислений
/// </summary>
public class FinanceCalculator : IFinanceCalculator
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
        var incomeTransactions = transactions.Where(t => t.Type == TransactionType.Доход && t.ServiceId.HasValue);
        
        foreach (var tx in incomeTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                SourceType = "Service",
                SourceId = tx.ServiceId,
                RoomId = tx.RoomId,
                Description = tx.Description
            });
        }
        
        // 2. Доходы от транзакций "Прочий доход"
        var otherIncomeTransactions = transactions.Where(t => t.Type == TransactionType.Доход && !t.ServiceId.HasValue && !t.BookingId.HasValue);
        foreach (var tx in otherIncomeTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                SourceType = "OtherIncome",
                SourceId = null,
                Description = tx.Description
            });
        }
        
        // 3. ОПЛАТА БРОНИРОВАНИЙ - только через транзакции, не через Booking.PaidAmount
        // Ищем транзакции связанные с бронированиями (это создаются в RecordBookingPaymentAsync)
        var bookingPaymentTransactions = transactions.Where(t => t.Type == TransactionType.Доход && t.BookingId.HasValue && t.RoomId.HasValue);
        foreach (var tx in bookingPaymentTransactions)
        {
            incomes.Add(new IncomeEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                SourceType = "Booking",
                SourceId = tx.BookingId,
                RoomId = tx.RoomId,
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
        var expenseTransactions = transactions.Where(t => t.Type == TransactionType.Расход);
        
        foreach (var tx in expenseTransactions)
        {
            expenses.Add(new ExpenseEntry
            {
                Date = tx.TransactionDate,
                Amount = tx.Amount,
                Category = tx.Category,
                SourceType = tx.BookingId.HasValue ? "Booking" : 
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

    /// <summary>
    /// Получает доходы по периодам
    /// </summary>
    public async Task<Dictionary<TKey, decimal>> GetIncomeByPeriodAsync<TKey>(
        DateTime startDate, DateTime endDate, PeriodType periodType) where TKey : notnull
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        var result = new Dictionary<TKey, decimal>();
        
        foreach (var income in incomes)
        {
            TKey key = periodType switch
            {
                PeriodType.Day => (TKey)(object)income.Date.Date,
                PeriodType.Week => (TKey)(object)GetWeekStart(income.Date),
                PeriodType.Month => (TKey)(object)income.Date.ToString("yyyy-MM"),
                PeriodType.Year => (TKey)(object)income.Date.ToString("yyyy"),
                _ => (TKey)(object)income.Date.Date
            };
            
            if (result.ContainsKey(key))
                result[key] += income.Amount;
            else
                result[key] = income.Amount;
        }
        
        return result;
    }

    /// <summary>
    /// Получает расходы по периодам
    /// </summary>
    public async Task<Dictionary<TKey, decimal>> GetExpenseByPeriodAsync<TKey>(
        DateTime startDate, DateTime endDate, PeriodType periodType) where TKey : notnull
    {
        var expenses = await GetAllExpensesAsync(startDate, endDate);
        var result = new Dictionary<TKey, decimal>();
        
        foreach (var expense in expenses)
        {
            TKey key = periodType switch
            {
                PeriodType.Day => (TKey)(object)expense.Date.Date,
                PeriodType.Week => (TKey)(object)GetWeekStart(expense.Date),
                PeriodType.Month => (TKey)(object)expense.Date.ToString("yyyy-MM"),
                PeriodType.Year => (TKey)(object)expense.Date.ToString("yyyy"),
                _ => (TKey)(object)expense.Date.Date
            };
            
            if (result.ContainsKey(key))
                result[key] += expense.Amount;
            else
                result[key] = expense.Amount;
        }
        
        return result;
    }

    /// <summary>
    /// Получает сводную финансовую информацию
    /// </summary>
    public async Task<FinancialSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        var expenses = await GetAllExpensesAsync(startDate, endDate);
        var bookings = await _bookingRepository.GetBookingsByDateRangeAsync(startDate, endDate);
        
        var summary = new FinancialSummary
        {
            TotalIncome = incomes.Sum(i => i.Amount),
            TotalExpense = expenses.Sum(e => e.Amount),
            BookingCount = incomes.Count(i => i.SourceType == "Booking"),
            ServiceCount = incomes.Count(i => i.SourceType == "Service"),
            IncomeByMonth = await GetIncomeByPeriodAsync<string>(startDate, endDate, PeriodType.Month) as Dictionary<string, decimal> ?? new(),
            IncomeByRoom = await GetIncomeByRoomAsync(startDate, endDate),
            IncomeByCategory = await GetIncomeByCategoryAsync(startDate, endDate)
        };
        
        summary.Profit = summary.TotalIncome - summary.TotalExpense;
        summary.AverageBookingIncome = summary.BookingCount > 0 
            ? incomes.Where(i => i.SourceType == "Booking").Sum(i => i.Amount) / summary.BookingCount 
            : 0;
        summary.AverageServiceIncome = summary.ServiceCount > 0
            ? incomes.Where(i => i.SourceType == "Service").Sum(i => i.Amount) / summary.ServiceCount
            : 0;
        
        return summary;
    }

    /// <summary>
    /// Получает доходы по номерам
    /// </summary>
    public async Task<Dictionary<int, decimal>> GetIncomeByRoomAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        return incomes
            .Where(i => i.RoomId.HasValue)
            .GroupBy(i => i.RoomId.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Amount)
            );
    }

    /// <summary>
    /// Получает доходы по категориям
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetIncomeByCategoryAsync(DateTime startDate, DateTime endDate)
    {
        var incomes = await GetAllIncomesAsync(startDate, endDate);
        return incomes
            .GroupBy(i => i.SourceType)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Amount)
            );
    }

    private DateTime GetWeekStart(DateTime date)
    {
        int days = (int)date.DayOfWeek;
        return date.AddDays(-days).Date;
    }
}

/// <summary>
/// Запись дохода
/// </summary>
public class IncomeEntry
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string SourceType { get; set; } = ""; // "Booking", "Service", "OtherIncome"
    public int? SourceId { get; set; }
    public int? RoomId { get; set; }
    public int? ClientId { get; set; }
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
    public string SourceType { get; set; } = ""; // "Booking", "Room", "Salary", "Other"
    public int? SourceId { get; set; }
    public string? Description { get; set; }
}
