using System.Windows;
using HotelSystem.Repositories;using HotelSystem.Services;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class ServiceDialog : Window
{
    public Service Service { get; private set; }
    private readonly bool _isEdit;
    private bool _isSaved = false;
    
    // Р С›РЎР‚Р С‘Р С–Р С‘Р Р…Р В°Р В»РЎРЉР Р…РЎвЂ№Р Вµ Р В·Р Р…Р В°РЎвЂЎР ВµР Р…Р С‘РЎРЏ
    private string _originalName = "";
    private string _originalDescription = "";
    private decimal _originalPrice;

    public ServiceDialog(Service? service = null)
    {
        InitializeComponent();
        _isEdit = service != null;
        Service = service ?? new Service();
        if (_isEdit) InitializeForm();
    }
    
    private void InitializeForm()
    {
        NameTextBox.Text = Service.Name;
        DescriptionTextBox.Text = Service.Description;
        PriceTextBox.Text = Service.Price.ToString();
        IsActiveCheckBox.IsChecked = Service.IsActive;
        
        _originalName = Service.Name ?? "";
        _originalDescription = Service.Description ?? "";
        _originalPrice = Service.Price;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text)) { MessageBox.Show("Р вЂ™Р Р†Р ВµР Т‘Р С‘РЎвЂљР Вµ Р Р…Р В°Р В·Р Р†Р В°Р Р…Р С‘Р Вµ", "Р С›РЎв‚¬Р С‘Р В±Р С”Р В°", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (!decimal.TryParse(PriceTextBox.Text, out var price) || price < 0) { MessageBox.Show("Р вЂ™Р Р†Р ВµР Т‘Р С‘РЎвЂљР Вµ Р С”Р С•РЎР‚РЎР‚Р ВµР С”РЎвЂљР Р…РЎС“РЎР‹ РЎвЂ Р ВµР Р…РЎС“", "Р С›РЎв‚¬Р С‘Р В±Р С”Р В°", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        
        Service.Name = NameTextBox.Text;
        Service.Description = DescriptionTextBox.Text;
        Service.Price = price;
        Service.IsActive = IsActiveCheckBox.IsChecked ?? true;
        
        _isSaved = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { Close(); }
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isSaved) return;
        
        var currentName = NameTextBox.Text ?? "";
        var currentDescription = DescriptionTextBox.Text ?? "";
        var currentPrice = decimal.TryParse(PriceTextBox.Text, out var p) ? p : 0;
        
        bool hasChanges = _isEdit
            ? currentName != _originalName || currentDescription != _originalDescription || currentPrice != _originalPrice
            : !string.IsNullOrEmpty(currentName) || !string.IsNullOrEmpty(currentDescription) || currentPrice > 0;
        
        if (hasChanges)
        {
            var result = MessageBox.Show("Р вЂўРЎРѓРЎвЂљРЎРЉ Р Р…Р ВµРЎРѓР С•РЎвЂ¦РЎР‚Р В°Р Р…РЎвЂР Р…Р Р…РЎвЂ№Р Вµ Р С‘Р В·Р СР ВµР Р…Р ВµР Р…Р С‘РЎРЏ. Р вЂ”Р В°Р С”РЎР‚РЎвЂ№РЎвЂљРЎРЉ?", "Р СџР С•Р Т‘РЎвЂљР Р†Р ВµРЎР‚Р В¶Р Т‘Р ВµР Р…Р С‘Р Вµ", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}

