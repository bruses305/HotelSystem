using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly ILogService _logService;

    public RoomService(IRoomRepository roomRepository, ILogService logService)
    {
        _roomRepository = roomRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync()
    {
        return await _roomRepository.GetAllAsync();
    }

    public async Task<Room?> GetRoomByIdAsync(int id)
    {
        return await _roomRepository.GetByIdAsync(id);
    }

    public async Task<Room> CreateRoomAsync(Room room)
    {
        var created = await _roomRepository.AddAsync(room);
        await _logService.LogAsync(LogLevel.Medium, $"Создание номера: {room.Name} пользователем: {AuthService.CurrentEmployee.FullName}", "RoomService");
        return created;
    }

    public async Task UpdateRoomAsync(Room room)
    {
        await _roomRepository.UpdateAsync(room);
        await _logService.LogAsync(LogLevel.Low, $"Обновление номера: {room.Name} пользователем: {AuthService.CurrentEmployee.FullName}", "RoomService");
    }

    public async Task DeleteRoomAsync(int id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        if (room != null)
        {
            await _roomRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Critical, $"удаление номера: {room.Name} пользователем: {AuthService.CurrentEmployee.FullName}", "RoomService");
        }
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut)
    {
        return await _roomRepository.GetAvailableRoomsAsync(checkIn, checkOut);
    }

    public async Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status)
    {
        return await _roomRepository.GetRoomsByStatusAsync(status);
    }

    public async Task<IEnumerable<Room>> GetRoomsByTypeAsync(RoomType type)
    {
        return await _roomRepository.GetRoomsByTypeAsync(type);
    }

    public async Task UpdateRoomStatusAsync(int id, RoomStatus status)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        if (room != null)
        {
            room.Status = status;
            room.UpdatedAt = DateTime.Now;
            await _roomRepository.UpdateAsync(room);
            await _logService.LogAsync(LogLevel.Low, $"обновление статуса номера° {room.Name} на ° {status} пользователем: {AuthService.CurrentEmployee.FullName}", "RoomService");
        }
    }

    public async Task<decimal> GetTotalExpensesAsync()
    {
        return await _roomRepository.GetTotalExpensesAsync();
    }
}
