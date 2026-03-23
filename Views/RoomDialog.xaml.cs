using System.Windows;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class RoomDialog : Window
{
 public Room Room { get; private set; }
 private readonly bool _isEdit;
 private bool _isSaved = false;

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
 if (string.IsNullOrWhiteSpace(NameTextBox.Text))
 {
 MessageBox.Show("Введите название номера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 if (!decimal.TryParse(PriceTextBox.Text, out var price) || price< 0)
 {
 MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
 return;
 }

 Room.Name = NameTextBox.Text;
 Room.Price = price;
 Room.Capacity = int.TryParse(CapacityTextBox.Text, out var cap) ? cap :1;
 Room.Description = DescriptionTextBox.Text;
 Room.Type = Enum.Parse<RoomType>((TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Standard");
 Room.WaterExpense = decimal.TryParse(WaterTextBox.Text, out var w) ? w :0;
 Room.ElectricityExpense = decimal.TryParse(ElectricityTextBox.Text, out var el) ? el :0;
 Room.InternetExpense = decimal.TryParse(InternetTextBox.Text, out var inter) ? inter :0;
 Room.CleaningExpense = decimal.TryParse(CleaningTextBox.Text, out var cl) ? cl :0;

 _isSaved = true;
 DialogResult = true;
 Close();
 }

 private void Cancel_Click(object sender, RoutedEventArgs e)
 {
 Close();
 }

 private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
 {
 if (_isSaved) return;

 var currentName = NameTextBox.Text ?? "";
 var currentPrice = decimal.TryParse(PriceTextBox.Text, out var p) ? p :0;
 var currentCapacity = int.TryParse(CapacityTextBox.Text, out var c) ? c :0;
 var currentDescription = DescriptionTextBox.Text ?? "";

 bool hasChanges = _isEdit
 ? currentName != _originalName || currentPrice != _originalPrice ||
 currentCapacity != _originalCapacity || currentDescription != _originalDescription
 : !string.IsNullOrEmpty(currentName) || currentPrice >0 || currentCapacity >0 ||
 !string.IsNullOrEmpty(currentDescription);

 if (hasChanges)
 {
 var result = MessageBox.Show("Есть несохранённые изменения. Закрыть?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
 if (result == MessageBoxResult.No)
 {
 e.Cancel = true;
 }
 }
 }
}
