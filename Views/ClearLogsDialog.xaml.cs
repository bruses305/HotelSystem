using System.Windows;

namespace HotelSystem.Views;

public partial class ClearLogsDialog : Window
{
    public int DaysToKeep { get; private set; } = 30;

    public ClearLogsDialog()
    {
        InitializeComponent();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(DaysTextBox.Text, out var days) && days > 0)
        {
            DaysToKeep = days;
            DialogResult = true;
            Close();
        }
 else
 {
 MessageBox.Show("Введите корректное количество дней", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
 }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
