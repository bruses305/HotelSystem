using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class ExpensePriceUpdateDialog : Window
{
    private readonly IExpensePriceUpdateService _updateService;
    private readonly List<Expense> _expensesWithParser;
    
    public ExpensePriceUpdateDialog(List<Expense> expensesWithParser)
    {
        InitializeComponent();
        _updateService = ServiceLocator.GetService<IExpensePriceUpdateService>();
        _expensesWithParser = expensesWithParser;
        
        // Запускаем обновление после загрузки окна
        Loaded += async (s, e) => await StartUpdateAsync();
    }

    private async Task StartUpdateAsync()
    {
        var total = _expensesWithParser.Count;
        var successCount = 0;
        var failCount = 0;
        
        ProgressBar.Maximum = total;
        ResultsPanel.Children.Clear();

        for (int i = 0; i < total; i++)
        {
            var expense = _expensesWithParser[i];

            // Обновляем прогресс
            ProgressText.Text = $"{i} из {total} загружено";
            ProgressBar.Value = i;
            CurrentItemText.Text = $"Обработка: {expense.Name}";

            // Обновляем UI
            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            try
            {
                var result = await _updateService.UpdatePriceAsync(expense);

                if (result.Success)
                {
                    successCount++;
                    var valueText = expense.UnitPrice.HasValue
                        ? $"Новая цена: {expense.UnitPrice.Value:N2} {AppConstants.Currency}"
                        : "";
                    AddResultItem(expense.Name, $"✅ {expense.Name} — обновлено", "#10B981", valueText);
                }
                else
                {
                    failCount++;
                    AddResultItem(expense.Name, $"❌ {expense.Name} — {result.Message}", "#EF4444", null);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                AddResultItem(expense.Name, $"❌ {expense.Name} — {ex.Message}", "#EF4444", null);
            }

            // Обновляем счётчики
            SuccessCountText.Text = $"✅ Успешно: {successCount}";
            FailCountText.Text = $"❌ Ошибок: {failCount}";
        }

        ProgressText.Text = "Готово!";
        CurrentItemText.Text = $"Обработано {total} расходов";
        CloseButton.IsEnabled = true;
        CloseButton.Content = "Закрыть";
    }

    private void AddResultItem(string name, string text, string color, string? valueText)
    {
        var border = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB")),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 6)
        };
        
        var stack = new StackPanel();
        
        var mainText = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(mainText);
        
        if (!string.IsNullOrEmpty(valueText))
        {
            var detailText = new TextBlock
            {
                Text = valueText,
                Foreground = new SolidColorBrush(Colors.Gray),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stack.Children.Add(detailText);
        }
        
        border.Child = stack;
        ResultsPanel.Children.Add(border);
        
        // Автопрокрутка к последнему элементу
        if (ResultsPanel.Parent is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToEnd();
        }
    }
        
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
