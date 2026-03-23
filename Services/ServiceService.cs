using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class ServiceService : IServiceService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogService _logService;

    public ServiceService(IServiceRepository serviceRepository, ILogService logService)
    {
        _serviceRepository = serviceRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Service>> GetAllServicesAsync()
    {
        return await _serviceRepository.GetAllAsync();
    }

    public async Task<Service?> GetServiceByIdAsync(int id)
    {
        return await _serviceRepository.GetByIdAsync(id);
    }

    public async Task<Service> CreateServiceAsync(Service service)
    {
        var created = await _serviceRepository.AddAsync(service);
        await _logService.LogAsync(LogLevel.Medium, 
            $"Добавление услуги°: {service.Name} пользователем: {AuthService.CurrentEmployee.FullName}", "ServiceService");
        return created;
    }

    public async Task UpdateServiceAsync(Service service)
    {
        await _serviceRepository.UpdateAsync(service);
        await _logService.LogAsync(LogLevel.Low, 
            $"Обновление услуги: {service.Name} пользователем: {AuthService.CurrentEmployee.FullName}", "ServiceService");
    }

    public async Task DeleteServiceAsync(int id)
    {
        var service = await _serviceRepository.GetByIdAsync(id);
        if (service != null)
        {
            await _serviceRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Critical, 
                $"Удаление услуги: {service.Name}  пользователем: {AuthService.CurrentEmployee.FullName}", "ServiceService");
        }
    }

    public async Task<IEnumerable<Service>> GetActiveServicesAsync()
    {
        return await _serviceRepository.GetActiveServicesAsync();
    }
}
