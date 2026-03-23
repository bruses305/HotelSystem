using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class BookingRepository : Repository<Booking>, IBookingRepository
{
    public BookingRepository(HotelDbContext context) : base(context) { }

    public async Task<Booking?> GetBookingWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(b => b.Room)
            .Include(b => b.Client)
            .Include(b => b.CreatedBy)
            .Include(b => b.BookingServices)
            .ThenInclude(bs => bs.Service)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(b => b.Room)
            .Include(b => b.Client)
            .OrderByDescending(b => b.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByRoomAsync(int roomId)
    {
        return await _dbSet
            .Where(b => b.RoomId == roomId)
            .Include(b => b.Client)
            .OrderByDescending(b => b.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByClientAsync(int clientId)
    {
        return await _dbSet
            .Where(b => b.ClientId == clientId)
            .Include(b => b.Room)
            .OrderByDescending(b => b.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(b => b.Room)
            .Include(b => b.Client)
            .Where(b => b.CheckInDate >= startDate && b.CheckInDate <= endDate)
            .OrderBy(b => b.CheckInDate)
            .ToListAsync();
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeBookingId = null)
    {
        var bookings = await _dbSet
            .Where(b => b.RoomId == roomId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) &&
                        ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                         (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                         (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
            .ToListAsync();

        if (excludeBookingId.HasValue)
        {
            bookings = bookings.Where(b => b.Id != excludeBookingId.Value).ToList();
        }

        return !bookings.Any();
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
    {
        return await _dbSet
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid)
            .Include(b => b.Room)
            .Include(b => b.Client)
            .OrderBy(b => b.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetPendingCheckInsAsync()
    {
        var today = DateTime.Today;
        return await _dbSet
            .Where(b => b.Status == BookingStatus.Confirmed && b.CheckInDate.Date == today)
            .Include(b => b.Room)
            .Include(b => b.Client)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetPendingCheckOutsAsync()
    {
        var today = DateTime.Today;
        return await _dbSet
            .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) && 
                        b.CheckOutDate.Date == today)
            .Include(b => b.Room)
            .Include(b => b.Client)
            .ToListAsync();
    }

    public async Task AddBookingServiceAsync(BookingService bookingService)
    {
        _context.Set<BookingService>().Add(bookingService);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveBookingServiceAsync(int bookingId, int serviceId)
    {
        var bookingService = await _context.Set<BookingService>()
            .FirstOrDefaultAsync(bs => bs.BookingId == bookingId && bs.ServiceId == serviceId);
        
        if (bookingService != null)
        {
            _context.Set<BookingService>().Remove(bookingService);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<BookingService?> GetBookingServiceAsync(int bookingId, int serviceId)
    {
        return await _context.Set<BookingService>()
            .FirstOrDefaultAsync(bs => bs.BookingId == bookingId && bs.ServiceId == serviceId);
    }
}
