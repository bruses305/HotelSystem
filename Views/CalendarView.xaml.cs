using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class CalendarView : Page
{
    private readonly IBookingService _bookingService;
    private readonly IRoomService _roomService;
    private DateTime _currentMonth;
    private List<Booking> _allBookings = new();
    private int? _selectedRoomId;

    public CalendarView()
    {
        InitializeComponent();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        LoadRoomsAsync();
        LoadDataAsync();
    }

    private async void LoadRoomsAsync()
    {
        var rooms = await _roomService.GetAllRoomsAsync();
        var roomsList = new List<Room> { new Room { Id = 0, Name = "Все номера" } };
        roomsList.AddRange(rooms);
        RoomFilterComboBox.ItemsSource = roomsList;
        RoomFilterComboBox.DisplayMemberPath = "Name";
        RoomFilterComboBox.SelectedValuePath = "Id";
        RoomFilterComboBox.SelectedIndex = 0;
    }

    private async void LoadDataAsync()
    {
        try
        {
            var startDate = _currentMonth.AddMonths(-1);
            var endDate = _currentMonth.AddMonths(2);
            _allBookings = (await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate)).ToList();
            RenderCalendar();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RenderCalendar()
    {
        MonthYearText.Text = _currentMonth.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
        CalendarGrid.Children.Clear();
        
        // Фильтруем бронирования по выбранному номеру
        var bookings = _selectedRoomId.HasValue && _selectedRoomId.Value > 0
            ? _allBookings.Where(b => b.RoomId == _selectedRoomId.Value).ToList()
            : _allBookings;
        
        var firstDay = _currentMonth;
        var daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
        var startDayOfWeek = ((int)firstDay.DayOfWeek + 6) % 7;
        for (int i = 0; i < startDayOfWeek; i++) CalendarGrid.Children.Add(new Border { Background = Brushes.Transparent });
        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
            var dayBookings = bookings.Where(b => date >= b.CheckInDate && date < b.CheckOutDate).ToList();
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
            if (dayBookings.Any()) border.Background = new SolidColorBrush(Color.FromArgb(50, 52, 152, 219));
            if (date == DateTime.Today) { border.BorderBrush = new SolidColorBrush(Color.FromRgb(39, 174, 96)); border.BorderThickness = new Thickness(2); }
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = day.ToString(), FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            foreach (var booking in dayBookings.Take(2)) stack.Children.Add(new TextBlock { Text = booking.Room?.Name ?? " -", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)), TextTrimming = TextTrimming.CharacterEllipsis });
            if (dayBookings.Count > 2) stack.Children.Add(new TextBlock { Text = "+" + (dayBookings.Count - 2), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)) });
            border.Child = stack;
            CalendarGrid.Children.Add(border);
        }
    }

    private void PreviousMonth_Click(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(-1); LoadDataAsync(); }
    private void NextMonth_Click(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(1); LoadDataAsync(); }
    
    private void RoomFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedRoomId = RoomFilterComboBox.SelectedValue as int?;
        RenderCalendar();
    }
    
    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        RoomFilterComboBox.SelectedIndex = 0;
    }
}
