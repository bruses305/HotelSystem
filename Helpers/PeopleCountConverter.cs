using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Converters;

/// <summary>
/// Конвертер для отображения количества людей в формате "X чел."
/// </summary>
public class PeopleCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            return s + " чел.";
        }
        if (value == null)
            return "0 чел.";

        int people = 0;
        if (value is int intPeople)
            people = intPeople;
        else if (value is short shortPeople)
            people = shortPeople;
        else if (value is long longPeople)
            people = (int)longPeople;
        else if (value is decimal decimalPeople)
            people = (int)decimalPeople;
        else if (value is double doublePeople)
            people = (int)doublePeople;
        else
            return "0 чел.";

        return $"{people} чел.";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Обратное преобразование не требуется
        throw new NotImplementedException();
    }
}