using System.Windows;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class BookingDialog : Window
{
    public Booking Booking { get; private set; }
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly bool _isEdit;
    private List<Room> _allRooms = new();
    private bool _isSaved = false;

    // Для отслеживания изменений
    private int _originalRoomId;
    private int _originalClientId;
    private DateTime _originalCheckIn;
    private DateTime _originalCheckOut;
    private string _originalNotes = "";

    public BookingDialog(Booking? booking = null)
    {
        InitializeComponent();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _clientService = ServiceLocator.GetService<IClientService>();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _isEdit = booking != null;
        Booking = booking ?? new Booking();
        InitializeForm();
    }

    private async void InitializeForm()
    {
        _allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
        RoomComboBox.ItemsSource = _allRooms;
        RoomComboBox.DisplayMemberPath = "Name";
        RoomComboBox.SelectedValuePath = "Id";
        
        var clients = await _clientService.GetAllClientsAsync();
        ClientComboBox.ItemsSource = clients;
        ClientComboBox.DisplayMemberPath = "FullName";
        ClientComboBox.SelectedValuePath = "Id";
        
        if (_isEdit)
        {
            RoomComboBox.SelectedValue = Booking.RoomId;
            ClientComboBox.SelectedValue = Booking.ClientId;
            CheckInDatePicker.SelectedDate = Booking.CheckInDate;
            CheckOutDatePicker.SelectedDate = Booking.CheckOutDate;
            NotesTextBox.Text = Booking.Notes;
            
            _originalRoomId = Booking.RoomId;
            _originalClientId = Booking.ClientId;
            _originalCheckIn = Booking.CheckInDate;
            _originalCheckOut = Booking.CheckOutDate;
            _originalNotes = Booking.Notes ?? "";
        }
        else
        {
            CheckInDatePicker.SelectedDate = DateTime.Today;
            CheckOutDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            
            _originalCheckIn = DateTime.Today;
            _originalCheckOut = DateTime.Today.AddDays(1);
        }
        UpdatePrice();
    }

    private async void UpdatePrice()
    {
        if (RoomComboBox.SelectedValue is int roomId && CheckInDatePicker.SelectedDate.HasValue && CheckOutDatePicker.SelectedDate.HasValue)
        {
            var price = await _bookingService.CalculateBookingPriceAsync(roomId, CheckInDatePicker.SelectedDate.Value, CheckOutDatePicker.SelectedDate.Value);
            TotalPriceText.Text = $"{price:N0}";
            TotalPriceText.Foreground = System.Windows.Media.Brushes.White;
            Booking.TotalPrice = price;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (RoomComboBox.SelectedValue == null)
        {
            MessageBox.Show("Выберите номер", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ClientComboBox.SelectedValue == null)
        {
            MessageBox.Show("Выберите клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!CheckInDatePicker.SelectedDate.HasValue || !CheckOutDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Выберите даты", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (CheckOutDatePicker.SelectedDate <= CheckInDatePicker.SelectedDate)
        {
            MessageBox.Show("Дата выезда должна быть позже даты заезда", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var roomId = (int)RoomComboBox.SelectedValue;
        var checkIn = CheckInDatePicker.SelectedDate.Value;
        var checkOut = CheckOutDatePicker.SelectedDate.Value;

        // Проверяем доступность номера
        var excludeId = _isEdit ? Booking.Id : (int?)null;
        var isAvailable = await _bookingService.IsRoomAvailableAsync(roomId, checkIn, checkOut, excludeId);

        if (!isAvailable)
        {
            // Получаем подробную информацию о пересечениях
            var allBookings = await _bookingService.GetBookingsByDateRangeAsync(checkIn.AddDays(-30), checkOut.AddDays(30));
            var overlaps = allBookings.Where(b =>
                b.RoomId == roomId &&
                b.Id != excludeId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Completed &&
                ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                 (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                 (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
                .ToList();

            if (overlaps.Any())
            {
                var overlapInfo = string.Join("\n", overlaps.Select(b =>
                    $"- {b.Client?.FullName ?? "Клиент #" + b.ClientId}: {b.CheckInDate:dd.MM} - {b.CheckOutDate:dd.MM}"));

                MessageBox.Show($"Номер занят на указанные даты бронированиями:\n\n{overlapInfo}\n\nВыберите другие даты!",
                    "Подтверждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Этот номер уже занят на выбранные даты! Выберите другие даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        Booking.RoomId = roomId;
        Booking.ClientId = (int)ClientComboBox.SelectedValue;
        Booking.CheckInDate = checkIn;
        Booking.CheckOutDate = checkOut;
        Booking.Notes = NotesTextBox.Text;

        DialogResult = true;
        _isSaved = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isSaved) return;

        // Проверяем наличие изменений
        var currentRoomId = RoomComboBox.SelectedValue as int? ?? 0;
        var currentClientId = ClientComboBox.SelectedValue as int? ?? 0;
        var currentCheckIn = CheckInDatePicker.SelectedDate ?? DateTime.MinValue;
        var currentCheckOut = CheckOutDatePicker.SelectedDate ?? DateTime.MinValue;
        var currentNotes = NotesTextBox.Text ?? "";

        bool hasChanges = false;
        if (_isEdit)
        {
            hasChanges = currentRoomId != _originalRoomId ||
                         currentClientId != _originalClientId ||
                         currentCheckIn != _originalCheckIn ||
                         currentCheckOut != _originalCheckOut ||
                         currentNotes != _originalNotes;
        }
        else
        {
            hasChanges = currentRoomId > 0 || currentClientId > 0 ||
                         currentCheckIn != DateTime.Today ||
                         currentCheckOut != DateTime.Today.AddDays(1) ||
                         !string.IsNullOrEmpty(currentNotes);
        }

        if (hasChanges)
        {
            var result = MessageBox.Show("Есть несохранённые изменения. Закрыть?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }

    private void RoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePrice();
    }

    private void CheckInDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePrice();
    }

    private void CheckOutDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePrice();
    }
}

