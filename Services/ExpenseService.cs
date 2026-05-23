using System.Windows;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ILogService _logService;

    public ExpenseService(IExpenseRepository expenseRepository, ILogService logService)
    {
        _expenseRepository = expenseRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Expense>> GetAllExpensesAsync()
    {
        return await _expenseRepository.GetAllAsync();
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        return await _expenseRepository.GetByIdAsync(id);
    }

    public async Task<Expense> CreateExpenseAsync(Expense expense)
    {
        var created = await _expenseRepository.AddAsync(expense);
        await _logService.LogAsync(LogLevel.Средние, $"Создание расхода: {expense.Name} ({expense.Category}) на сумму {expense.Amount} руб.", "ExpenseService");
        return created;
    }

    public async Task UpdateExpenseAsync(Expense expense)
    {
        await _expenseRepository.UpdateAsync(expense);
        await _logService.LogAsync(LogLevel.Обычные, $"Обновление расхода: {expense.Name} (ID: {expense.Id})", "ExpenseService");
    }

    public async Task DeleteExpenseAsync(int id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense != null)
        {
            await _expenseRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Важные, $"Удаление расхода: {expense.Name} (ID: {expense.Id})", "ExpenseService");
        }
    }

    public async Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category)
    {
        return await _expenseRepository.GetExpensesByCategoryAsync(category);
    }

    public async Task<IEnumerable<Expense>> GetOverdueExpensesAsync(int weeks)
    {
        return await _expenseRepository.GetOverdueExpensesAsync(weeks);
    }

    public async Task PayExpenseAsync(int id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense != null)
        {
            expense.LastPaymentDate = DateTime.Now;
            expense.UpdatedAt = DateTime.Now;
            await _expenseRepository.UpdateAsync(expense);
            await _logService.LogAsync(LogLevel.Обычные, $"Оплата расхода: {expense.Name} (ID: {expense.Id}), сумма: {expense.Amount} руб.", "ExpenseService");
        }
    }

    public async Task<decimal> GetTotalExpensesAsync()
    {
        return await _expenseRepository.GetAllAsync()
            .ContinueWith(t => t.Result.Sum(e => e.Amount));
    }

    public async Task<decimal> GetTotalExpensesByCategoryAsync(string category)
    {
        return await _expenseRepository.GetTotalExpensesByCategoryAsync(category);
    }
}