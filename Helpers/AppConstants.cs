namespace HotelSystem.Helpers;

/// <summary>
/// Статические константы приложения
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Валюта для отображения цен (можно изменить в настройках)
    /// </summary>
    public static string Currency { get; set; } = "Br";
    
    /// <summary>
    /// Формат отображения цены с валютой (decimal)
    /// </summary>
    public static string FormatPrice(decimal amount) => $"{amount:N0} {Currency}";
    
    /// <summary>
    /// Формат отображения цены с валютой (double)
    /// </summary>
    public static string FormatPriceDouble(double amount) => $"{amount:N0} {Currency}";
    
    /// <summary>
    /// Формат отображения цены без валюты (только число)
    /// </summary>
    public static string FormatPriceOnly(decimal amount) => $"{amount:N0}";
    
    /// <summary>
    /// Формат отображения цены с валютой (принимает object для гибкости)
    /// </summary>
    public static string FormatPrice(object amount)
    {
        if (amount is decimal d)
            return $"{d:N0} {Currency}";
        if (amount is double dbl)
            return $"{dbl:N0} {Currency}";
        if (amount is int i)
            return $"{i:N0} {Currency}";
        return $"{amount} {Currency}";
    }
}
