using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllRoomsAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    Task<Room> CreateRoomAsync(Room room);
    Task UpdateRoomAsync(Room room);
    Task DeleteRoomAsync(int id);
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut);
    Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status);
    Task<IEnumerable<Room>> GetRoomsByTypeAsync(RoomType type);
    Task UpdateRoomStatusAsync(int id, RoomStatus status);
    Task<decimal> GetTotalExpensesAsync();
}
