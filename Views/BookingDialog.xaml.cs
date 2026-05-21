using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;
using HotelSystem.Controls;

namespace HotelSystem.Views;

public partial class BookingDialog : DialogBase
{
    public Booking Booking { get; private set; }
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly bool _isEdit;
    private List<Room> _allRooms = new();
        
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

    protected override bool HasChanges => 
        RoomComboBox.SelectedValue as int? != _originalRoomId ||
        ClientAutoComplete.SelectedClient?.Id != _originalClientId ||
        CheckInDatePicker.SelectedDate != _originalCheckIn ||
        CheckOutDatePicker.SelectedDate != _originalCheckOut ||
        NotesTextBox.Text != _originalNotes;
    
    protected override async void Save()
    {
        if (RoomComboBox.SelectedValue == null)
        {
            MessageBoxHelper.ShowError("Выберите номер");
            return;
        }

        if (ClientAutoComplete.SelectedClient == null)
        {
            MessageBoxHelper.ShowError("Выберите или создайте клиента");
            return;
        }

        if (!CheckInDatePicker.SelectedDate.HasValue || !CheckOutDatePicker.SelectedDate.HasValue)
        {
            MessageBoxHelper.ShowError("Выберите даты");
            return;
        }
        
        if (CheckOutDatePicker.SelectedDate <= CheckInDatePicker.SelectedDate)
        {
            MessageBoxHelper.ShowError("Дата выезда должна быть позже даты заезда");
            return;
        }

        var roomId = (int)RoomComboBox.SelectedValue;
        var clientId = ClientAutoComplete.SelectedClient.Id;
        var checkIn = CheckInDatePicker.SelectedDate.Value;
        var checkOut = CheckOutDatePicker.SelectedDate.Value;

        // Проверяем доступность номера
        var excludeId = _isEdit ? Booking.Id : (int?)null;
        var isAvailable = await _bookingService.IsRoomAvailableAsync(roomId, checkIn, checkOut, excludeId);

        if (!isAvailable)
        {
            var allBookings = await _bookingService.GetBookingsByDateRangeAsync(checkIn.AddDays(-30), checkOut.AddDays(30));
            var overlaps = allBookings.Where(b =>
                b.RoomId == roomId &&
                b.Id != excludeId &&
                b.Status != BookingStatus.Отменено &&
                b.Status != BookingStatus.Завершено &&
                ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                 (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                 (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
                .ToList();

            if (overlaps.Any())
            {
                var overlapInfo = string.Join("\n", overlaps.Select(b =>
                    $"- {b.Client?.FullName ?? "Клиент #" + b.ClientId}: {b.CheckInDate:dd.MM} - {b.CheckOutDate:dd.MM}"));

                MessageBoxHelper.ShowWarning($"Номер занят на указанные даты бронированиями:\n\n{overlapInfo}\n\nВыберите другие даты!");
            }
            else
            {
                MessageBoxHelper.ShowError("Этот номер уже занят на выбранные даты! Выберите другие даты.");
            }
            return;
        }

        Booking.RoomId = roomId;
        Booking.ClientId = clientId;
        Booking.CheckInDate = checkIn;
        Booking.CheckOutDate = checkOut;
        Booking.Notes = NotesTextBox.Text;

        MarkAsSaved();
        DialogResult = true;
        CloseWithoutPrompt();
    }

    protected override void Cancel()
    {
        base.Cancel();
        CloseWithoutPrompt();
    }

    private async void InitializeForm()
    {
        _allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
        RoomComboBox.ItemsSource = _allRooms;
        RoomComboBox.DisplayMemberPath = "Name";
        RoomComboBox.SelectedValuePath = "Id";

        var clients = await _clientService.GetAllClientsAsync();
        ClientAutoComplete.SetClients(clients.ToList());

        ClientAutoComplete.SetClientSelectedHandler(client =>
        {
            if (client != null) Booking.ClientId = client.Id;
            return null;
        });
        ClientAutoComplete.SetCreateClientHandler(CreateNewClientAsync);

        if (_isEdit)
        {
            RoomComboBox.SelectedValue = Booking.RoomId;
            // Загружаем клиента, если его нет в объекте Booking
            if (Booking.Client == null && Booking.ClientId > 0)
            {
                Booking.Client = await _clientService.GetClientByIdAsync(Booking.ClientId);
            }

            if (Booking.Client != null)
                ClientAutoComplete.InputText = Booking.Client.FullName;

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

        await UpdatePrice();
    }

    private async Task UpdatePrice()
    {
        if (RoomComboBox.SelectedValue is int roomId && 
            CheckInDatePicker.SelectedDate.HasValue && 
            CheckOutDatePicker.SelectedDate.HasValue)
        {
            try
            {
                var price = await _bookingService.CalculateBookingPriceAsync(roomId, 
                    CheckInDatePicker.SelectedDate.Value, 
                    CheckOutDatePicker.SelectedDate.Value);
                TotalPriceText.Text = AppConstants.FormatPrice(price);   // ← так и есть
                Booking.TotalPrice = price;
            }
            catch (Exception ex)
            {
                TotalPriceText.Text = "Ошибка";
                MessageBox.Show($"UpdatePrice error: {ex.Message}");
            }
        }
    }

    private async Task<Client?> CreateNewClientAsync()
    {
        var clientName = ClientAutoComplete.InputText.Trim();
        
        if (string.IsNullOrEmpty(clientName))
            return null;

        // Проверяем права на создание клиента
        if (!PermissionChecker.CanCreate(PermissionCategory.Clients))
        {
            MessageBoxHelper.ShowError("Недостаточно прав для создания клиента!");
            return null;
        }

        // Создаём диалог клиента на основе главного окна
        var clientDialog = new ClientDialog();
        
        // Показываем как модальный диалог относительно главного окна
        var mainWindow = Application.Current.MainWindow;
        clientDialog.Owner = mainWindow;
        
        // Устанавливаем имя ПЕРЕД показом диалога
        if (!string.IsNullOrEmpty(clientName))
        {
            clientDialog.SetClientName(clientName);
        }
        
        var result = clientDialog.ShowDialog();

        if (result == true && clientDialog.Client != null)
        {
            var newClient = clientDialog.Client;
            
            // Сохраняем клиента
            await _clientService.CreateClientAsync(newClient);
            
            // Обновляем список клиентов
            var clients = await _clientService.GetAllClientsAsync();
            ClientAutoComplete.SetClients(clients.ToList());
            
            // Выбираем созданного клиента
            ClientAutoComplete.InputText = newClient.FullName;
            Booking.ClientId = newClient.Id;
            
            return newClient;
        }

        return null;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
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

