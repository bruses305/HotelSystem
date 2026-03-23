using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IServiceRepository : IRepository<Service>
{
    Task<IEnumerable<Service>> GetActiveServicesAsync();
}
