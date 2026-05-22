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

    public RoomsView() : this(null) { }

    public RoomsView(int? highlightRoomId = null)
    {
        InitializeComponent();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _highlightRoomId = highlightRoomId;
        LoadRoomsAsync();
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Rooms) && FindName("AddRoomButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadRoomsAsync()
    {
        try
        {
            _allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
            RoomsGrid.ItemsSource = _allRooms;

            // Выделение нужного номера
            if (_highlightRoomId.HasValue)
            {
                var index = _allRooms.FindIndex(r => r.Id == _highlightRoomId.Value);
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