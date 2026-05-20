using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class PaymentDialog : Window
{
    public decimal PaymentAmount { get; private set; }
    private readonly decimal _remainingAmount;

    public PaymentDialog(decimal totalPrice, decimal paidAmount)
    {
        InitializeComponent();

        _remainingAmount = totalPrice - paidAmount;

        TotalText.Text = AppConstants.FormatPrice(totalPrice);
        PaidText.Text = AppConstants.FormatPrice(paidAmount);
        RemainingText.Text = AppConstants.FormatPrice(_remainingAmount);
        AmountTextBox.Text = _remainingAmount.ToString("N0");
    }

    private void Pay_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountTextBox.Text, out var amount))
        {
            MessageBox.Show("Введите корректную сумму", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (amount > _remainingAmount)
        {
            var result = MessageBox.Show(
                $"Сумма превышает остаток на {AppConstants.FormatPrice(amount - _remainingAmount)}. Продолжить?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        PaymentAmount = amount;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Можно добавить валидацию при изменении суммы
    }
}
