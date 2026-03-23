using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IBookingService
{
    Task<IEnumerable<Booking>> GetAllBookingsAsync();
    Task<IEnumerable<Booking>> GetAllBookingsWithDetailsAsync();
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<Booking> CreateBookingAsync(Booking booking);
    Task UpdateBookingAsync(Booking booking);
    Task DeleteBookingAsync(int id);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null);
    Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync();
    Task UpdateBookingStatusAsync(int id, BookingStatus status);
    Task CheckInAsync(int id);
    Task CompleteBookingAsync(int id);
    Task CancelBookingAsync(int id, string reason);
    Task<IEnumerable<Booking>> GetPendingCheckInsAsync();
    Task<IEnumerable<Booking>> GetPendingCheckOutsAsync();
    Task AddServiceToBookingAsync(int bookingId, int serviceId, int quantity);
    Task RemoveServiceFromBookingAsync(int bookingId, int serviceId);
    Task<decimal> CalculateBookingPriceAsync(int roomId, DateTime checkIn, DateTime checkOut);
}
