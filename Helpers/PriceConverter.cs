using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Helpers;

/// <summary>
/// Конвертер для отображения цен с валютой в DataGrid
/// </summary>
public class PriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return AppConstants.FormatPrice(0);
        
        return AppConstants.FormatPrice(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            // Убираем валюту и пробелы, пытаемся преобразовать обратно
            var cleaned = str.Replace(AppConstants.Currency, "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Any, culture, out var result))
                return result;
        }
        return 0m;
    }
}

/// <summary>
/// Конвертер для отображения цен без валюты (только число)
/// </summary>
public class PriceOnlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "0";
        
        return AppConstants.FormatPriceOnly(decimal.TryParse(value.ToString(), out var d) ? d : 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
