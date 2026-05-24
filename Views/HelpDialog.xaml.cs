using System.Windows;

namespace HotelSystem.Views;

public partial class HelpDialog : Window
{
    public HelpDialog(string title, string content)
    {
        InitializeComponent();
        TitleText.Text = title;
        ContentText.Text = content;
        Owner = Application.Current.MainWindow;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}