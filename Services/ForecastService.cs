using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

/// <summary>
/// Продвинутая система прогнозирования загрузки и доходов отеля
/// Использует исторические данные, сезонность, тренды
/// </summary>
public class ForecastService : IForecastService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IEmployeeService _employeeService;

    public ForecastService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        ITransactionRepository transactionRepository,
        IExpenseRepository expenseRepository,
        IEmployeeService employeeService)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _transactionRepository = transactionRepository;
        _expenseRepository = expenseRepository;
        _employeeService = employeeService;
    }

    /// <summary>
    /// Получает исторические данные для анализа
    /// </summary>
    public async Task<HistoricalData> GetHistoricalDataAsync(DateTime fromDate, DateTime toDate)
    {
        var bookings = await _bookingRepository.GetAllAsync();
        var filteredBookings = bookings
            .Where(b => b.CheckInDate >= fromDate && b.CheckInDate <= toDate)
            .Where(b => b.Status != BookingStatus.Отменено)
            .ToList();

        var rooms = await _roomRepository.GetAllAsync();
        var transactions = await _transactionRepository.GetAllAsync();
        var expenses = await _expenseRepository.GetAllAsync();
        var employees = await _employeeService.GetAllEmployeesAsync();

        return new HistoricalData
        {
            Bookings = filteredBookings,
            TotalRooms = rooms.Count(),
            TotalRoomCapacity = rooms.Sum(r => r.Capacity),
            TotalIncome = transactions.Where(t => t.Type == TransactionType.Доход).Sum(t => t.Amount),
            TotalExpenses = transactions.Where(t => t.Type == TransactionType.Расход).Sum(t => t.Amount) +
                           expenses.Sum(e => e.Amount) +
                           employees.Where(e => e.IsActive).Sum(e => e.Salary),
            DateRange = (fromDate, toDate)
        };
    }

    /// <summary>
    /// Рассчитывает сезонные коэффициенты по месяцам
    /// </summary>
    public SeasonalCoefficients CalculateSeasonalCoefficients(HistoricalData data)
    {
        var monthlyData = data.Bookings
            .GroupBy(b => b.CheckInDate.Month)
            .ToDictionary(
                g => g.Key,
                g => new MonthlyStats
                {
                    BookingCount = g.Count(),
                    TotalNights = g.Sum(b => b.Days),
                    TotalIncome = g.Sum(b => b.TotalPrice),
                    AvgDailyRate = g.Any() ? g.Sum(b => b.TotalPrice) / g.Sum(b => b.Days) : 0
                });

        var avgBookingsPerMonth = monthlyData.Values.Average(m => m.BookingCount);
        var coefficients = new Dictionary<int, decimal>();

        for (int month = 1; month <= 12; month++)
        {
            if (monthlyData.TryGetValue(month, out var stats) && avgBookingsPerMonth > 0)
            {
                coefficients[month] = (decimal)Math.Round(stats.BookingCount / avgBookingsPerMonth, 2);
            }
            else
            {
                coefficients[month] = 1.0m; // По умолчанию
            }
        }

        return new SeasonalCoefficients
        {
            MonthlyCoefficients = coefficients,
            MonthlyStats = monthlyData
        };
    }

    /// <summary>
    /// Рассчитывает коэффициенты по дням недели
    /// </summary>
    public Dictionary<DayOfWeek, decimal> CalculateDayOfWeekCoefficients(HistoricalData data)
    {
        var dowData = data.Bookings
            .GroupBy(b => b.CheckInDate.DayOfWeek)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        var avgBookingsPerDay = dowData.Values.Average();
        var coefficients = new Dictionary<DayOfWeek, decimal>();

        foreach (DayOfWeek dow in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (dowData.TryGetValue(dow, out var count) && avgBookingsPerDay > 0)
            {
                coefficients[dow] = Math.Round((decimal)count / (decimal)avgBookingsPerDay, 2);
            }
            else
            {
                coefficients[dow] = 1.0m;
            }
        }

        return coefficients;
    }

    /// <summary>
    /// Рассчитывает тренд (рост/падение за последние месяцы)
    /// </summary>
    public TrendAnalysis CalculateTrend(HistoricalData data)
    {
        var monthlyIncome = data.Bookings
            .GroupBy(b => new { b.CheckInDate.Year, b.CheckInDate.Month })
            .Select(g => new { g.Key, Income = g.Sum(b => b.TotalPrice), Count = g.Count() })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .ToList();

        if (monthlyIncome.Count < 2)
            return new TrendAnalysis { TrendDirection = TrendDirection.Stable, GrowthRate = 0 };

        var firstMonth = monthlyIncome.First();
        var lastMonth = monthlyIncome.Last();

        var incomeGrowth = firstMonth.Income > 0
            ? (lastMonth.Income - firstMonth.Income) / firstMonth.Income * 100
            : 0;

        var bookingGrowth = firstMonth.Count > 0
            ? (lastMonth.Count - firstMonth.Count) / (decimal)firstMonth.Count * 100
            : 0;

        var direction = incomeGrowth > 5 ? TrendDirection.Growing :
                       incomeGrowth < -5 ? TrendDirection.Declining :
                       TrendDirection.Stable;

        return new TrendAnalysis
        {
            TrendDirection = direction,
            GrowthRate = Math.Round(incomeGrowth, 1),
            BookingGrowthRate = Math.Round(bookingGrowth, 1),
            MonthlyData = monthlyIncome.Select(m => new MonthlyTrendData
            {
                Year = m.Key.Year,
                Month = m.Key.Month,
                Income = m.Income,
                BookingCount = m.Count
            }).ToList()
        };
    }

    /// <summary>
    /// Рассчитывает ключевые метрики отеля
    /// </summary>
    public HotelMetrics CalculateMetrics(HistoricalData data)
    {
        var totalNights = data.Bookings.Sum(b => b.Days);
        var totalRoomNightsAvailable = data.TotalRooms *
            (data.DateRange.toDate - data.DateRange.fromDate).Days;

        var occupancyRate = totalRoomNightsAvailable > 0
            ? (decimal)totalNights / totalRoomNightsAvailable * 100
            : 0;

        var avgDailyRate = totalNights > 0
            ? data.Bookings.Sum(b => b.TotalPrice) / totalNights
            : 0;

        var revPAR = totalRoomNightsAvailable > 0
            ? data.Bookings.Sum(b => b.TotalPrice) / totalRoomNightsAvailable
            : 0;

        var avgStayDuration = data.Bookings.Any()
            ? data.Bookings.Average(b => b.Days)
            : 0;

        return new HotelMetrics
        {
            OccupancyRate = Math.Round(occupancyRate, 1),
            AverageDailyRate = Math.Round(avgDailyRate, 2),
            RevPAR = Math.Round(revPAR, 2),
            AverageStayDuration = Math.Round((decimal)avgStayDuration, 1),
            TotalBookings = data.Bookings.Count,
            TotalNights = totalNights,
            TotalIncome = data.TotalIncome,
            TotalExpenses = data.TotalExpenses,
            NetProfit = data.TotalIncome - data.TotalExpenses
        };
    }

    /// <summary>
    /// Строит прогноз на будущий период
    /// </summary>
    public async Task<ForecastPrediction> PredictAsync(DateTime fromDate, DateTime toDate)
    {
        // 1. Получаем исторические данные (последние 12 месяцев)
        var historyFrom = fromDate.AddMonths(-12);
        var historyData = await GetHistoricalDataAsync(historyFrom, fromDate.AddDays(-1));

        // 2. Рассчитываем коэффициенты
        var seasonal = CalculateSeasonalCoefficients(historyData);
        var dowCoeffs = CalculateDayOfWeekCoefficients(historyData);
        var trend = CalculateTrend(historyData);
        var metrics = CalculateMetrics(historyData);

        // 3. Базовые значения для прогноза
        var daysInPeriod = (toDate - fromDate).Days;
        var monthsInPeriod = (toDate.Year - fromDate.Year) * 12 + toDate.Month - fromDate.Month + 1;

        // Средние значения за день из истории
        var historicalDays = (historyData.DateRange.toDate - historyData.DateRange.fromDate).Days;
        var avgDailyBookings = historicalDays > 0 ? (decimal)historyData.Bookings.Count / historicalDays : 0;
        var avgDailyIncome = historicalDays > 0 ? historyData.TotalIncome / historicalDays : 0;

        // 4. Расчёт прогноза по дням
        var dailyForecasts = new List<DailyForecast>();
        var currentDate = fromDate;

        while (currentDate <= toDate)
        {
            var monthCoeff = seasonal.MonthlyCoefficients.GetValueOrDefault(currentDate.Month, 1.0m);
            var dowCoeff = dowCoeffs.GetValueOrDefault(currentDate.DayOfWeek, 1.0m);
            var trendMultiplier = 1 + (trend.GrowthRate / 100 / 30); // Дневной тренд

            // Базовый прогноз
            var predictedBookings = avgDailyBookings * monthCoeff * dowCoeff * trendMultiplier;
            var predictedIncome = avgDailyIncome * monthCoeff * dowCoeff * trendMultiplier;

            // Вероятностный диапазон (±20%)
            var bookingRange = predictedBookings * 0.2m;
            var incomeRange = predictedIncome * 0.2m;

            dailyForecasts.Add(new DailyForecast
            {
                Date = currentDate,
                PredictedBookings = Math.Round(predictedBookings, 1),
                MinBookings = Math.Max(0, Math.Round(predictedBookings - bookingRange, 1)),
                MaxBookings = Math.Round(predictedBookings + bookingRange, 1),
                PredictedIncome = Math.Round(predictedIncome, 2),
                MinIncome = Math.Max(0, Math.Round(predictedIncome - incomeRange, 2)),
                MaxIncome = Math.Round(predictedIncome + incomeRange, 2),
                OccupancyRate = historyData.TotalRooms > 0
                    ? Math.Round(predictedBookings / historyData.TotalRooms * 100, 1)
                    : 0,
                SeasonalFactor = monthCoeff,
                DayOfWeekFactor = dowCoeff
            });

            currentDate = currentDate.AddDays(1);
        }

        // 5. Итоговые значения
        var totalPredictedIncome = dailyForecasts.Sum(d => d.PredictedIncome);
        var totalMinIncome = dailyForecasts.Sum(d => d.MinIncome);
        var totalMaxIncome = dailyForecasts.Sum(d => d.MaxIncome);
        var totalPredictedBookings = dailyForecasts.Sum(d => d.PredictedBookings);

        // Расходы = фиксированные (ЗП + коммунальные) + переменные (зависят от загрузки)
        var monthlyFixedExpenses = historyData.TotalExpenses / Math.Max(1, historicalDays / 30);
        var totalFixedExpenses = monthlyFixedExpenses * monthsInPeriod;
        var variableExpensesRatio = 0.3m; // 30% расходов переменные
        var totalVariableExpenses = totalPredictedIncome * variableExpensesRatio;
        var totalPredictedExpenses = totalFixedExpenses + totalVariableExpenses;

        return new ForecastPrediction
        {
            FromDate = fromDate,
            ToDate = toDate,
            DailyForecasts = dailyForecasts,
            TotalPredictedIncome = Math.Round(totalPredictedIncome, 2),
            MinIncome = Math.Round(totalMinIncome, 2),
            MaxIncome = Math.Round(totalMaxIncome, 2),
            TotalPredictedBookings = Math.Round(totalPredictedBookings, 1),
            TotalPredictedExpenses = Math.Round(totalPredictedExpenses, 2),
            NetProfit = Math.Round(totalPredictedIncome - totalPredictedExpenses, 2),
            HistoricalMetrics = metrics,
            SeasonalData = seasonal,
            TrendData = trend,
            ConfidenceLevel = CalculateConfidenceLevel(historyData)
        };
    }

    private decimal CalculateConfidenceLevel(HistoricalData data)
    {
        var days = (data.DateRange.toDate - data.DateRange.fromDate).Days;
        if (days >= 365) return 85;
        if (days >= 180) return 70;
        if (days >= 90) return 55;
        return 40;
    }
}

