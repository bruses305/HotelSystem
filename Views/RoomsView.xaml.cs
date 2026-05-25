using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class RoomsView : Page
{
    private readonly IRoomService _roomService;
    private int? _highlightRoomId;
    private List<Room> _allRooms = new();
    private List<Room> _filteredRooms = new();
    private RoomType? _selectedTypeFilter;
    private string _searchText = string.Empty;

    public RoomsView() : this(null) { }

    public RoomsView(int? highlightRoomId = null)
    {
        InitializeComponent();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _highlightRoomId = highlightRoomId;
        LoadRoomsAsync();
        CheckPermissions();
        
        // Добавляем обработчик клавиатуры для Ctrl+A
        RoomsGrid.AddHandler(KeyDownEvent, new KeyEventHandler(RoomsGrid_KeyDown), true);
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Rooms) && FindName("AddRoomButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
        
        if (!PermissionChecker.CanEdit(PermissionCategory.Rooms) && FindName("IncreaseProfitButton") is Button profitBtn)
        {
            profitBtn.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadRoomsAsync()
    {
        try
        {
            _allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
            _filteredRooms = _allRooms.ToList();
            ApplyFilters();

            // Выделение нужного номера
            if (_highlightRoomId.HasValue)
            {
                var index = _filteredRooms.FindIndex(r => r.Id == _highlightRoomId.Value);
                if (index >= 0)
                {
                    RoomsGrid.SelectedIndex = index;
                    RoomsGrid.ScrollIntoView(RoomsGrid.Items[index]);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки номеров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
        
    private void ApplyFilters()
    {
        var filtered = _allRooms.AsEnumerable();

        // Фильтр по типу
        if (_selectedTypeFilter.HasValue)
        {
            filtered = filtered.Where(r => r.Type == _selectedTypeFilter.Value);
        }
        
        // Фильтр по поиску
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(r => 
                r.Name.ToLower().Contains(search) || 
                r.Description.ToLower().Contains(search));
        }

        _filteredRooms = filtered.ToList();
        RoomsGrid.ItemsSource = _filteredRooms;
    }

    private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var tag = selectedItem.Tag?.ToString();
            _selectedTypeFilter = tag switch
            {
                "Standard" => RoomType.Стандартный,
                "Lux" => RoomType.Люкс,
                "Apartment" => RoomType.Апартаменты,
                _ => null
            };
            ApplyFilters();
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchTextBox.Text;
        ApplyFilters();
    }

    private void RoomsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FindName("IncreaseProfitButton") is Button profitBtn)
        {
            profitBtn.IsEnabled = RoomsGrid.SelectedItems.Count > 0;
            
            // Меняем цвет кнопки при выделении
            if (RoomsGrid.SelectedItems.Count > 0)
            {
                profitBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            }
            else
            {
                profitBtn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }
    }

    private void RoomsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+A - выделение всех номеров (только видимых в фильтре)
        if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            RoomsGrid.SelectAll();
            e.Handled = true;
        }
    }
        
    private async void IncreaseProfit_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Rooms))
        {
            MessageBox.Show("Недостаточно прав для изменения прибыли!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (RoomsGrid.SelectedItems.Count == 0)
        {
            MessageBox.Show("Пожалуйста, выберите номера для изменения прибыли.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var selectedRooms = RoomsGrid.SelectedItems.Cast<Room>().ToList();
        var dialog = new RoomProfitIncreaseDialog(selectedRooms);
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() == true && dialog.RoomsToUpdate.Any())
        {
            try
            {
                int successCount = 0;
                int failCount = 0;
                string errors = string.Empty;
                
                foreach (var room in dialog.RoomsToUpdate)
                {
                    try
                    {
                        // Создаём новый объект только с Id и Profit для обновления
                        var roomToUpdate = new Room
                        {
                            Id = room.Id,
                            Profit = room.Profit
                        };
                        
                        await _roomService.UpdateRoomAsync(roomToUpdate);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors += $"• {room.Name}: {ex.Message}\n";
                    }
                }
                
                LoadRoomsAsync();
                
                if (failCount == 0)
                {
                    MessageBox.Show($"Прибыль обновлена для {successCount} номеров!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Обновлено: {successCount}, Ошибок: {failCount}\n\n{errors}", "Частичный успех", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void AddRoom_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Rooms))
        {
            MessageBox.Show("Недостаточно прав для создания номеров!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new RoomDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _roomService.CreateRoomAsync(dialog.Room);
                LoadRoomsAsync();
                MessageBox.Show("Номер добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditRoom_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Rooms))
        {
            MessageBox.Show("Недостаточно прав для редактирования номеров!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Room room)
        {
            var dialog = new RoomDialog(room);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _roomService.UpdateRoomAsync(dialog.Room);
                LoadRoomsAsync();
            }
        }
    }

    private void RoomsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Rooms))
        {
            MessageBox.Show("Недостаточно прав для редактирования номеров!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (RoomsGrid.SelectedItem is Room room)
        {
            var dialog = new RoomDialog(room);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _roomService.UpdateRoomAsync(dialog.Room);
                LoadRoomsAsync();
            }
        }
    }

    private async void DeleteRoom_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanDelete(PermissionCategory.Rooms))
        {
            MessageBox.Show("Недостаточно прав для удаления номеров!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Room room)
        {
            var result = MessageBox.Show($"Удалить номер {room.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _roomService.DeleteRoomAsync(room.Id);
                    LoadRoomsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}