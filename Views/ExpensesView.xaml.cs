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
using HotelSystem.Repositories;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class ExpensesView : Page
{
    private readonly IExpenseService _expenseService;
    private readonly IEmployeeService _employeeService;
    private readonly ITransactionRepository _transactionRepository;
    private List<Expense> _allExpenses = new();

    public ExpensesView()
    {
        InitializeComponent();
        _expenseService = ServiceLocator.GetService<IExpenseService>();
        _employeeService = ServiceLocator.GetService<IEmployeeService>();
        _transactionRepository = ServiceLocator.GetService<ITransactionRepository>();
        LoadExpensesAsync();
        CheckPermissions();
        
        // Регистрируем конвертеры
        ExpensesGrid.Resources["ExpenseStatusConverter"] = new ExpenseStatusConverter();
        ExpensesGrid.Resources["ExpenseStatusTextConverter"] = new ExpenseStatusTextConverter();
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
        
        if (!PermissionChecker.CanEdit(PermissionCategory.Expenses) && FindName("PaySalariesButton") is Button paySalariesButton)
        {
            paySalariesButton.Visibility = Visibility.Collapsed;
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

    private async void EditExpense_Click(object sender, RoutedEventArgs e)
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
                try
                {
                    await _expenseService.UpdateExpenseAsync(dialog.Expense);
                    LoadExpensesAsync();
                    MessageBox.Show("Расход обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void ExpensesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                try
                {
                    await _expenseService.UpdateExpenseAsync(dialog.Expense);
                    LoadExpensesAsync();
                    MessageBox.Show("Расход обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            var result = MessageBox.Show(
                $"Оплатить расход \"{expense.Name}\"?\nСумма: {expense.Amount:N2} ₽", 
                "Подтверждение оплаты", 
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. Обновляем дату оплаты в Expense
                    await _expenseService.PayExpenseAsync(expense.Id);
                    
                    // 2. Создаём Transaction для финансового учёта
                    var transaction = new Transaction
                    {
                        Type = TransactionType.Расход,
                        Category = Enum.Parse<TransactionCategory>(expense.Category),
                        Amount = expense.Amount,
                        TransactionDate = DateTime.Now,
                        Description = $"Оплата расхода: {expense.Name}",
                        Quantity = 1
                    };
                    await _transactionRepository.AddAsync(transaction);
                    
                    LoadExpensesAsync();
                    MessageBox.Show("Расход оплачен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
        
        var overdueExpenses = _allExpenses.Where(exp => 
        {
            var daysSincePayment = (DateTime.Now - exp.LastPaymentDate).TotalDays;
            return daysSincePayment >= 30; // Месяц или больше
        }).ToList();
        
        if (overdueExpenses.Count == 0)
        {
            MessageBox.Show("Нет расходов, которые требуют оплаты (месяц или больше с последней оплаты).", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var totalAmount = overdueExpenses.Sum(exp => exp.Amount);
        var result = MessageBox.Show(
            $"Оплатить {overdueExpenses.Count} расходов?\nОбщая сумма: {totalAmount:N2} ₽", 
            "Подтверждение", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                foreach (var expense in overdueExpenses)
                {
                    // 1. Обновляем дату оплаты
                    await _expenseService.PayExpenseAsync(expense.Id);
                    
                    // 2. Создаём Transaction для каждого расхода
                    var transaction = new Transaction
                    {
                        Type = TransactionType.Расход,
                        Category = Enum.Parse<TransactionCategory>(expense.Category),
                        Amount = expense.Amount,
                        TransactionDate = DateTime.Now,
                        Description = $"Оплата расхода: {expense.Name}",
                        Quantity = 1
                    };
                    await _transactionRepository.AddAsync(transaction);
                }
                LoadExpensesAsync();
                MessageBox.Show($"Оплачено {overdueExpenses.Count} расходов на сумму {totalAmount:N2} ₽!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void DeleteExpense_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanDelete(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для удаления расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Expense expense)
        {
            var result = MessageBox.Show($"Удалить расход \"{expense.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _expenseService.DeleteExpenseAsync(expense.Id);
                    LoadExpensesAsync();
                    MessageBox.Show("Расход удалён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void PaySalaries_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для выплаты ЗП!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            var activeEmployees = employees.Where(e => e.IsActive).ToList();
            
            if (!activeEmployees.Any())
            {
                MessageBox.Show("Нет активных сотрудников для выплаты ЗП!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var totalSalary = activeEmployees.Sum(e => e.Salary);
            var salaryText = string.Join("\n", activeEmployees.Select(e => $"{e.FullName}: {e.Salary:N0} ₽"));
            
            var result = MessageBox.Show(
                $"Выплатить зарплату {activeEmployees.Count} сотрудникам?\n\n" +
                $"Общая сумма: {totalSalary:N0} ₽\n\n" +
                $"Сотрудники:\n{salaryText}",
                "Подтверждение выплаты ЗП",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Создаём Transaction для выплаты ЗП
                var transaction = new Transaction
                {
                    Type = TransactionType.Расход,
                    Category = TransactionCategory.Зарплата,
                    Amount = totalSalary,
                    TransactionDate = DateTime.Now,
                    Description = $"Выплата ЗП за текущий месяц. Сотрудников: {activeEmployees.Count}",
                    Quantity = activeEmployees.Count
                };
                
                await _transactionRepository.AddAsync(transaction);
                LoadExpensesAsync();
                MessageBox.Show($"Зарплата выплачена {activeEmployees.Count} сотрудникам на сумму {totalSalary:N0} ₽!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка выплаты ЗП: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
        
// Конвертер для статуса оплаты
// Определяет "оплачено" если дата меньше 1 месяца назад
public class ExpenseStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime lastPaymentDate)
        {
            var daysSincePayment = (DateTime.Now - lastPaymentDate).TotalDays;
            return daysSincePayment < 30 ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }
        return new SolidColorBrush(Color.FromRgb(239, 68, 68));
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Конвертер для текста статуса оплаты
public class ExpenseStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is DateTime lastPaymentDate)
        {
            var daysSincePayment = (DateTime.Now - lastPaymentDate).TotalDays;
            return daysSincePayment < 30 ? "Оплачено" : "Не оплачено";
        }
        return "Не оплачено";
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}