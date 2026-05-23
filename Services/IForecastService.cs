namespace HotelSystem.Services;

public interface IForecastService
{
    Task<HistoricalData> GetHistoricalDataAsync(DateTime fromDate, DateTime toDate);
    SeasonalCoefficients CalculateSeasonalCoefficients(HistoricalData data);
    Dictionary<DayOfWeek, decimal> CalculateDayOfWeekCoefficients(HistoricalData data);
    TrendAnalysis CalculateTrend(HistoricalData data);
    HotelMetrics CalculateMetrics(HistoricalData data);
    Task<ForecastPrediction> PredictAsync(DateTime fromDate, DateTime toDate);
}