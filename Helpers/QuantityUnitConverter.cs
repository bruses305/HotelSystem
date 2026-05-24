using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Helpers;

public class QuantityUnitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "—";

        if (value is decimal quantity)
        {
            return $"{quantity:F2}";
        }

        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class QuantityWithUnitConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var quantity = values.Length > 0 ? values[0] : null;
        var unit = values.Length > 1 ? values[1] : null;

        if (quantity == null || quantity is not decimal qty || qty == 0)
            return "—";

        var unitName = unit as string;
        if (string.IsNullOrWhiteSpace(unitName))
            return $"{qty:F2}";

        return $"{qty:F2} {unitName}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}