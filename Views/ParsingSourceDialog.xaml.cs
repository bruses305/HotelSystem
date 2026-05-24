using System;
using System.Windows;
using System.Windows.Controls;
using HotelSystem.Models;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class ParsingSourceDialog : Window
{
    public ParsingSource Source { get; private set; }
    private readonly IPriceParseService _parseService;

    public ParsingSourceDialog(ParsingSource? source = null)
    {
        InitializeComponent();
        _parseService = new PriceParseService();
        Source = source ?? new ParsingSource();
        Owner = Application.Current.MainWindow;
        LoadData();
    }

    private void LoadData()
    {
        UrlTextBox.Text = Source.Url;
        XPathTextBox.Text = Source.XPath;
        ClassSelectorTextBox.Text = Source.ClassBasedSelector;

        if (Source.LastSuccessfulParse != default)
        {
            LastParseBorder.Visibility = Visibility.Visible;
            LastParseDateText.Text = $"Дата: {Source.LastSuccessfulParse:dd.MM.yyyy HH:mm}";
            LastParseValueText.Text = $"Цена: {Source.LastParsedValue:F2}";
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        TestButton.Content = "⏳ Проверка...";
        TestResultText.Text = "";

        var result = await _parseService.FetchPriceAsync(UrlTextBox.Text, XPathTextBox.Text);

        switch (result.Type)
        {
            case ParseResultType.Success:
                TestResultText.Text = $"✅ Успешно! Цена: {result.Value:F2}";
                TestResultText.Foreground = System.Windows.Media.Brushes.Green;
                ClassSelectorTextBox.Text = result.ClassBasedSelector;
                // Сохраняем значение для передачи
                Source.LastParsedValue = result.Value;
                Source.LastSuccessfulParse = DateTime.Now;
                break;
            case ParseResultType.NoInternet:
                TestResultText.Text = "❌ Нет подключения к интернету";
                TestResultText.Foreground = System.Windows.Media.Brushes.Red;
                break;
            case ParseResultType.SiteUnavailable:
                TestResultText.Text = $"❌ Сайт недоступен";
                TestResultText.Foreground = System.Windows.Media.Brushes.Red;
                break;
            case ParseResultType.ValueNotFound:
                TestResultText.Text = "❌ Значение не найдено (XPath)";
                TestResultText.Foreground = System.Windows.Media.Brushes.Red;
                break;
            default:
                TestResultText.Text = $"❌ {result.ErrorMessage}";
                TestResultText.Foreground = System.Windows.Media.Brushes.Red;
                break;
        }

        TestButton.IsEnabled = true;
        TestButton.Content = "Проверить";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
        {
            MessageBox.Show("Введите URL", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(XPathTextBox.Text))
        {
            MessageBox.Show("Введите XPath", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Source.Url = UrlTextBox.Text.Trim();
        Source.XPath = XPathTextBox.Text.Trim();
        Source.ClassBasedSelector = ClassSelectorTextBox.Text.Trim();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void HelpUrl_Click(object sender, RoutedEventArgs e)
    {
        var help = new HelpDialog(
            "Как получить ссылку",
            "1. Откройте сайт с ценой в браузере\n" +
            "2. Скопируйте URL из адресной строки\n" +
            "3. Вставьте в это поле\n\n" +
            "Пример: https://example.com/tariff");
        help.Owner = this;
        help.ShowDialog();
    }

    private void HelpXPath_Click(object sender, RoutedEventArgs e)
    {
        var help = new HelpDialog(
            "Как получить XPath",
            "1. На странице с ценой нажмите F12 (Инструменты разработчика)\n" +
            "2. Нажмите на значок выбора элемента (стрелка ↖️)\n" +
            "3. Кликните на цену на странице\n" +
            "4. В панели Elements нажмите правой кнопкой на подсвеченной строке\n" +
            "5. Выберите Copy → Copy XPath\n" +
            "6. Вставьте в это поле\n\n" +
            "Пример: //div[@class='price']/span[2]");
        help.Owner = this;
        help.ShowDialog();
    }

    private void PasteUrl_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            UrlTextBox.Text = Clipboard.GetText();
        }
        catch
        {
            MessageBox.Show("Не удалось вставить из буфера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}