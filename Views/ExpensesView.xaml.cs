using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class ExpensesView : Page
{
    private readonly IExpenseService _expenseService;
    private List<Expense> _allExpenses = new();

    public ExpensesView()
    {
        InitializeComponent();
        _expenseService = ServiceLocator.GetService<IExpenseService>();
        LoadExpensesAsync();
        CheckPermissions();
        
        // Регистрируем конвертеры
        RegisterConverters();
    }

    private void RegisterConverters()
    {
        // Конвертер для цвета статуса даты
        ExpensesGrid.Resources["DateStatusConverter"] = new DateStatusConverter();
        // Конвертер для текста статуса даты
        ExpensesGrid.Resources["DateStatusTextConverter"] = new DateStatusTextConverter();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Expenses) && FindName("AddExpenseButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
        
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses) && FindName("PayAllButton") is Button payAllButton)
        {
            payAllButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadExpensesAsync()
    {
        try
        {
            _allExpenses = (await _expenseService.GetAllExpensesAsync()).ToList();
            ExpensesGrid.ItemsSource = _allExpenses;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки расходов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddExpense_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для создания расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new ExpenseDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _expenseService.CreateExpenseAsync(dialog.Expense);
                LoadExpensesAsync();
                MessageBox.Show("Расход добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditExpense_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для редактирования расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Expense expense)
        {
            var dialog = new ExpenseDialog(expense);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _expenseService.UpdateExpenseAsync(dialog.Expense);
                LoadExpensesAsync();
            }
        }
    }

    private void ExpensesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для редактирования расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (ExpensesGrid.SelectedItem is Expense expense)
        {
            var dialog = new ExpenseDialog(expense);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _expenseService.UpdateExpenseAsync(dialog.Expense);
                LoadExpensesAsync();
            }
        }
    }

    private async void PayExpense_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для оплаты расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Expense expense)
        {
            try
            {
                await _expenseService.PayExpenseAsync(expense.Id);
                LoadExpensesAsync();
                MessageBox.Show("Расход оплачен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void PayAll_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для оплаты расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var overdueExpenses = _allExpenses.Where(e => 
        {
            var weeksSincePayment = (DateTime.Now - e.LastPaymentDate).TotalDays / 7;
            return weeksSincePayment >= 4; // Месяц или больше
        }).ToList();
        
        if (overdueExpenses.Count == 0)
        {
            MessageBox.Show("Нет расходов, которые требуют оплаты (месяц или больше с последней оплаты).", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = MessageBox.Show($"Оплатить {overdueExpenses.Count} расходов? Сумма: {overdueExpenses.Sum(e => e.Amount):N2} ₽", 
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                foreach (var expense in overdueExpenses)
                {
                    await _expenseService.PayExpenseAsync(expense.Id);
                }
                LoadExpensesAsync();
                MessageBox.Show($"Оплачено {overdueExpenses.Count} расходов!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

// Конвертер для цвета даты (красный, желтый, белый)
public class DateStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime date)
        {
            var weeksSincePayment = (DateTime.Now - date).TotalDays / 7;
            
            if (weeksSincePayment > 5) // Больше 5 недель
                return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Красный
            else if (weeksSincePayment >= 3) // 3-5 недель
                return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Желтый
            else // Менее 3 недель
                return new SolidColorBrush(Colors.White);
        }
        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Конвертер для текста статуса даты
public class DateStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime date)
        {
            var weeksSincePayment = (DateTime.Now - date).TotalDays / 7;
            
            if (weeksSincePayment > 5) // Больше 5 недель
                return "Просрочено";
            else if (weeksSincePayment >= 3) // 3-5 недель
                return "Напоминание";
            else // Менее 3 недель
                return "Актуально";
        }
        return "Актуально";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}