using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class RoomDialog : DialogBase
{
    public Room Room { get; private set; }
    private bool _dataLoaded;
    private readonly IRoomCostCalculationService _costCalculationService;
        
    public RoomDialog(Room? room = null)
    {
        InitializeComponent();
        Room = room ?? new Room();
        _costCalculationService = ServiceLocator.GetService<IRoomCostCalculationService>();
        LoadData();
        
        // Подписка на изменения после загрузки данных
        NameTextBox.TextChanged += MarkAsChanged;
        ProfitTextBox.TextChanged += AreaTextBox_TextChanged;
        AreaTextBox.TextChanged += AreaTextBox_TextChanged;
        CapacityTextBox.TextChanged += MarkAsChanged;
        DescriptionTextBox.TextChanged += MarkAsChanged;
        TypeComboBox.SelectionChanged += MarkAsChanged;
    }

    private void MarkAsChanged(object sender, EventArgs e)
    {
        _dataLoaded = true; // Теперь это флаг "были изменения"
    }

    protected override bool HasChanges => _dataLoaded;
    
    protected override void Save()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Введите название номера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(ProfitTextBox.Text, out var profit) || profit < 0)
        {
            MessageBox.Show("Введите корректную прибыль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Room.Name = NameTextBox.Text;
        Room.Profit = profit;
        Room.Cost = decimal.TryParse(CostTextBox.Text, out var cost) ? cost : 0;
        Room.Capacity = int.TryParse(CapacityTextBox.Text, out var cap) ? cap : 1;
        Room.Description = DescriptionTextBox.Text;
        Room.Type = Enum.Parse<RoomType>((TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Стандартный");
        Room.Area = decimal.TryParse(AreaTextBox.Text, out var area) ? area : 0;
        
        MarkAsSaved();
        DialogResult = true;
        CloseWithoutPrompt();
    }

    protected override void Cancel()
    {
        base.Cancel();
        CloseWithoutPrompt();
    }

    private async void LoadData()
    {
        NameTextBox.Text = Room.Name;
        ProfitTextBox.Text = Room.Profit.ToString();
        AreaTextBox.Text = Room.Area.ToString();
        CapacityTextBox.Text = Room.Capacity.ToString();
        DescriptionTextBox.Text = Room.Description;
        
        foreach (ComboBoxItem item in TypeComboBox.Items)
            if (item.Tag?.ToString() == Room.Type.ToString()) { TypeComboBox.SelectedItem = item; break; }

        // Авторасчёт себестоимости и цены
        await UpdateCostAndPriceAsync();
    }

    private async void AreaTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _dataLoaded = true; // Были изменения
        await UpdateCostAndPriceAsync();
    }

    private async Task UpdateCostAndPriceAsync()
    {
        if (decimal.TryParse(AreaTextBox.Text, out var area) && area > 0)
        {
            try
            {
                // Рассчитываем себестоимость через сервис
                var room = new Room { Area = area };
                var calculatedCost = await _costCalculationService.CalculateRoomCostAsync(room);
                
                CostTextBox.Text = calculatedCost.ToString();
                
                if (decimal.TryParse(ProfitTextBox.Text, out var profit))
                {
                    PriceTextBox.Text = (profit + calculatedCost).ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчёта себестоимости: {ex.Message}");
            }
        }
        else
        {
            PriceTextBox.Text = "0";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
