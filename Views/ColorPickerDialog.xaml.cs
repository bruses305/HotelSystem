using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelSystem.Views;

public partial class ColorPickerDialog : Window
{
    public string SelectedColor { get; private set; } = "#3498DB";
    
    private readonly string[] _quickColors = new[]
    {
        "#E74C3C", "#E67E22", "#F39C12", "#F1C40F",
        "#2ECC71", "#27AE60", "#1ABC9C", "#16A085",
        "#3498DB", "#2980B9", "#9B59B6", "#8E44AD",
        "#34495E", "#2C3E50", "#95A5A6", "#7F8C8D",
        "#ECF0F1", "#BDC3C7", "#FF6B6B", "#4ECDC4"
    };

    public ColorPickerDialog(string initialColor = "#3498DB")
    {
        InitializeComponent();
        SelectedColor = initialColor;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Подключаем обработчики ПОСЛЕ полной инициализации
        RedSlider.ValueChanged += Slider_ValueChanged;
        GreenSlider.ValueChanged += Slider_ValueChanged;
        BlueSlider.ValueChanged += Slider_ValueChanged;
        RedTextBox.TextChanged += TextBox_TextChanged;
        GreenTextBox.TextChanged += TextBox_TextChanged;
        BlueTextBox.TextChanged += TextBox_TextChanged;
        HexTextBox.TextChanged += HexTextBox_TextChanged;
        
        LoadQuickColors();
        SetColorFromHex(SelectedColor);
    }

    private void LoadQuickColors()
    {
        foreach (var hex in _quickColors)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var btn = new Button
                {
                    Background = new SolidColorBrush(color),
                    Tag = hex,
                    ToolTip = hex
                };
                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string h)
                    {
                        SetColorFromHex(h);
                    }
                };
                QuickColorsPanel.Children.Add(btn);
            }
            catch { }
        }
    }

    private void SetColorFromHex(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
            UpdatePreview();
        }
        catch { }
    }

    private void UpdatePreview()
    {
        byte r = (byte)RedSlider.Value;
        byte g = (byte)GreenSlider.Value;
        byte b = (byte)BlueSlider.Value;
        
        var color = Color.FromRgb(r, g, b);
        PreviewBorder.Background = new SolidColorBrush(color);
        SelectedColor = $"#{r:X2}{g:X2}{b:X2}";
        PreviewText.Text = SelectedColor.ToUpper();
        
        // Автоматический выбор цвета текста
        double brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        PreviewText.Foreground = brightness > 0.5 ? Brushes.Black : Brushes.White;
        
        // Обновляем текстбоксы
        RedTextBox.Text = r.ToString();
        GreenTextBox.Text = g.ToString();
        BlueTextBox.Text = b.ToString();
        HexTextBox.Text = SelectedColor.ToUpper();
    }

    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdatePreview();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && int.TryParse(tb.Text, out int value))
        {
            value = Math.Clamp(value, 0, 255);
            if (tb == RedTextBox && RedSlider != null) RedSlider.Value = value;
            else if (tb == GreenTextBox && GreenSlider != null) GreenSlider.Value = value;
            else if (tb == BlueTextBox && BlueSlider != null) BlueSlider.Value = value;
        }
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb && tb.Text.StartsWith("#") && tb.Text.Length == 7)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(tb.Text);
                if (RedSlider != null) RedSlider.Value = color.R;
                if (GreenSlider != null) GreenSlider.Value = color.G;
                if (BlueSlider != null) BlueSlider.Value = color.B;
                UpdatePreview();
            }
            catch { }
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
