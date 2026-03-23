using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogService _logService;

    public ClientService(
        IClientRepository clientRepository,
        IBookingRepository bookingRepository,
        ILogService logService)
    {
        _clientRepository = clientRepository;
        _bookingRepository = bookingRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Client>> GetAllClientsAsync()
    {
        return await _clientRepository.GetAllAsync();
    }

    public async Task<Client?> GetClientByIdAsync(int id)
    {
        return await _clientRepository.GetClientWithBookingsAsync(id);
    }

    public async Task<Client> CreateClientAsync(Client client)
    {
        var created = await _clientRepository.AddAsync(client);
        await _logService.LogAsync(LogLevel.Medium, 
            $"Создание клиента: {client.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "ClientService");
        return created;
    }

    public async Task UpdateClientAsync(Client client)
    {
        client.UpdatedAt = DateTime.Now;
        await _clientRepository.UpdateAsync(client);
        await _logService.LogAsync(LogLevel.Low, 
            $"Клиент обнавлён: {client.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "ClientService");
    }

    public async Task DeleteClientAsync(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client != null)
        {
            await _clientRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Critical, 
                $"Клиент удалён: {client.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "ClientService");
        }
    }

    public async Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm)
    {
        return await _clientRepository.SearchClientsAsync(searchTerm);
    }

    public async Task<IEnumerable<Client>> GetTopClientsAsync(int count)
    {
        return await _clientRepository.GetTopClientsAsync(count);
    }

    public async Task<IEnumerable<Booking>> GetClientBookingsAsync(int clientId)
    {
        return await _bookingRepository.GetBookingsByClientAsync(clientId);
    }
}
