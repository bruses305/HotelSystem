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
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РІР‚ВР РЋР вЂљР В РЎвЂўР В Р вЂ¦Р В РЎвЂР РЋР вЂљР В РЎвЂўР В Р вЂ Р В Р’В°Р В Р вЂ¦Р В РЎвЂР В Р’Вµ", Tag = TransactionCategory.Booking });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РІР‚СњР В РЎвЂўР В РЎвЂ”Р В РЎвЂўР В Р’В»Р В Р вЂ¦Р В РЎвЂР РЋРІР‚С™Р В Р’ВµР В Р’В»Р РЋР Р‰Р В Р вЂ¦Р В Р’В°Р РЋР РЏ Р РЋРЎвЂњР РЋР С“Р В Р’В»Р РЋРЎвЂњР В РЎвЂ“Р В Р’В°", Tag = TransactionCategory.AdditionalService });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РІР‚вЂќР В Р’В°Р РЋР вЂљР В РЎвЂ”Р В Р’В»Р В Р’В°Р РЋРІР‚С™Р В Р’В°", Tag = TransactionCategory.Salary });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РЎв„ўР В РЎвЂўР В РЎВР В РЎВР РЋРЎвЂњР В Р вЂ¦Р В Р’В°Р В Р’В»Р РЋР Р‰Р В Р вЂ¦Р РЋРІР‚в„–Р В Р’Вµ Р РЋРЎвЂњР РЋР С“Р В Р’В»Р РЋРЎвЂњР В РЎвЂ“Р В РЎвЂ", Tag = TransactionCategory.Utilities });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РІР‚вЂќР В Р’В°Р В РЎвЂќР РЋРЎвЂњР В РЎвЂ”Р В РЎвЂќР В РЎвЂ", Tag = TransactionCategory.Purchase });
        CategoryComboBox.Items.Add(new ComboBoxItem { Content = "Р В РЎвЂєР В Р’В±Р РЋР С“Р В Р’В»Р РЋРЎвЂњР В Р’В¶Р В РЎвЂР В Р вЂ Р В Р’В°Р В Р вЂ¦Р В РЎвЂР В Р’Вµ", Tag = TransactionCategory.Maintenance });
        TypeComboBox.SelectedIndex = 0;
        CategoryComboBox.SelectedIndex = 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount <= 0) { MessageBox.Show("Р В РІР‚в„ўР В Р вЂ Р В Р’ВµР В РўвЂР В РЎвЂР РЋРІР‚С™Р В Р’Вµ Р В РЎвЂќР В РЎвЂўР РЋР вЂљР РЋР вЂљР В Р’ВµР В РЎвЂќР РЋРІР‚С™Р В Р вЂ¦Р РЋРЎвЂњР РЋР вЂ№ Р РЋР С“Р РЋРЎвЂњР В РЎВР В РЎВР РЋРЎвЂњ", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
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
