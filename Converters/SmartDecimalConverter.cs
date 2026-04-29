using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Converters;

/// <summary>
/// Конвертер для отображения чисел с умной точностью.
/// Показывает дробную часть только если она есть, без лишних нулей.
/// Примеры: 1, 1.5, 1.25
/// </summary>
public class SmartDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return decimalValue.ToString("0.##", culture);
        }
        if (value is double doubleValue)
        {
            return doubleValue.ToString("0.##", culture);
        }
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            if (decimal.TryParse(strValue, NumberStyles.Any, culture, out var decimalResult))
            {
                if (targetType == typeof(decimal))
                    return decimalResult;
                if (targetType == typeof(double))
                    return (double)decimalResult;
            }
        }
        return value;
    }
}
