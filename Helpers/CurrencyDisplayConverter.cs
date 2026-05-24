using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Helpers;

/// <summary>
/// Простой конвертер для заголовков колонок — добавляет символ валюты из AppConstants
/// </summary>
public class CurrencyDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
            return $"{text} {AppConstants.Currency}";
        
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
