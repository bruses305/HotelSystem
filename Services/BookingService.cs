using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Models;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogService _logService;

    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IClientRepository clientRepository,
        IServiceRepository serviceRepository,
        ITransactionRepository transactionRepository,
        ILogService logService)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _clientRepository = clientRepository;
        _serviceRepository = serviceRepository;
        _transactionRepository = transactionRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
    {
        return await _bookingRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Booking>> GetAllBookingsWithDetailsAsync()
    {
        return await _bookingRepository.GetAllWithDetailsAsync();
    }

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        return await _bookingRepository.GetBookingWithDetailsAsync(id);
    }

    public async Task<Booking> CreateBookingAsync(Booking booking)
    {
        if (!await IsRoomAvailableAsync(booking.RoomId, booking.CheckInDate, booking.CheckOutDate))
        {
            throw new InvalidOperationException("Номер недоступен на выбранные даты");
        }

        var room = await _roomRepository.GetByIdAsync(booking.RoomId);
        if (room == null)
            throw new InvalidOperationException("Номер не найден");

        booking.TotalPrice =
            await CalculateBookingPriceAsync(booking.RoomId, booking.CheckInDate, booking.CheckOutDate);
        booking.Status = BookingStatus.Ожидание;

        var created = await _bookingRepository.AddAsync(booking);

        await _logService.LogAsync(
            LogLevel.Средние,
            $"Создано бронирование #{created.Id} для клиента {booking.ClientId}  пользователем: {AuthService.CurrentEmployee.FullName}",
            "BookingService");

        var client = await _clientRepository.GetByIdAsync(booking.ClientId);
        var bookingRoom = await _roomRepository.GetByIdAsync(booking.RoomId);
        NotificationService.Instance.AddNotification(
            "Новое бронирование",
            $"{bookingRoom?.Name ?? "Номер"} - {client?.FullName ?? "Клиент"} ({booking.CheckInDate:dd.MM} - {booking.CheckOutDate:dd.MM})",
            NotificationType.Booking,
            created.Id);

        return created;
    }

    public async Task UpdateBookingAsync(Booking booking)
    {
        booking.UpdatedAt = DateTime.Now;
        await _bookingRepository.UpdateAsync(booking);
        await _logService.LogAsync(LogLevel.Обычные, $"Обновлено бронирование #{booking.Id} пользователем: {AuthService.CurrentEmployee.FullName}", "BookingService");
    }

    public async Task DeleteBookingAsync(int id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking != null)
        {
            await _bookingRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Важные, $"Удалено бронирование #{id} пользователем: {AuthService.CurrentEmployee.FullName}", "BookingService");
        }
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut,
        int? excludeBookingId = null)
    {
        return await _bookingRepository.IsRoomAvailableAsync(roomId, checkIn, checkOut, excludeBookingId);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _bookingRepository.GetBookingsByDateRangeAsync(startDate, endDate);
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
    {
        return await _bookingRepository.GetActiveBookingsAsync();
    }

    public async Task UpdateBookingStatusAsync(int id, BookingStatus status)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking != null)
        {
            booking.Status = status;
            booking.UpdatedAt = DateTime.Now;
            await _bookingRepository.UpdateAsync(booking);

            await _logService.LogAsync(
                LogLevel.Обычные,
                $"Статус бронирования #{id} изменён на {status} пользователем: {AuthService.CurrentEmployee.FullName}",
                "BookingService");
        }
    }

    public async Task CheckInAsync(int id)
    {
        var booking = await _bookingRepository.GetBookingWithDetailsAsync(id);
        if (booking == null) return;

        booking.Status = BookingStatus.Заселён;
        booking.UpdatedAt = DateTime.Now;

        var room = await _roomRepository.GetByIdAsync(booking.RoomId);
        if (room != null)
        {
            room.Status = RoomStatus.Занят;
            await _roomRepository.UpdateAsync(room);
        }

        await _bookingRepository.UpdateAsync(booking);

        await _logService.LogAsync(
            LogLevel.Средние,
            $"Гость заселён в номер {room?.Name}. Бронирование #{id}",
            "BookingService");

        NotificationService.Instance.AddNotification(
            "Гость заселён",
            $"{booking.Room?.Name ?? room?.Name ?? booking.RoomId.ToString()} - {booking.Client?.FullName ?? "Клиент"}",
            NotificationType.CheckIn,
            booking.Id);
    }

    public async Task CompleteBookingAsync(int id)
    {
        var booking = await _bookingRepository.GetBookingWithDetailsAsync(id);
        if (booking == null) return;

        booking.Status = BookingStatus.Завершено;
        booking.UpdatedAt = DateTime.Now;

        var room = await _roomRepository.GetByIdAsync(booking.RoomId);
        if (room != null)
        {
            room.Status = RoomStatus.Свободен;
            await _roomRepository.UpdateAsync(room);
        }

        var client = await _clientRepository.GetByIdAsync(booking.ClientId);
        if (client != null)
        {
            client.TotalSpent += booking.PaidAmount;
            await _clientRepository.UpdateAsync(client);
        }

        await _bookingRepository.UpdateAsync(booking);

        await _logService.LogAsync(
            LogLevel.Средние,
            $"Завершено бронирование #{id}",
            "BookingService");

        NotificationService.Instance.AddNotification(
            "Гость выселен",
            $"{booking.Room?.Name ?? room?.Name ?? booking.RoomId.ToString()} - {booking.Client?.FullName ?? "Клиент"}",
            NotificationType.CheckOut,
            booking.Id);
    }

    public async Task CancelBookingAsync(int id, string reason)
    {
        var booking = await _bookingRepository.GetBookingWithDetailsAsync(id);
        if (booking == null) return;

        booking.Status = BookingStatus.Отменено;
        booking.Notes += $"\nОтмена: {reason}";
        booking.UpdatedAt = DateTime.Now;

        var room = await _roomRepository.GetByIdAsync(booking.RoomId);
        if (room != null)
        {
            room.Status = RoomStatus.Свободен;
            await _roomRepository.UpdateAsync(room);
        }

        await _bookingRepository.UpdateAsync(booking);

        await _logService.LogAsync(
            LogLevel.Важные,
            $"Отменено бронирование #{id}. Причина: {reason} пользователем: {AuthService.CurrentEmployee.FullName}",
            "BookingService");

        NotificationService.Instance.AddNotification(
            "Бронирование отменено",
            $"{booking.Room?.Name ?? room?.Name ?? booking.RoomId.ToString()} - {booking.Client?.FullName ?? "Клиент"}. Причина: {reason}",
            NotificationType.Cancellation,
            booking.Id);
    }

    public async Task<IEnumerable<Booking>> GetPendingCheckInsAsync()
    {
        return await _bookingRepository.GetPendingCheckInsAsync();
    }

    public async Task<IEnumerable<Booking>> GetPendingCheckOutsAsync()
    {
        return await _bookingRepository.GetPendingCheckOutsAsync();
    }

    public async Task AddServiceToBookingAsync(int bookingId, int serviceId, int quantity)
    {
        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service == null) throw new InvalidOperationException("Услуга не найдена");

        var booking = await _bookingRepository.GetBookingWithDetailsAsync(bookingId);
        if (booking == null) throw new InvalidOperationException("Бронирование не найдено");

        var bookingServiceEntity = new Models.Entities.BookingService
        {
            BookingId = bookingId,
            ServiceId = serviceId,
            Quantity = quantity,
            TotalPrice = service.Price * quantity
        };

        await _bookingRepository.AddBookingServiceAsync(bookingServiceEntity);

        booking.TotalPrice += bookingServiceEntity.TotalPrice;
        await _bookingRepository.UpdateAsync(booking);

        await _logService.LogAsync(
            LogLevel.Обычные,
            $"Добавлена услуга {service.Name} в бронирование #{bookingId} ({quantity} шт.) пользователем: {AuthService.CurrentEmployee.FullName}",
            "BookingService");
    }

    public async Task RemoveServiceFromBookingAsync(int bookingId, int serviceId)
    {
        var booking = await _bookingRepository.GetBookingWithDetailsAsync(bookingId);
        if (booking == null) throw new InvalidOperationException("Бронирование не найдено");

        var bookingService = await _bookingRepository.GetBookingServiceAsync(bookingId, serviceId);
        if (bookingService == null) throw new InvalidOperationException("Услуга не найдена в бронировании");

        booking.TotalPrice -= bookingService.TotalPrice;
        await _bookingRepository.RemoveBookingServiceAsync(bookingId, serviceId);
        await _bookingRepository.UpdateAsync(booking);

        await _logService.LogAsync(
            LogLevel.Обычные,
            $"Удалена услуга из бронирования #{bookingId} пользователем: {AuthService.CurrentEmployee.FullName}",
            "BookingService");
    }

    public async Task<decimal> CalculateBookingPriceAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return 0;

        var days = (checkOut - checkIn).Days;
        return room.Price * days;
    }
}
