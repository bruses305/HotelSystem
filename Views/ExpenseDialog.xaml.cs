using System;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using HotelSystem.Models;
using HotelSystem.Models.Entities;
using HotelSystem.Views;

namespace HotelSystem.Views;

public partial class ExpenseDialog : Window
{
    public Expense Expense { get; private set; }
    private ParsingSource? _parsingSource;
    private bool _dataLoaded;
    private bool _isSaved;
    private bool _isCancelled;

    public ExpenseDialog() : this(new Expense()) { }

    public ExpenseDialog(Expense expense)
    {
        InitializeComponent();
        Expense = expense;
        LoadData();
        _dataLoaded = true;
        Owner = Application.Current.MainWindow;
    }

    private void LoadData()
    {
        NameTextBox.Text = Expense.Name;
        DescriptionTextBox.Text = Expense.Description;
        
        // Загрузка полей разбиения
        if (Expense.Quantity.HasValue)
        {
            // Загружаем разбитые поля
            UnitPriceTextBox.Text = Expense.UnitPrice?.ToString() ?? "";
            QuantityTextBox.Text = Expense.Quantity.Value.ToString();
            UnitNameTextBox.Text = Expense.UnitName ?? "";
            SplitGrid.Visibility = Visibility.Visible;
            AmountTextBox.IsEnabled = false;
            UpdateUnitLabelText();
            // Сумма вычисляется автоматически
            CalculateAmountFromSplit();
        }
        else
        {
            // Просто загружаем сумму
            AmountTextBox.Text = Expense.Amount.ToString();
        }
        
        // Загрузка парсинга
        if (!string.IsNullOrEmpty(Expense.PriceSourceJson))
        {
            _parsingSource = JsonConvert.DeserializeObject<ParsingSource>(Expense.PriceSourceJson);
            UpdateParsingStatus();
        }
        
        // Заполняем ComboBox из TransactionCategory enum
        foreach (TransactionCategory category in Enum.GetValues(typeof(TransactionCategory)))
        {
            CategoryComboBox.Items.Add(new ComboBoxItem
            {
                Content = category.ToString(),
                Tag = category.ToString()
            });
        }
        
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
    }

    private void UpdateUnitLabelText()
    {
        var unitName = UnitNameTextBox.Text?.Trim();
        UnitLabelText.Text = string.IsNullOrEmpty(unitName) ? "Цена за 1" : $"Цена за 1 {unitName}";
    }

    private void UpdateParsingStatus()
    {
        if (_parsingSource != null)
        {
            ParsingStatusText.Text = _parsingSource.LastSuccessfulParse != default
                ? $"✅ Настроено (последнее: {_parsingSource.LastSuccessfulParse:dd.MM.yyyy})"
                : "⚙ Настроено (не проверялось)";
        }
    }

    private bool HasChanges => _dataLoaded;

    private void Save()
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
        Expense.Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? TransactionCategory.Бронирование.ToString();
        Expense.LastPaymentDate = LastPaymentDatePicker.SelectedDate.Value;
        
        // Сохранение полей разбиения
        if (SplitGrid.Visibility == Visibility.Visible)
        {
            Expense.UnitPrice = decimal.TryParse(UnitPriceTextBox.Text, out var unitPrice) ? unitPrice : null;
            Expense.Quantity = decimal.TryParse(QuantityTextBox.Text, out var quantity) ? quantity : null;
            Expense.UnitName = UnitNameTextBox.Text?.Trim() ?? "";
        }
        else
        {
            // При закрытии разбиения сбрасываем
            Expense.UnitPrice = null;
            Expense.Quantity = null;
            Expense.UnitName = "";
        }
        
        // Сохранение парсинга
        Expense.PriceSourceJson = _parsingSource != null ? JsonConvert.SerializeObject(_parsingSource) : null;

        _isSaved = true;
        DialogResult = true;
        Close();
    }

    private void Cancel()
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

    private void SplitAmount_Click(object sender, RoutedEventArgs e)
    {
        if (SplitGrid.Visibility == Visibility.Collapsed)
        {
            // Открыть разбиение
            SplitGrid.Visibility = Visibility.Visible;
            AmountTextBox.IsEnabled = false;
            
            // Если сумма есть, разбить её
            if (decimal.TryParse(AmountTextBox.Text, out var amount))
            {
                if (decimal.TryParse(QuantityTextBox.Text, out var qty) && qty > 0)
                {
                    UnitPriceTextBox.Text = (amount / qty).ToString();
                }
                else
                {
                    UnitPriceTextBox.Text = amount.ToString();
                    QuantityTextBox.Text = "1";
                }
            }
            UpdateUnitLabelText();
        }
        else
        {
            // Закрыть разбиение
            CalculateAmountFromSplit();
            SplitGrid.Visibility = Visibility.Collapsed;
            AmountTextBox.IsEnabled = true;
            Expense.Quantity = null; // Сброс при закрытии
            Expense.UnitPrice = null;
            Expense.UnitName = "";
        }
    }

    private void UnitPriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SplitGrid.Visibility == Visibility.Visible)
        {
            CalculateAmountFromSplit();
        }
    }

    private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SplitGrid.Visibility == Visibility.Visible)
        {
            CalculateAmountFromSplit();
        }
    }

    private void CalculateAmountFromSplit()
    {
        if (decimal.TryParse(UnitPriceTextBox.Text, out var price) && 
            decimal.TryParse(QuantityTextBox.Text, out var qty))
        {
            var total = price * qty;
            AmountTextBox.Text = total.ToString();
        }
    }

    private void UnitNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateUnitLabelText();
    }

    private void ParsingSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ParsingSourceDialog(_parsingSource);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            _parsingSource = dialog.Source;
            UpdateParsingStatus();
            
            // Если парсинг был успешным, передаём значение в UnitPriceTextBox
            if (_parsingSource != null && _parsingSource.LastParsedValue > 0)
            {
                // Если разбиение открыто, ставим цену в UnitPriceTextBox
                if (SplitGrid.Visibility == Visibility.Visible)
                {
                    UnitPriceTextBox.Text = _parsingSource.LastParsedValue.ToString();
                    CalculateAmountFromSplit();
                }
                else
                {
                    // Если разбиение закрыто, открываем его и ставим цену
                    SplitGrid.Visibility = Visibility.Visible;
                    AmountTextBox.IsEnabled = false;
                    UnitPriceTextBox.Text = _parsingSource.LastParsedValue.ToString();
                    QuantityTextBox.Text = "1";
                    UpdateUnitLabelText();
                    CalculateAmountFromSplit();
                }
            }
        }
    }
}