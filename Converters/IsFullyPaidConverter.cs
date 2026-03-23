using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Converters;

public class IsFullyPaidConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is decimal paidAmount && values[1] is decimal totalPrice)
        {
            return paidAmount >= totalPrice;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
