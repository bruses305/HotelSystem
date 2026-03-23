using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IServiceService
{
    Task<IEnumerable<Service>> GetAllServicesAsync();
    Task<Service?> GetServiceByIdAsync(int id);
    Task<Service> CreateServiceAsync(Service service);
    Task UpdateServiceAsync(Service service);
    Task DeleteServiceAsync(int id);
    Task<IEnumerable<Service>> GetActiveServicesAsync();
}
