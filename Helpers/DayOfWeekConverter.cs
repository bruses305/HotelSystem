using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Helpers;

public class DayOfWeekConverter : IValueConverter
{
    private static readonly Dictionary<DayOfWeek, string> RussianDays = new()
    {
        [DayOfWeek.Monday] = "Пн",
        [DayOfWeek.Tuesday] = "Вт",
        [DayOfWeek.Wednesday] = "Ср",
        [DayOfWeek.Thursday] = "Чт",
        [DayOfWeek.Friday] = "Пт",
        [DayOfWeek.Saturday] = "Сб",
        [DayOfWeek.Sunday] = "Вс"
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime date)
        {
            return RussianDays.GetValueOrDefault(date.DayOfWeek, date.ToString("ddd", new CultureInfo("ru-RU")));
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}