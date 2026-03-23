using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HotelSystem.Views;

public partial class PaymentDialog : Window
{
    public decimal PaymentAmount { get; private set; }
    private readonly decimal _remainingAmount;

    public PaymentDialog(decimal totalPrice, decimal paidAmount)
    {
        InitializeComponent();

        _remainingAmount = totalPrice - paidAmount;

        TotalText.Text = $"{totalPrice:N0}Br";
        PaidText.Text = $"{paidAmount:N0}Br";
        RemainingText.Text = $"{_remainingAmount:N0}Br";
        AmountTextBox.Text = _remainingAmount.ToString("N0");
    }

    private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void Pay_Click(object sender, RoutedEventArgs e)
    {
        var raw = AmountTextBox.Text.Replace("Br", "").Replace(" ", "").Trim();

        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            MessageBox.Show("Введите корректную сумму.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (amount <= 0)
        {
            MessageBox.Show("Сумма оплаты должна быть больше нуля.", "Ошибка", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (amount > _remainingAmount)
        {
            var result = MessageBox.Show(
                $"Сумма превышает остаток на {amount - _remainingAmount:N0} Br. Продолжить?",
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
}
