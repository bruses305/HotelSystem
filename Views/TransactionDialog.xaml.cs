using System.Windows;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class TransactionDialog : Window
{
    public Transaction Transaction { get; private set; } = new();

    public TransactionDialog()
    {
        InitializeComponent();
        InitializeCategories();
    }

    private void InitializeCategories()
    {
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Бронирование", Tag = TransactionCategory.Booking });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Дополнительные услуги", Tag = TransactionCategory.AdditionalService });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Выплаты", Tag = TransactionCategory.Salary });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Комунальные услуги", Tag = TransactionCategory.Utilities });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Покупка", Tag = TransactionCategory.Purchase });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Инной доход", Tag = TransactionCategory.Maintenance });
        TypeComboBox.SelectedIndex = 0;
        CategoryComboBox.SelectedIndex = 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0) { MessageBox.Show("Ошибка сохранения: недостаток данных", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var typeTag = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var categoryTag = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag;
        Transaction.Type = typeTag == "Income" ? TransactionType.Income : TransactionType.Expense;
        Transaction.Category = categoryTag is TransactionCategory cat ? cat : TransactionCategory.Booking;
        Transaction.Amount = amount;
        Transaction.Description = DescriptionTextBox.Text;
        Transaction.TransactionDate = DateTime.Now;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
