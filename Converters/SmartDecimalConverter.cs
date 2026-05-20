using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Converters;

public class SmartDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
            return decimalValue.ToString("0.##", culture);
        if (value is double doubleValue)
            return doubleValue.ToString("0.##", culture);
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue && decimal.TryParse(strValue, NumberStyles.Any, culture, out var decimalResult))
        {
            if (targetType == typeof(decimal))
                return decimalResult;
            if (targetType == typeof(double))
                return (double)decimalResult;
        }
        return value;
    }
}