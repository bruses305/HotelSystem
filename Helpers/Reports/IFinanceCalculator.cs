using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Единый интерфейс для всех финансовых расчётов
/// Централизует всю финансовую логику
/// </summary>
public interface IFinanceCalculator
{
    /// <summary>
    /// Получает общую сумму доходов за период
    /// </summary>
    Task<decimal> GetTotalIncomeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Получает общую сумму расходов за период
    /// </summary>
    Task<decimal> GetTotalExpenseAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Получает прибыль за период
    /// </summary>
    Task<decimal> GetProfitAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Получает доходы по периодам (день/неделя/месяц)
    /// </summary>
    Task<Dictionary<TKey, decimal>> GetIncomeByPeriodAsync<TKey>(
        DateTime startDate, DateTime endDate, PeriodType periodType);
    
    /// <summary>
    /// Получает расходы по периодам
    /// </summary>
    Task<Dictionary<TKey, decimal>> GetExpenseByPeriodAsync<TKey>(
        DateTime startDate, DateTime endDate, PeriodType periodType);
    
    /// <summary>
    /// Получает сводную финансовую информацию
    /// </summary>
    Task<FinancialSummary> GetSummaryAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Получает доходы по номерам
    /// </summary>
    Task<Dictionary<int, decimal>> GetIncomeByRoomAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Получает доходы по категориям
    /// </summary>
    Task<Dictionary<string, decimal>> GetIncomeByCategoryAsync(DateTime startDate, DateTime endDate);
}

/// <summary>
/// Тип периода для группировки
/// </summary>
public enum PeriodType
{
    Day,
    Week,
    Month,
    Year
}

/// <summary>
/// Финансовая сводка
/// </summary>
public class FinancialSummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Profit { get; set; }
    public int BookingCount { get; set; }
    public int ServiceCount { get; set; }
    public decimal AverageBookingIncome { get; set; }
    public decimal AverageServiceIncome { get; set; }
    
    public Dictionary<string, decimal> IncomeByMonth { get; set; } = new();
    public Dictionary<int, decimal> IncomeByRoom { get; set; } = new();
    public Dictionary<string, decimal> IncomeByCategory { get; set; } = new();
}

/// <summary>
/// Запись дохода
/// </summary>
public class IncomeRecord
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string SourceType { get; set; } = ""; // "Booking", "Service", "Other"
    public int? SourceId { get; set; }
    public string? Description { get; set; }
    public int? RoomId { get; set; }
    public int? ClientId { get; set; }
}

/// <summary>
/// Запись расхода
/// </summary>
public class ExpenseRecord
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = "";
    public string SourceType { get; set; } = ""; // "Booking", "Room", "Salary", "Other"
    public int? SourceId { get; set; }
    public string? Description { get; set; }
}