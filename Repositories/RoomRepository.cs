using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class RoomRepository : Repository<Room>, IRoomRepository
{
    public RoomRepository(HotelDbContext context) : base(context) { }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
    {
        var bookedRoomIds = await _context.Bookings
            .Where(b => b.Status != BookingStatus.Отменено &&
                        ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                         (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                         (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
            .Select(b => b.RoomId)
            .ToListAsync();

        return await _dbSet
            .Where(r => !bookedRoomIds.Contains(r.Id) && r.Status != RoomStatus.Ремонт)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomWithBookingsAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Bookings.Where(b => b.Status != BookingStatus.Отменено))
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status)
    {
        return await _dbSet.Where(r => r.Status == status).ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetRoomsByTypeAsync(RoomType type)
    {
        return await _dbSet.Where(r => r.Type == type).ToListAsync();
    }

    public async Task<decimal> GetTotalExpensesAsync()
    {
        // Возвращаем сумму себестоимости всех номеров
        var rooms = await _dbSet.ToListAsync();
        return rooms.Sum(r => r.Cost);
    }
}
