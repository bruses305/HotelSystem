using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<Booking?> GetBookingWithDetailsAsync(int id);
    Task<IEnumerable<Booking>> GetAllWithDetailsAsync();
    Task<IEnumerable<Booking>> GetBookingsByRoomAsync(int roomId);
    Task<IEnumerable<Booking>> GetBookingsByClientAsync(int clientId);
    Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync();
    Task<IEnumerable<Booking>> GetPendingCheckInsAsync();
    Task<IEnumerable<Booking>> GetPendingCheckOutsAsync();
    Task AddBookingServiceAsync(BookingService bookingService);
    Task RemoveBookingServiceAsync(int bookingId, int serviceId);
    Task<BookingService?> GetBookingServiceAsync(int bookingId, int serviceId);
}
