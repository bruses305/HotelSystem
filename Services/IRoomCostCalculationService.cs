using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IRoomCostCalculationService
{
    Task<decimal> CalculateRoomCostAsync(Room room, decimal occupancyRate = 0.8m);
    Task<Dictionary<int, decimal>> CalculateAllRoomsCostAsync();
    Task<CostCalculationDetails> GetCalculationDetailsAsync(Room room, decimal occupancyRate = 0.8m);
}