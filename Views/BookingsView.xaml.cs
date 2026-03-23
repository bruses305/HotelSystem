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
            var bookings = (await _bookingService.GetAllBookingsWithDetailsAsync()).ToList();
            
            foreach (var booking in bookings.Where(b => b.Status == BookingStatus.Confirmed && b.CheckOutDate < DateTime.Today))
            {
                booking.Status = BookingStatus.Completed;
                await _bookingService.UpdateBookingAsync(booking);
            }
            
            BookingsGrid.ItemsSource = bookings;
            
            if (_highlightBookingId.HasValue)
            { 
                var index = bookings.ToList().FindIndex(b => b.Id == _highlightBookingId.Value);
                if (index >= 0)
                {
                    BookingsGrid.SelectedIndex = index;
                    BookingsGrid.ScrollIntoView(BookingsGrid.Items[index]);
                }
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Данные не загруженны", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void AddBooking_Click(object sender, RoutedEventArgs e)
    {
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

    private async void ApplyFilter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var startDate = FilterStartDatePicker.SelectedDate;
            var endDate = FilterEndDatePicker.SelectedDate;
            var roomId = FilterRoomComboBox.SelectedValue as int?;
            
            var allBookings = (await _bookingService.GetAllBookingsWithDetailsAsync()).ToList();
            var filtered = allBookings.AsEnumerable();
            
            if (startDate.HasValue)
            {
                filtered = filtered.Where(b => b.CheckOutDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                filtered = filtered.Where(b => b.CheckInDate <= endDate.Value);
            }
            
            if (roomId.HasValue && roomId.Value > 0)
            {
                filtered = filtered.Where(b => b.RoomId == roomId.Value);
            }
            
            BookingsGrid.ItemsSource = filtered.ToList();
        }
        catch (Exception ex) 
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Фильтры не применены", MessageBoxButton.OK, MessageBoxImage.Error); 
        }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        FilterStartDatePicker.SelectedDate = null;
        FilterEndDatePicker.SelectedDate = null;
        FilterRoomComboBox.SelectedIndex = 0;
        LoadBookingsAsync();
    }

    private async void CheckIn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            await _bookingService.CheckInAsync(booking.Id);
            LoadBookingsAsync();
            MessageBox.Show("Гость успешно заселён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void CheckOut_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var result = MessageBox.Show("Выселить гостя?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                booking.Status = BookingStatus.Completed;
                await _bookingService.CompleteBookingAsync(booking.Id);
                LoadBookingsAsync();
                MessageBox.Show("Гость выселен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void CancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Booking booking)
        {
            var result = MessageBox.Show("Отменить бронирование?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                booking.Status = BookingStatus.Cancelled;
                await _bookingService.UpdateBookingAsync(booking);
                LoadBookingsAsync(); 
            }
        }
    }

    private async void PayBooking_Click(object sender, RoutedEventArgs e)
    {
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
        if (BookingsGrid.SelectedItem is Booking booking)
        {
            EditBooking_Click(sender, new RoutedEventArgs());
        }
    }
}


