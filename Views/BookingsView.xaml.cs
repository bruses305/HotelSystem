using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class BookingsView : Page
{
    private readonly IBookingService _bookingService;
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IFinanceService _financeService;
    private int? _highlightBookingId;
    private List<Booking> _allBookings = new List<Booking>();
    
    public BookingsView(int? highlightBookingId = null)
    {
        InitializeComponent();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _clientService = ServiceLocator.GetService<IClientService>();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        _highlightBookingId = highlightBookingId;
        
        LoadRoomsForFilter();
        LoadBookingsAsync();
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Bookings) && FindName("AddBookingButton") is Button addButton)
        { 
            addButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadRoomsForFilter()
    {
        var roomsList = new List<Room> { new Room { Id = 0, Name = "Все номера" } };
        var rooms = await _roomService.GetAllRoomsAsync();
        roomsList.AddRange(rooms);
        FilterRoomComboBox.ItemsSource = roomsList;
        FilterRoomComboBox.DisplayMemberPath = "Name";
        FilterRoomComboBox.SelectedValuePath = "Id";
        FilterRoomComboBox.SelectedIndex = 0;
    }

    private async void LoadBookingsAsync()
    {
        try 
        {
            _allBookings = (await _bookingService.GetAllBookingsWithDetailsAsync()).ToList();
            
            foreach (var booking in _allBookings.Where(b => b.Status == BookingStatus.Подтверждено && b.CheckOutDate < DateTime.Today))
            {
                booking.Status = BookingStatus.Завершено;
                await _bookingService.UpdateBookingAsync(booking);
            }
            
            ApplyFilter();
            
            if (_highlightBookingId.HasValue)
            {
                var index = _allBookings.FindIndex(b => b.Id == _highlightBookingId.Value);
                if (index >= 0)
                {
                    BookingsGrid.SelectedIndex = index;
                    BookingsGrid.ScrollIntoView(BookingsGrid.Items[index]);
                }
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Данные не загруженны", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ApplyFilter()
    {
        if (_allBookings == null)
            return;
        
        var startDate = FilterStartDatePicker.SelectedDate;
        var endDate = FilterEndDatePicker.SelectedDate;
        var roomId = FilterRoomComboBox.SelectedValue as int?;
        var status = (FilterStatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var searchQuery = SearchTextBox.Text;
        
        var filtered = _allBookings.AsQueryable();
        
        if (startDate.HasValue)
            filtered = filtered.Where(b => b.CheckOutDate >= startDate.Value);
        
        if (endDate.HasValue)
            filtered = filtered.Where(b => b.CheckInDate <= endDate.Value.AddDays(1));
        
        if (roomId.HasValue && roomId.Value > 0)
            filtered = filtered.Where(b => b.RoomId == roomId.Value);
        
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            // Для статуса Ожидание показываем и Pending и CheckedIn
            if (status == "Pending")
                filtered = filtered.Where(b => b.Status == BookingStatus.Оплачено || b.Status == BookingStatus.Заселён);
            else
                filtered = filtered.Where(b => b.Status.ToString() == status);
        }
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var query = searchQuery.Trim().ToLower();
            filtered = filtered.Where(b =>
                b.Id.ToString().Contains(query) ||
                (b.Client != null && b.Client.FullName.ToLower().Contains(query)) ||
                (b.Client != null && b.Client.Phone != null && b.Client.Phone.ToLower().Contains(query)) ||
                b.Room.Name.ToLower().Contains(query) ||
                b.CheckInDate.ToString("dd.MM.yyyy").Contains(query)
            );
        }
        
        BookingsGrid.ItemsSource = filtered.ToList();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try { ApplyFilter(); }
        catch { /* Игнорируем ошибки при вводе */ }
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFilter();
        }
    }

    private async void AddBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для создания бронирований!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new BookingDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                await _bookingService.CreateBookingAsync(dialog.Booking); 
                LoadBookingsAsync();
                MessageBox.Show("Бронирование успешно создано!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); 
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    private void EditBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для редактирования бронирований!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var dialog = new BookingDialog(booking);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _bookingService.UpdateBookingAsync(dialog.Booking);
                LoadBookingsAsync();
            }
        }
    }

    private async void DeleteBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanDelete(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для удаления бронирований!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var result = MessageBox.Show("Удалить бронирование?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _bookingService.DeleteBookingAsync(booking.Id); 
                LoadBookingsAsync();
            }
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        FilterStartDatePicker.SelectedDate = null;
        FilterEndDatePicker.SelectedDate = null;
        FilterRoomComboBox.SelectedIndex = 0;
        FilterStatusComboBox.SelectedIndex = 0;
        ApplyFilter();
    }

    private void FilterChanged(object sender, SelectionChangedEventArgs e)
    {
        try { ApplyFilter(); }
        catch { /* Игнорируем ошибки при смене фильтров */ }
    }

    private async void CheckIn_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для заселения гостя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            await _bookingService.CheckInAsync(booking.Id);
            LoadBookingsAsync();
            MessageBox.Show("Гость успешно заселён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void CheckOut_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для выселения гостя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var result = MessageBox.Show("Выселить гостя?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                booking.Status = BookingStatus.Завершено;
                await _bookingService.CompleteBookingAsync(booking.Id);
                LoadBookingsAsync();
                MessageBox.Show("Гость выселен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void CancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для отмены бронирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var result = MessageBox.Show("Отменить бронирование?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                booking.Status = BookingStatus.Отменено;
                await _bookingService.UpdateBookingAsync(booking);
                LoadBookingsAsync();
            }
        }
    }

    private async void PayBooking_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Finance))
        {
            MessageBox.Show("Недостаточно прав для оплаты бронирования!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var amount = booking.TotalPrice - booking.PaidAmount;
            if (amount <= 0)
            {
                MessageBox.Show("Бронирование уже полностью оплачено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new PaymentDialog(booking.TotalPrice, booking.PaidAmount);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _financeService.RecordBookingPaymentAsync(booking.Id, dialog.PaymentAmount);
                    
                    LoadBookingsAsync();
                    MessageBox.Show($"Оплата {dialog.PaymentAmount:N0} Br принята!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка оплаты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BookingsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Bookings))
        {
            MessageBox.Show("Недостаточно прав для редактирования бронирований!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (BookingsGrid.SelectedItem is Booking booking)
        {
            var dialog = new BookingDialog(booking);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _bookingService.UpdateBookingAsync(dialog.Booking);
                LoadBookingsAsync();
            }
        }
    }
}


