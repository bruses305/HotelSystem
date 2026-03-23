using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IRoomRepository : IRepository<Room>
{
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
    Task<Room?> GetRoomWithBookingsAsync(int id);
    Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status);
    Task<IEnumerable<Room>> GetRoomsByTypeAsync(RoomType type);
    Task<decimal> GetTotalExpensesAsync();
}
