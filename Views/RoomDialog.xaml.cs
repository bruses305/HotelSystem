using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class RoomDialog : DialogBase
{
    public Room Room { get; private set; }
    private readonly bool _isEdit;
        
    private string _originalName = "";
    private decimal _originalPrice;
    private int _originalCapacity;
    private string _originalDescription = "";

    public RoomDialog(Room? room = null)
    {
        InitializeComponent();
        _isEdit = room != null;
        Room = room ?? new Room();
        if (_isEdit) InitializeForm();
    }

    protected override bool HasChanges => 
        NameTextBox.Text?.Trim() != _originalName ||
        decimal.TryParse(PriceTextBox.Text, out var p) && p != _originalPrice ||
        int.TryParse(CapacityTextBox.Text, out var c) && c != _originalCapacity ||
        DescriptionTextBox.Text?.Trim() != _originalDescription;
    
    protected override void Save()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBoxHelper.ShowError("Введите название номера");
            return;
        }

        if (!decimal.TryParse(PriceTextBox.Text, out var price) || price < 0)
        {
            MessageBoxHelper.ShowError("Введите корректную цену");
            return;
        }

        Room.Name = NameTextBox.Text;
        Room.Price = price;
        Room.Capacity = int.TryParse(CapacityTextBox.Text, out var cap) ? cap : 1;
        Room.Description = DescriptionTextBox.Text;
        Room.Type = Enum.Parse<RoomType>((TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Стандартный");
        Room.WaterExpense = decimal.TryParse(WaterTextBox.Text, out var w) ? w : 0;
        Room.ElectricityExpense = decimal.TryParse(ElectricityTextBox.Text, out var el) ? el : 0;
        Room.InternetExpense = decimal.TryParse(InternetTextBox.Text, out var inter) ? inter : 0;
        Room.CleaningExpense = decimal.TryParse(CleaningTextBox.Text, out var cl) ? cl : 0;
        
        MarkAsSaved();
        DialogResult = true;
        CloseWithoutPrompt();
    }
    
    protected override void Cancel()
    {
        base.Cancel();
        CloseWithoutPrompt();
    }

    private void InitializeForm()
    {
        NameTextBox.Text = Room.Name;
        PriceTextBox.Text = Room.Price.ToString();
        CapacityTextBox.Text = Room.Capacity.ToString();
        DescriptionTextBox.Text = Room.Description;
        WaterTextBox.Text = Room.WaterExpense.ToString();
        ElectricityTextBox.Text = Room.ElectricityExpense.ToString();
        InternetTextBox.Text = Room.InternetExpense.ToString();
        CleaningTextBox.Text = Room.CleaningExpense.ToString();
        foreach (ComboBoxItem item in TypeComboBox.Items)
            if (item.Tag?.ToString() == Room.Type.ToString()) { TypeComboBox.SelectedItem = item; break; }

        _originalName = Room.Name ?? "";
        _originalPrice = Room.Price;
        _originalCapacity = Room.Capacity;
        _originalDescription = Room.Description ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Логика перенесена в базовый класс
    }
}
