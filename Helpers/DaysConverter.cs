using System;
using System.Globalization;
using System.Windows.Data;

namespace HotelSystem.Converters
{
    /// <summary>
    /// Конвертер для отображения количества дней с правильным склонением
    /// (1 день, 2 дня, 5 дней, 21 день и т.д.)
    /// </summary>
    public class DaysConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "0 дней";

            int days;
            try
            {
                days = System.Convert.ToInt32(value);
            }
            catch
            {
                return "0 дней";
            }

            return GetDaysString(days);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetDaysString(int days)
        {
            if (days < 0) days = -days; // модуль

            int lastDigit = days % 10;
            int lastTwoDigits = days % 100;

            // Случаи 11-14 дней (исключения)
            if (lastTwoDigits >= 11 && lastTwoDigits <= 14)
                return $"{days} дней";

            // Склонение по последней цифре
            switch (lastDigit)
            {
                case 1:
                    return $"{days} день";
                case 2:
                case 3:
                case 4:
                    return $"{days} дня";
                default:
                    return $"{days} дней";
            }
        }
    }
}