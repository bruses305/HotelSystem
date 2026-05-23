using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Helpers;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class AdvancedForecastWindow : Window
{
    private readonly IForecastService _forecastService;

    public AdvancedForecastWindow()
    {
        InitializeComponent();
        _forecastService = ServiceLocator.GetService<IForecastService>();
        
        // Устанавливаем даты по умолчанию (следующий месяц)
        FromDatePicker.SelectedDate = DateTime.Now.AddDays(1);
        ToDatePicker.SelectedDate = DateTime.Now.AddMonths(1);
        
        // Автоматический расчёт при открытии
        _ = LoadForecastAsync();
    }

    private async void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadForecastAsync();
    }

    private async Task LoadForecastAsync()
    {
        if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Выберите период прогноза!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fromDate = FromDatePicker.SelectedDate.Value;
        var toDate = ToDatePicker.SelectedDate.Value;

        if (toDate <= fromDate)
        {
            MessageBox.Show("Дата окончания должна быть позже даты начала!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CalculateButton.IsEnabled = false;
        CalculateButton.Content = "Расчёт...";

        try
        {
            var prediction = await _forecastService.PredictAsync(fromDate, toDate);
            DisplayResults(prediction);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка прогнозирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CalculateButton.IsEnabled = true;
            CalculateButton.Content = "Рассчитать прогноз";
        }
    }

    private void DisplayResults(ForecastPrediction prediction)
    {
        // Итоговые показатели
        TotalIncomeText.Text = AppConstants.FormatPrice(prediction.TotalPredictedIncome);
        IncomeRangeText.Text = $"{AppConstants.FormatPrice(prediction.MinIncome)} — {AppConstants.FormatPrice(prediction.MaxIncome)}";
        TotalExpensesText.Text = AppConstants.FormatPrice(prediction.TotalPredictedExpenses);
        NetProfitText.Text = AppConstants.FormatPrice(prediction.NetProfit);
        TotalBookingsText.Text = $"{prediction.TotalPredictedBookings:N0}";
        ConfidenceText.Text = $"{prediction.ConfidenceLevel}%";

        // Цвет прибыли
        NetProfitText.Foreground = prediction.NetProfit >= 0 
            ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) 
            : new SolidColorBrush(Color.FromRgb(239, 68, 68));

        // Исторические метрики
        var metrics = prediction.HistoricalMetrics;
        OccupancyText.Text = $"{metrics.OccupancyRate}%";
        AdrText.Text = AppConstants.FormatPrice(metrics.AverageDailyRate);
        RevparText.Text = AppConstants.FormatPrice(metrics.RevPAR);
        AvgStayText.Text = $"{metrics.AverageStayDuration} дн.";

        // Тренд
        var trend = prediction.TrendData;
        var trendColor = trend.TrendDirection == TrendDirection.Growing ? "#10B981" :
                        trend.TrendDirection == TrendDirection.Declining ? "#EF4444" : "#F59E0B";
        TrendText.Text = trend.TrendDirection switch
        {
            TrendDirection.Growing => "📈 Рост",
            TrendDirection.Declining => "📉 Падение",
            _ => "➡ Стабильно"
        };
        TrendText.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(trendColor));
        TrendDetailsText.Text = $"Рост доходов: {trend.GrowthRate}% | Рост бронирований: {trend.BookingGrowthRate}%";

        // Сезонные коэффициенты
        DisplaySeasonalCoefficients(prediction.SeasonalData);

        // Дни недели
        DisplayDayOfWeekCoefficients(prediction);

        // Таблица прогноза
        ForecastDataGrid.ItemsSource = prediction.DailyForecasts;
    }

    private void DisplaySeasonalCoefficients(SeasonalCoefficients seasonal)
    {
        SeasonalGrid.Children.Clear();
        SeasonalGrid.RowDefinitions.Clear();

        var months = new[] { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };
        
        for (int i = 0; i < 4; i++)
        {
            SeasonalGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (int i = 0; i < 12; i++)
        {
            var coeff = seasonal.MonthlyCoefficients.GetValueOrDefault(i + 1, 1.0m);
            var border = new Border
            {
                Background = coeff > 1.1m ? new SolidColorBrush(Color.FromRgb(209, 250, 229)) :
                            coeff < 0.9m ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) :
                            new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(2)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = months[i], FontWeight = FontWeights.SemiBold, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = $"{coeff:F2}x", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)) });
            border.Child = stack;

            Grid.SetRow(border, i / 3);
            Grid.SetColumn(border, i % 3);
            SeasonalGrid.Children.Add(border);
        }
    }

    private void DisplayDayOfWeekCoefficients(ForecastPrediction prediction)
    {
        DowGrid.Children.Clear();
        DowGrid.RowDefinitions.Clear();

        var dayNames = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "Пн",
            [DayOfWeek.Tuesday] = "Вт",
            [DayOfWeek.Wednesday] = "Ср",
            [DayOfWeek.Thursday] = "Чт",
            [DayOfWeek.Friday] = "Пт",
            [DayOfWeek.Saturday] = "Сб",
            [DayOfWeek.Sunday] = "Вс"
        };

        // Получаем коэффициенты из первых дней прогноза (они одинаковые для всех)
        var dowCoeffs = prediction.DailyForecasts
            .GroupBy(d => d.Date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First().DayOfWeekFactor);

        for (int i = 0; i < 4; i++)
        {
            DowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        int idx = 0;
        foreach (var dow in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            var coeff = dowCoeffs.GetValueOrDefault(dow, 1.0m);
            var border = new Border
            {
                Background = coeff > 1.1m ? new SolidColorBrush(Color.FromRgb(209, 250, 229)) :
                            coeff < 0.9m ? new SolidColorBrush(Color.FromRgb(254, 226, 226)) :
                            new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(2)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = dayNames[dow], FontWeight = FontWeights.SemiBold, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = $"{coeff:F2}x", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(75, 85, 99)) });
            border.Child = stack;

            Grid.SetRow(border, idx / 2);
            Grid.SetColumn(border, idx % 2);
            DowGrid.Children.Add(border);
            idx++;
        }
    }
}