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
            .Where(b => b.Status != BookingStatus.Cancelled &&
                        ((b.CheckInDate <= checkIn && b.CheckOutDate > checkIn) ||
                         (b.CheckInDate < checkOut && b.CheckOutDate >= checkOut) ||
                         (b.CheckInDate >= checkIn && b.CheckOutDate <= checkOut)))
            .Select(b => b.RoomId)
            .ToListAsync();

        return await _dbSet
            .Where(r => !bookedRoomIds.Contains(r.Id) && r.Status != RoomStatus.Repair)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomWithBookingsAsync(int id)
    {
        return await _dbSet
            .Include(r => r.Bookings.Where(b => b.Status != BookingStatus.Cancelled))
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
        // SQLite Р В Р вЂ¦Р В Р’Вµ Р В РЎвЂ”Р В РЎвЂўР В РўвЂР В РўвЂР В Р’ВµР РЋР вЂљР В Р’В¶Р В РЎвЂР В Р вЂ Р В Р’В°Р В Р’ВµР РЋРІР‚С™ SumAsync Р В РўвЂР В Р’В»Р РЋР РЏ decimal, Р В Р’В·Р В Р’В°Р В РЎвЂ“Р РЋР вЂљР РЋРЎвЂњР В Р’В¶Р В Р’В°Р В Р’ВµР В РЎВ Р В Р вЂ¦Р В Р’В° Р В РЎвЂќР В Р’В»Р В РЎвЂР В Р’ВµР В Р вЂ¦Р РЋРІР‚С™
        var rooms = await _dbSet.ToListAsync();
        return rooms.Sum(r => r.WaterExpense + r.ElectricityExpense + 
                              r.InternetExpense + r.CleaningExpense);
    }
}
