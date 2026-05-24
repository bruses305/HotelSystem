using System.Windows;
using System.Windows.Controls;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class SettingsView : Page
{
    public SettingsView()
    {
        InitializeComponent();
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        var settings = SettingsService.Instance.Settings;
        HotelNameTextBox.Text = settings.HotelName;
        AddressTextBox.Text = settings.Address;
        PhoneTextBox.Text = settings.Phone;
        LogRetentionTextBox.Text = settings.LogRetentionDays.ToString();
    }
    
    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Instance.Settings;
        settings.HotelName = HotelNameTextBox.Text;
        settings.Address = AddressTextBox.Text;
        settings.Phone = PhoneTextBox.Text;
        
        if (int.TryParse(LogRetentionTextBox.Text, out var days) && days > 0)
            settings.LogRetentionDays = days;
        
        SettingsService.Instance.SaveSettings();
    }
    
    private void ResetSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.ResetSettings();
        LoadSettings();
    }
}


