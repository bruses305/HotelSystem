using System;
using System.Windows;
using System.Windows.Controls;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class ExpenseDialog : DialogBase
{
    public Expense Expense { get; private set; }
    private bool _dataLoaded;

    public ExpenseDialog() : this(new Expense()) { }

    public ExpenseDialog(Expense expense)
    {
        InitializeComponent();
        Expense = expense;
        LoadData();
        _dataLoaded = true;
    }

    private void LoadData()
    {
        NameTextBox.Text = Expense.Name;
        DescriptionTextBox.Text = Expense.Description;
        AmountTextBox.Text = Expense.Amount.ToString();
        
        // Установка категории
        if (!string.IsNullOrEmpty(Expense.Category))
        {
            foreach (ComboBoxItem item in CategoryComboBox.Items)
            {
                if (item.Tag?.ToString() == Expense.Category)
                {
                    CategoryComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        
        // Установка даты
        if (Expense.LastPaymentDate != default)
        {
            LastPaymentDatePicker.SelectedDate = Expense.LastPaymentDate;
        }
        else
        {
            LastPaymentDatePicker.SelectedDate = DateTime.Now;
        }

        // Статус оплаты
        IsPaidCheckBox.IsChecked = Expense.IsPaid;
    }

    protected override bool HasChanges => _dataLoaded;

    protected override void Save()
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Введите название расхода!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount < 0)
        {
            MessageBox.Show("Введите корректную сумму!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (LastPaymentDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Выберите дату последней оплаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Сохранение данных
        Expense.Name = NameTextBox.Text.Trim();
        Expense.Description = DescriptionTextBox.Text.Trim();
        Expense.Amount = amount;
        Expense.Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Другое";
        Expense.LastPaymentDate = LastPaymentDatePicker.SelectedDate.Value;
        Expense.IsPaid = IsPaidCheckBox.IsChecked ?? false;

        _isSaved = true;
        DialogResult = true;
        Close();
    }

    protected override void Cancel()
    {
        _isCancelled = true;
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancel();
    }
}