// ===== МОДЕЛИ ДАННЫХ =====

public class HistoricalData
{
    public List<Booking> Bookings { get; set; } = new();
    public int TotalRooms { get; set; }
    public int TotalRoomCapacity { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public (DateTime fromDate, DateTime toDate) DateRange { get; set; }
}

public class SeasonalCoefficients
{
    public Dictionary<int, decimal> MonthlyCoefficients { get; set; } = new();
    public Dictionary<int, MonthlyStats> MonthlyStats { get; set; } = new();
}

public class MonthlyStats
{
    public int BookingCount { get; set; }
    public int TotalNights { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal AvgDailyRate { get; set; }
}

public class TrendAnalysis
{
    public TrendDirection TrendDirection { get; set; }
    public decimal GrowthRate { get; set; }
    public decimal BookingGrowthRate { get; set; }
    public List<MonthlyTrendData> MonthlyData { get; set; } = new();
}

public enum TrendDirection
{
    Growing,    // Рост
    Stable,     // Стабильно
    Declining   // Падение
}

public class MonthlyTrendData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public int BookingCount { get; set; }
}

public class HotelMetrics
{
    public decimal OccupancyRate { get; set; }
    public decimal AverageDailyRate { get; set; }
    public decimal RevPAR { get; set; }
    public decimal AverageStayDuration { get; set; }
    public int TotalBookings { get; set; }
    public int TotalNights { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
}

public class ForecastPrediction
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DailyForecast> DailyForecasts { get; set; } = new();
    public decimal TotalPredictedIncome { get; set; }
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public decimal TotalPredictedBookings { get; set; }
    public decimal TotalPredictedExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public HotelMetrics HistoricalMetrics { get; set; } = new();
    public SeasonalCoefficients SeasonalData { get; set; } = new();
    public TrendAnalysis TrendData { get; set; } = new();
    public decimal ConfidenceLevel { get; set; }
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public decimal PredictedBookings { get; set; }
    public decimal MinBookings { get; set; }
    public decimal MaxBookings { get; set; }
    public decimal PredictedIncome { get; set; }
    public decimal MinIncome { get; set; }
    public decimal MaxIncome { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal SeasonalFactor { get; set; }
    public decimal DayOfWeekFactor { get; set; }
}