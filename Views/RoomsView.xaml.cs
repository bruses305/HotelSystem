using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class RoomsView : Page
{
    private readonly IRoomService _roomService;

    public RoomsView()
    {
        InitializeComponent();
        _roomService = ServiceLocator.GetService<IRoomService>();
        LoadRoomsAsync();
    }

    private async void LoadRoomsAsync()
    {
        try { RoomsGrid.ItemsSource = await _roomService.GetAllRoomsAsync(); }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void AddRoom_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RoomDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try { await _roomService.CreateRoomAsync(dialog.Room); LoadRoomsAsync(); MessageBox.Show("Номер добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    private void EditRoom_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Room room)
        {
            var dialog = new RoomDialog(room);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) { _ = _roomService.UpdateRoomAsync(dialog.Room); LoadRoomsAsync(); }
        }
    }

    private async void DeleteRoom_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Room room)
        {
            var result = MessageBox.Show($"Удалить номер {room.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) { try { await _roomService.DeleteRoomAsync(room.Id); LoadRoomsAsync(); } catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); } }
        }
    }
}

